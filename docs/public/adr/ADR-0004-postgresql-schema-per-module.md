# ADR-0004: PostgreSQL с разделением по схемам

**Status:** Accepted
**Date:** 2026-02-23
**Updated:** 2026-08-02 — финализация при переносе в `docs/public/adr/`; ранее 2026-05-10 — систематизированы OnDelete-стратегии и CHECK-constraints как defense-in-depth
**Stage:** 0

---

## Related (Связи)
- **Связанные ADR:** [ADR-0001](./ADR-0001-modular-monolith.md) (Modular Monolith — единая БД как следствие выбора монолита), [ADR-0006](./ADR-0006-dish-recipe-single-aggregate.md) (Dish + Recipe как один агрегат — cascade delete внутри агрегата), [ADR-0007](./ADR-0007-media-polymorphic-entity-links.md) (применение принципа «без FK между модулями» в Media), [ADR-0011](./ADR-0011-two-layer-dish-storage.md) (Двухслойное хранение Dish — jsonb-снепшот внутри схемы `dishes`)
- **Связанные модули:** все — каждый модуль владеет своей схемой
- **Связанная документация:** `<wiki>/02_Архитектура.md` §6 (стратегия данных), §7.4 (кросс-модульные ссылки без FK), §9.3–9.4 (Database per Service)

---

## 1. Context (Контекст)

В ADR-0001 принят модульный монолит с 8 бизнес-модулями. Каждый модуль инкапсулирует свой домен и владеет своими данными. Необходимо определить стратегию хранения данных:

- Как обеспечить логическую изоляцию данных между модулями, сохраняя простоту управления.
- Как подготовить переход к Database per Service при выделении модулей в микросервисы.
- Как управлять миграциями, когда у каждого модуля свой `DbContext`.
- Какую СУБД выбрать с учётом требований проекта (JSONB для гибких данных, полнотекстовый поиск, типы `uuid`, `timestamp with time zone`).

Ограничения: один разработчик, минимальный бюджет (локальный Docker, без managed-сервисов на старте).

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Единая БД, единая схема (public)

Все таблицы всех модулей в схеме `public`. Один `DbContext` на всё приложение.

**Плюсы:** максимально просто, единые миграции, можно использовать FK между таблицами разных модулей.
**Минусы:** нет логического разделения — таблицы `Dishes`, `Orders`, `Users` вперемешку. FK между модулями создают жёсткую связь, которую невозможно разорвать при выделении модуля. Один `DbContext` знает обо всех сущностях всех модулей — нарушение инкапсуляции.

**Причина отклонения:** противоречит принципу модульности (ADR-0001). Невозможно выделить модуль в микросервис без массивного рефакторинга БД.

### Вариант B — Отдельная БД на каждый модуль (Database per Module)

Каждый модуль подключается к своей физической БД. Полная изоляция.

**Плюсы:** максимальная изоляция, каждый модуль можно масштабировать и бэкапить отдельно, готово к микросервисам «из коробки».
**Минусы:** для одного разработчика — огромная операционная нагрузка: 8 строк подключения, 8 отдельных инстансов (или контейнеров), невозможность выполнить транзакцию, затрагивающую два модуля (даже в монолите). Docker Compose с 8 PostgreSQL-контейнерами избыточен для дипломного проекта.

**Причина отклонения:** непропорциональная сложность для одного разработчика. Транзакции между модулями (например, регистрация пользователя + создание профиля) потребовали бы Saga уже на Этапе 1.

### Вариант C — Единая БД, разделение по схемам (Schema per Module) ⭐

Один экземпляр PostgreSQL, каждый модуль работает в своей именованной схеме (`auth`, `users`, `dishes`, `media`, `subscriptions`, `notifications`, `social`, `orders`). Каждый модуль имеет свой `DbContext`, настроенный на свою схему.

**Плюсы:** логическая изоляция (таблицы `dishes.Dishes` и `users.UserProfiles` не смешиваются), каждый модуль управляет своими миграциями, простота инфраструктуры (один контейнер PostgreSQL), транзакции внутри одной БД при необходимости. При выделении в микросервис — `pg_dump` схемы в отдельную БД.
**Минусы:** физически одна БД — единая точка отказа, общие ресурсы (CPU, RAM, IO). FK между схемами технически возможны (PostgreSQL это позволяет) — нужна дисциплина, чтобы их не создавать.

---

## 3. Decision (Принятое решение)

Использовать **один экземпляр PostgreSQL 16** с **отдельной схемой на каждый модуль**.

### Структура схем

| Модуль | Схема | DbContext | Примеры таблиц |
|--------|-------|-----------|----------------|
| Auth | `auth` | `AuthDbContext` | `auth.AspNetUsers`, `auth.RefreshTokens` |
| Users | `users` | `UsersDbContext` | `users.UserProfiles` |
| Dishes | `dishes` | `DishesDbContext` | `dishes.Dishes`, `dishes.Recipes`, `dishes.Categories`, `dishes.Ingredients` |
| Media | `media` | `MediaDbContext` | `media.MediaFiles` |
| Subscriptions | `subscriptions` | `SubscriptionsDbContext` | `subscriptions.Plans`, `subscriptions.UserSubscriptions` |

### Правила

1. **Один DbContext на модуль.** Каждый DbContext настроен на свою схему через `HasDefaultSchema("xxx")` в `OnModelCreating`.

2. **Нет FK между схемами.** Ссылки на сущности другого модуля хранятся как `Guid` без FK-constraint. Целостность проверяется на уровне Application через межмодульные сервисы (например, `IMediaService.GetMetadataAsync`).

3. **Каждый модуль управляет своими миграциями.** Команда `dotnet ef` требует `--context` при нескольких DbContext в solution.

4. **Все временные поля — `timestamp with time zone`** (PostgreSQL) / `DateTimeOffset` (C#).

5. **PostgreSQL порт 5433** в docker-compose (конфликт с локально установленным PostgreSQL на машине разработчика).

### Кросс-модульные ссылки

```csharp
// В модуле Dishes — ссылка на автора из модуля Users:
public class Dish : AggregateRoot<Guid>
{
    public Guid AuthorUserId { get; private set; }  // Просто Guid, без FK на users.UserProfiles
}

// В модуле Users — ссылка на аватар из модуля Media:
public class UserProfile : Entity<Guid>
{
    public Guid? AvatarMediaId { get; private set; }  // Просто Guid, без FK на media.MediaFiles
}
```

Валидация: `AuthorUserId` извлекается из JWT (гарантированно существует), `AvatarMediaId` проверяется через `IMediaService`.

---

## 4. Rationale (Обоснование)

1. **Баланс изоляции и простоты.** Схемы дают логическое разделение (видно в `pg_catalog`, в IDE, в инструментах администрирования), не требуя отдельных инстансов БД. Один контейнер PostgreSQL в docker-compose — минимальная инфраструктура.

2. **Подготовка к Database per Service.** При выделении модуля в микросервис его схема экспортируется через `pg_dump -n dishes` в отдельную БД. Connection string меняется в appsettings — Domain и Application не затрагиваются.

3. **Изоляция миграций.** Каждый модуль развивает свою схему независимо. Добавление столбца в `dishes.Ingredients` не затрагивает `auth.RefreshTokens`. Конфликты миграций между модулями исключены.

4. **Нет FK между модулями — осознанное решение.** FK между схемами создал бы coupling на уровне БД, который невозможно разорвать при выделении. Каскадные удаления между модулями опасны: удаление пользователя не должно молча удалять его блюда — это бизнес-решение, а не БД-constraint.

5. **PostgreSQL — оптимальный выбор.** Нативный тип `uuid` (16 байт, оптимизированный) для `Entity<Guid>`. JSONB для `PublishedVersionData` в модуле Dishes (ADR-0011). `timestamp with time zone` для `DateTimeOffset`. Расширение `pg_trgm` для полнотекстового поиска на старте (до Elasticsearch). Открытый исходный код, бесплатный.

---

## 5. Consequences (Последствия)

### Positive (Положительные)
- Один PostgreSQL-контейнер — простая инфраструктура, одна строка подключения, один бэкап.
- Таблицы визуально сгруппированы по модулям в любом инструменте (pgAdmin, DataGrip, DBeaver).
- Транзакции между модулями технически возможны (одна БД), но используются осознанно и минимально.
- EF Core конфигурации каждого модуля изолированы в своём DbContext — нет risk'а случайно модифицировать чужую таблицу.

### Negative / Trade-offs (Отрицательные / компромиссы)
- Единая точка отказа — падение PostgreSQL останавливает все модули. Митигация: master-replica с автоматическим failover (Этап 8).
- Общие ресурсы — тяжёлый запрос в модуле Dishes может повлиять на производительность Auth. Митигация: read-реплики для read-heavy модулей.
- Дисциплина — PostgreSQL позволяет создать FK между схемами. Нужно проверять на Code Review.
- `--context` флаг обязателен для всех `dotnet ef` команд — легко забыть.

### Areas of Caution (На что обратить внимание)

- Каждый новый DbContext требует `IDesignTimeDbContextFactory` для EF Core CLI.
- Не создавать навигационные свойства EF Core, пересекающие границы модулей.
- Connection string одна на все модули (`Database` в appsettings). При переходе к Database per Service — каждый модуль получит свою строку.

#### OnDelete-стратегии для FK внутри модуля

Стратегия каскадирования выбирается **осознанно по роли сущности**, а не по умолчанию EF Core. Внутри одного модуля применяются четыре типа связей:

- **`Restrict`** — для FK на **справочники и защищаемые сущности**. Родитель не удаляется, пока на него ссылаются потомки. Примеры: `Ingredient.BaseMeasureUnitId → MeasureUnit`, `IngredientSpec.NutritionId → Nutrition`, `Category.ParentId → Category` (self-FK). Смысл: справочник теряет значение, если убрать хотя бы одну запись без учёта зависимостей.
- **`SetNull`** — для **опциональных вспомогательных ссылок**. При удалении родителя FK-поле обнуляется, потомок продолжает существовать без ссылки. Пример: `Ingredient.DefaultNutritionId → Nutrition` (КБЖУ по умолчанию — вспомогательное поле, ингредиент без неё остаётся валиден). Требование: FK-поле объявлено как `nullable`.
- **`Cascade`** — для **owned/composition-связей внутри одного агрегата**. Удаление родителя = удаление потомков. Примеры: `IngredientSpec.IngredientId → Ingredient` (spec — часть родителя, отдельно не существует), `Dish → Recipe`, `Recipe → RecipeStep`, `Recipe → RecipeIngredient`. Применяется только внутри одного модуля и внутри одного агрегата (ADR-0006).
- **`UNIQUE`-индекс** — для **1:1 связей**, реализуемых через FK + уникальный индекс на FK-колонке. Пример: `IngredientSpec.NutritionId` (у каждой spec — свой собственный Nutrition-объект, разделять нельзя). В связке с `Restrict` — гарантия «один-к-одному» на уровне БД.

**Правило Code Review:** при добавлении новой FK-связи внутри модуля явно проговорить в PR-описании выбранную стратегию OnDelete и обосновать, почему выбрана именно она. Стратегия по умолчанию EF Core (`Cascade` для required FK, `ClientSetNull` для optional) редко подходит для доменных сущностей.

#### CHECK-constraints как defense in depth

Условные инварианты сущностей (типа «если поле A = true, то поле B обязательно заполнено») защищаются **на двух уровнях одновременно**:

- **Application (FluentValidation)** — понятная ошибка пользователю с локализованным сообщением при попытке ввести некорректные данные. Основной путь проверки.
- **БД (CHECK-constraint)** — последняя линия защиты от багов Application-слоя (обход валидатора, прямая запись в БД миграциями, race-condition). Не заменяет FluentValidation, а страхует.

Примеры реализованных CHECK-ограничений в модуле Dishes:

- `Ingredient`: `"IsLiquid" = false OR "DensityApprox" IS NOT NULL` (жидкость обязана иметь плотность).
- `Ingredient`: `"IsAllergen" = false OR "AllergenType" IS NOT NULL` (аллерген обязан иметь тип).
- `RecipeIngredient`: `("IngredientId" IS NOT NULL AND "FreeformText" IS NULL) OR ("IngredientId" IS NULL AND "FreeformText" IS NOT NULL)` — XOR между ссылкой на справочник и свободным вводом (ADR-0012).

Реализация через `builder.ToTable("Xxx", t => t.HasCheckConstraint("CK_Xxx_YYY", "..."))` в EF Core 8 (в EF Core 7 constraint объявлялся на уровне entity builder — синтаксис изменился). Имена — по конвенции `CK_<Table>_<Reason>`. В SQL-выражении имена колонок с PascalCase **обязательно** в двойных кавычках, иначе PostgreSQL приводит их к lowercase и не находит поле.

**Правило Code Review:** для каждого условного инварианта в Domain-сущности — соответствующий CHECK-constraint в EF-конфигурации. Не полагаться только на валидатор; не полагаться только на БД.

#### DEFERRABLE UNIQUE для переставляемых значений

Если уникальная колонка **переставляется** внутри набора (например, `Order` позиций рецепта при reorder), обычный UNIQUE-индекс в EF-модели заставляет EF строить циклические зависимости UPDATE-ов при swap. Решение — `UNIQUE ... DEFERRABLE INITIALLY DEFERRED` raw SQL-ом в миграции (проверка в конце транзакции) с **удалением** уникального индекса из EF-модели. Применено к `(RecipeId, Order)` на `RecipeSteps` и `RecipeIngredients` (миграция `RecipeOrderDeferrableUnique`).

---

## 6. Future Scope (Будущие направления)

- **Этап 8+:** Read-реплики PostgreSQL для read-heavy модулей (Dishes каталог, Search). Подключение через отдельный read-only connection string.
- **Этап 8+:** Redis-кеширование для горячих данных (каталог, тарифы подписок, сессии).
- **При выделении в микросервисы:** `pg_dump -n {schema}` → отдельная БД. Connection string модуля меняется в appsettings. EF Core миграции продолжают работать.
- **PostgreSQL кластер:** Patroni + etcd или Managed PostgreSQL в Yandex Cloud для автоматического failover.

---

## 7. Implementation Reference (Связь с кодовой базой)

- `docker-compose.yml` — PostgreSQL 16 alpine, порт 5433, named volume `postgres-data`, healthcheck через `pg_isready`.
- `appsettings.Development.json` — единая строка подключения `ConnectionStrings.Database` (host/port 5433/база), общая для всех DbContext.
- `src/Modules/Auth/...Infrastructure/Persistence/AuthDbContext.cs` — `HasDefaultSchema("auth")`.
- `src/Modules/Users/...Infrastructure/Persistence/UsersDbContext.cs` — `HasDefaultSchema("users")`.
- `src/Modules/Dishes/...Infrastructure/Persistence/DishesDbContext.cs` — `HasDefaultSchema("dishes")`.
- `src/Modules/Media/...Infrastructure/Persistence/MediaDbContext.cs` — `HasDefaultSchema("media")`.
- Каждый Infrastructure-проект содержит `{ModuleName}DbContextFactory : IDesignTimeDbContextFactory<{ModuleName}DbContext>`.
- Примеры OnDelete-стратегий: `src/Modules/Dishes/.../Infrastructure/Persistence/Configurations/IngredientConfiguration.cs` (`Restrict` на `BaseMeasureUnitId`, `SetNull` на `DefaultNutritionId`), `IngredientSpecConfiguration.cs` (`Cascade` на `IngredientId`, `Restrict` + UNIQUE на `NutritionId`).
- Примеры CHECK-constraints: `src/Modules/Dishes/.../Infrastructure/Persistence/Configurations/IngredientConfiguration.cs` (`CK_Ingredients_LiquidDensity`, `CK_Ingredients_AllergenType`).

---

## История изменений

- **2026-02-23:** Accepted. Стратегия данных определена при проектировании архитектуры (v0.0.1).
- **2026-03-09:** PostgreSQL развёрнут в docker-compose (v0.3.0), connection string прописан в appsettings.
- **2026-03-30:** Первый DbContext (`AuthDbContext`, схема `auth`) реализован на Этапе 1 (v0.5.0).
- **2026-05-10:** Расширен раздел Areas of Caution: систематизированы OnDelete-стратегии для FK внутри модуля (Restrict/SetNull/Cascade/UNIQUE) и добавлен параграф про CHECK-constraints как defense in depth (примеры из модуля Dishes v0.8.0).
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`. XOR-CHECK на `RecipeIngredient` отмечен реализованным. Добавлен параграф про DEFERRABLE UNIQUE для переставляемых значений (`Order` позиций рецепта, 2026-08-01).
