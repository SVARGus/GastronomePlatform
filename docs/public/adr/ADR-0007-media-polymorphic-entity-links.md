# ADR-0007: Кросс-модульные ссылки в Media — полиморфная привязка `EntityType + EntityId`

**Status:** Accepted
**Date:** 2026-05-31
**Updated:** 2026-08-02 — финализация при переносе в `docs/public/adr/`; добавлен контракт `IMediaService` (реализован 2026-06-13)
**Stage:** 2

---

## Related (Связи)

- **Связанные ADR:** [ADR-0001](./ADR-0001-modular-monolith.md) (модульный монолит — границы модулей, которые ссылка не должна нарушать), [ADR-0004](./ADR-0004-postgresql-schema-per-module.md) (схема на модуль; общий принцип «кросс-модульные ссылки без FK-constraint»), [ADR-0002](./ADR-0002-clean-architecture.md) (целостность проверяется на уровне Application)
- **Связанные модули:** Media (владелец решения); потребители — Dishes (`Dish.MainImageId`, `RecipeStep.ImageMediaId`, `Category.IconMediaId`, `Ingredient.ImageMediaId`), Users (`UserProfile.AvatarMediaId`)
- **Связанная документация:** `docs/public/modules/media/domain-model.md` (агрегат `MediaFile`, жизненный цикл orphan/attached), `docs/public/policies/POL-002-media-access.md`, `docs/public/policies/POL-003-media-ownership.md`

---

## 1. Context (Контекст)

Модуль Media хранит файлы для сущностей **разных** модулей: главное фото блюда и фото шага рецепта (Dishes), аватар пользователя (Users), иконка категории; в будущем — логотипы бизнесов, документы. Media при этом — инфраструктурный модуль нижнего уровня: он не должен знать доменные модели потребителей, а потребители не должны зависеть от внутренностей Media.

Нужен способ связать `MediaFile` с «сущностью-владельцем» так, чтобы:

1. Media мог ответить «чей это файл» — для авторизации доступа (POL-002/POL-003) и каскадной очистки при удалении владельца.
2. Появление нового типа-потребителя (новый модуль, новая сущность) не требовало миграций схемы `media`.
3. Границы модулей не нарушались FK-constraint-ами между схемами (принцип ADR-0004).
4. Незакреплённые файлы (загружен, но сущность так и не сохранена) не копились вечно.

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Ссылки только на стороне потребителей

`Dish.MainImageId → media.MediaFiles` и т.п.; Media о владельцах не знает.

**Плюсы:** Media максимально прост, никакой полиморфности.
**Минусы:** Media не может ни авторизовать доступ («файл приватного профиля?»), ни найти файлы удаляемой сущности, ни отличить orphan от привязанного — очистка сирот потребовала бы сканирования таблиц всех модулей-потребителей, что разрушает границы модулей.

**Причина отклонения:** orphan-очистка и авторизация доступа — обязанности Media; без знания владельца они нереализуемы.

### Вариант B — Отдельная join-таблица на каждый тип владельца

`media.DishImages (MediaId, DishId)`, `media.UserAvatars (MediaId, UserId)`, … с настоящими FK.

**Плюсы:** ссылочная целостность на уровне БД, типизированные связи.
**Минусы:** FK из схемы `media` в чужие схемы — прямое нарушение границ модулей (ADR-0004): модуль нельзя выделить в микросервис, миграции сцеплены. Каждый новый тип владельца — новая таблица и миграция в чужом для него модуле Media.

**Причина отклонения:** архитектурно несовместимо с модульным монолитом; масштабируется миграциями вместо констант.

### Вариант C — Флаг `IsAttached` + ссылки у потребителей

Media хранит только булев признак привязанности.

**Плюсы:** минимализм.
**Минусы:** флаг дублирует информацию, которую всё равно надо иметь («кто владелец») и при этом не отвечает на неё: авторизация и каскадная очистка остаются нерешёнными. Синхронизация флага с реальностью — ручная.

**Причина отклонения:** «знание без деталей» — худшее из обоих миров; пара `EntityType + EntityId` полностью заменяет флаг (attached ⇔ `EntityType IS NOT NULL`).

### Вариант D — Полиморфная пара `EntityType (varchar) + EntityId (uuid)` без FK ⭐

`MediaFile.EntityType` — строковый тип владельца из констант `MediaEntityTypes` («Dish», «RecipeStep», «UserAvatar», «CategoryIcon», «IngredientImage»), `MediaFile.EntityId` — идентификатор. FK-constraint отсутствует; целостность — на уровне Application через межмодульный сервис `IMediaService`.

**Плюсы:** новый тип владельца = новая константа (без миграции); Media знает владельца для авторизации, очистки и orphan-жизненного цикла; границы модулей не нарушены; при выделении Media в микросервис пара переживает переезд без изменений.
**Минусы:** нет ссылочной целостности БД — возможны «висячие» ссылки при рассинхронизации; строковый тип не проверяется компилятором.

---

## 3. Decision (Принятое решение)

### 3.1. Полиморфная пара на `MediaFile`

- `EntityType : varchar(50) NULL` — тип сущности-владельца, значения только из констант `MediaEntityTypes` (`Media.Domain/Constants/` — аналог `PlatformRoles` в Common).
- `EntityId : uuid NULL` — идентификатор владельца.
- **Инвариант парности:** оба NULL (orphan) или оба заполнены (attached); смешанное состояние запрещено Domain-фабрикой `MediaFile.Upload → Result<MediaFile>` (межполевой инвариант — причина формы `Result<T>` у фабрики).
- FK-constraint в БД отсутствует намеренно — общий принцип кросс-модульных ссылок проекта (ADR-0004).

### 3.2. Жизненный цикл через пару

- **Orphan** (`EntityType IS NULL`): файл загружен, но не привязан; `ExpiresAt = now + orphanTimeout` (конфигурация, по умолчанию 24 ч). Фоновая задача UC-MED-210 удаляет просроченных сирот. Частичный индекс `(Status, ExpiresAt) WHERE EntityType IS NULL` — компактная выборка кандидатов.
- **Attached** (`EntityType IS NOT NULL`): `AttachedAt` заполнен, `ExpiresAt = NULL`. Индекс `(EntityType, EntityId)` — «все файлы сущности» для каскадной очистки.
- `SoftDelete` требует предварительного detach (`MEDIA.STILL_ATTACHED`) — нельзя удалить файл, на который смотрит живая сущность.
- `EntityType` определяет и `DataCategory` (Public/Personal) при загрузке — маппинг в одном месте `Media.Application`; `intendedEntityType` передаётся клиентом при upload.

### 3.3. Контракт для модулей-потребителей — `IMediaService`

Потребители работают с Media **только** через межмодульный сервис (5 методов):

- `GetMetadataAsync(mediaId, ct)` / `GetMetadataBatchAsync(mediaIds, ct)` → `MediaMetadataDto` — проверка существования и метаданные перед сохранением ссылки у потребителя;
- `AttachToEntityAsync(mediaId, actorUserId, entityType, entityId, ct)` — привязка с проверкой владения файлом (POL-003);
- `DetachFromEntityAsync(mediaId, ct)` — отвязка (авторизацию операции уже выполнил вызывающий модуль над своей сущностью);
- `DeleteByEntityAsync(entityType, entityId, ct)` — каскадная очистка файлов удаляемой сущности.

Расположение контракта — `Media.Application/Contracts/IMediaService.cs` (известное отклонение от паттерна `Domain/Contracts`, принятого для `IAuthUserService`: при выделении Media в микросервис контракт копируется в `*.Contracts`-пакет, и расположение Application/Domain на операцию не влияет).

Типовой сценарий (смена главного фото блюда): хендлер Dishes проверяет файл через `GetMetadataAsync` → `AttachToEntityAsync(..., "Dish", dishId, ...)` → сохраняет `Dish.MainImageId`; при замене — `DetachFromEntityAsync` старого. Целостность двусторонней ссылки обеспечивается порядком операций в хендлере, не БД.

---

## 4. Rationale (Обоснование)

1. **Границы модулей важнее ссылочной целостности БД.** Проект — модульный монолит с прицелом на выделение модулей (ADR-0001): FK между схемами закрыл бы этот путь. Целостность через `IMediaService` — та же модель, что для `Dish.AuthorUserId → users` (без FK, проверка в Application).
2. **Расширяемость константой.** Новый потребитель (логотип бизнеса, Этап 6) — одна константа в `MediaEntityTypes` + правило маппинга DataCategory. Ни одной миграции Media.
3. **Media остаётся невежественным о доменах потребителей.** Он знает строку «Dish», но не тип `Dish` — ни ссылок на сборки, ни знания моделей.
4. **Orphan-жизненный цикл бесплатно.** Пара «не привязан + ExpiresAt» даёт естественную сборку мусора без отдельного статуса Temporary.
5. **Один вход для потребителей.** Пять методов `IMediaService` — вся публичная поверхность Media для модулей; внутренние UC (UC-MED-200..204) снаружи не видны (POL-003).

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- Новые типы владельцев добавляются без миграций и без знания Media о доменах потребителей.
- Авторизация доступа к файлу (POL-002: приватность аватаров/документов) и каскадная очистка реализуемы внутри Media.
- Сироты убираются фоновой задачей по частичному индексу — предсказуемая стоимость.
- Путь к микросервису Media открыт: пара переезжает как есть, контракт копируется в `*.Contracts`.

### Negative / Trade-offs (Отрицательные / компромиссы)

- Нет ссылочной целостности БД: удаление сущности мимо `DeleteByEntityAsync` оставит «висячие» attached-файлы. Митигация — дисциплина хендлеров удаления + (Этап 8+) фоновая сверка.
- Строковый `EntityType` не проверяется компилятором — только константы `MediaEntityTypes` и валидация при upload/attach.
- Двусторонняя ссылка (у потребителя `MainImageId`, у Media `EntityType+EntityId`) синхронизируется порядком операций в хендлере — ошибки порядка дают рассинхронизацию, заметную только по данным.

### Areas of Caution (На что обратить внимание)

- **Порядок операций в хендлерах потребителей:** проверить файл → attach → сохранить ссылку у себя; при замене — detach старого. Обратный порядок оставляет окно для orphan-очистки привязываемого файла.
- **`DetachFromEntityAsync` не проверяет владение** — авторизация на вызывающем модуле (он уже проверил права на свою сущность). Не вызывать по данным из недоверенного ввода.
- **При добавлении константы** в `MediaEntityTypes` — не забыть правило маппинга `EntityType → DataCategory` (личные типы должны становиться Personal, иначе файл окажется публичным).
- **Freeform-удаления сущностей** (архив блюда и т.п.) должны решать судьбу файлов явно: attach остаётся (файл жив с сущностью) или `DeleteByEntityAsync` (каскад).

---

## 6. Future Scope (Будущие направления)

- **Этап 6+:** новые типы владельцев (`BusinessLogo`, `UserDocument`) — уже зарезервированы комментариями в `MediaEntityTypes`.
- **Этап 8+:** фоновая сверка «висячих» attached-ссылок (файл указывает на несуществующую сущность) — компенсирует отсутствие FK.
- **Этап 8+:** при выделении Media в микросервис — пакет `Media.Contracts` (`IMediaService`, `MediaMetadataDto`, `MediaEntityTypes`), транспорт — по обстоятельствам (HTTP/gRPC), семантика пары не меняется.

---

## 7. Implementation Reference (Связь с кодовой базой)

- `src/Modules/Media/GastronomePlatform.Modules.Media.Domain/Entities/MediaFile.cs` — поля `EntityType`/`EntityId`/`AttachedAt`/`ExpiresAt`; фабрика `Upload → Result<MediaFile>` (инвариант парности); методы `AttachToEntity`, `DetachFromEntity`, `SoftDelete` (требует detach); события `MediaAttachedEvent`/`MediaDetachedEvent`.
- `src/Modules/Media/GastronomePlatform.Modules.Media.Domain/Constants/MediaEntityTypes.cs` — константы `DISH`, `RECIPE_STEP`, `CATEGORY_ICON`, `INGREDIENT_IMAGE`, `USER_AVATAR` (+ зарезервированные).
- `src/Modules/Media/GastronomePlatform.Modules.Media.Application/Contracts/IMediaService.cs` — 5 методов контракта + `MediaMetadataDto`; реализация — `Application/Services/MediaService.cs`.
- `src/Modules/Media/GastronomePlatform.Modules.Media.Infrastructure/Persistence/Configurations/MediaFileConfiguration.cs` — индексы `(EntityType, EntityId)` и частичный `(Status, ExpiresAt) WHERE EntityType IS NULL`.
- Потребители: `Dishes.Application` (main-image, фото шага — attach/detach через `IMediaService`), `Users.Application` (аватар).

---

## История изменений

- **2026-05-31:** Accepted. Пара `EntityType + EntityId`, инвариант парности и orphan-жизненный цикл реализованы вместе с Domain-слоем Media и первой миграцией схемы `media`.
- **2026-06-13:** Реализован межмодульный контракт `IMediaService` (5 методов); хендлеры Dishes переведены на attach/detach через сервис. Зафиксировано расположение контракта в `Media.Application/Contracts` как осознанное отклонение от паттерна `Domain/Contracts`.
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`, присвоен номер ADR-0007.
