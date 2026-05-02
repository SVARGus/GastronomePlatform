# Use Cases — Модуль Media

> **Версия:** 1.0 (Этап 2)
> **Последнее изменение:** 2026-05-02
> **Статус:** Проектирование
> **Связанные документы:** `domain-model.md` (модуль Media), `domain-model.md` (модуль Dishes), `POL-002-media-access.md`, `POL-003-media-ownership.md`

Полный список сценариев модуля Media. Каждый Use Case описывается отдельным файлом по шаблону [UseCase-template.md](../../../DocTemplates/UseCase-template.md) **по мере реализации**. Этот документ — навигационный индекс с краткими описаниями.

---

## Легенда статусов

| Статус | Значение |
|--------|----------|
| **Core** | Реализуем на Этапе 2 |
| **Drafted** | Зафиксирован, реализация на указанном этапе |
| **Deferred** | Дальняя перспектива |

---

## Сводная таблица всех UC

| ID | Название | Тип | Статус | Этап | Описание |
|----|----------|-----|--------|------|----------|
| UC-MED-001 | Загрузить файл (пользователь) | Cmd | Core | 2 | JPEG/PNG, до 10 MB, 100×100 — 4096×4096 px |
| UC-MED-002 | Получить файл по ID | Qry | Core | 2 | Стриминг через контроллер |
| UC-MED-003 | Получить миниатюру файла | Qry | Core | 2 | По размеру и формату |
| UC-MED-004 | Получить метаданные файла | Qry | Core | 2 | Без содержимого |
| UC-MED-005 | Удалить свой файл (пользователь) | Cmd | Core | 2 | Soft delete; только владелец |
| UC-MED-006 | Получить мои файлы | Qry | Drafted | 8+ | Список + сводный объём |
| UC-MED-101 | Загрузить системный файл (admin) | Cmd | Core | 2 | Иконки категорий, фото ингредиентов; SVG разрешён |
| UC-MED-102 | Удалить любой файл (admin) | Cmd | Core | 2 | Soft delete без проверки владельца |
| UC-MED-103 | Получить файлы пользователя (admin) | Qry | Core | 2 | Аудит, экспорт по 152-ФЗ |
| UC-MED-200 | Получить метаданные файла (внутренний) | Internal | Core | 2 | `IMediaService.GetMetadataAsync` |
| UC-MED-201 | Batch-получение метаданных (внутренний) | Internal | Core | 2 | `IMediaService.GetMetadataBatchAsync` |
| UC-MED-202 | Привязать файл к сущности (внутренний) | Internal | Core | 2 | `IMediaService.AttachToEntityAsync` |
| UC-MED-203 | Отвязать файл от сущности (внутренний) | Internal | Core | 2 | `IMediaService.DetachFromEntityAsync` |
| UC-MED-204 | Удалить файлы сущности (внутренний) | Internal | Core | 2 | `IMediaService.DeleteByEntityAsync` |
| UC-MED-210 | Очистка файлов-сирот (фоновая) | Job | Drafted | 8+ | Hosted service; поиск по `EntityType IS NULL AND ExpiresAt < now` |
| UC-MED-211 | Физическое удаление soft-deleted файлов (фоновая) | Job | Drafted | 8+ | Hosted service; через 7 дней после `DeletedAt` |
| UC-MED-212 | Проверка целостности кросс-модульных ссылок (фоновая) | Job | Drafted | 8+ | Проверка существования EntityId |
| UC-MED-213 | Асинхронная генерация thumbnails (фоновая) | Job | Drafted | 8+ | Через RabbitMQ |

---

## Группировка по подразделам

### 001–099. Пользовательские сценарии

##### UC-MED-001 — Загрузить файл (пользователь)

**Тип:** Command. **Статус:** Core. **Этап:** 2.

Multipart upload. Валидации: MIME ∈ {jpeg, png}, magic bytes, размер 5 KB – 10 MB, dimensions 100×100 – 4096×4096, запрет EXE-сигнатур. После валидации: сохранение через `IFileStorage`, синхронная генерация Medium thumbnail (400×400 JPEG), создание `MediaFile` со статусом Ready, `ExpiresAt = now + 24h`. Возвращает `MediaId`. Файл — orphan (EntityType IS NULL) до момента attach.

##### UC-MED-002 — Получить файл по ID

**Тип:** Query. **Статус:** Core. **Этап:** 2.
**Authorization:** POL-002.

`GET /api/media/{id}`. Стриминг файла через контроллер. Заголовки: `Content-Type`, `Content-Length`, `Cache-Control`, `ETag`, `Last-Modified`.

**Доступ (Этап 2):**
- `DataCategory = Public` (Dish.MainImage, RecipeStep.Image, CategoryIcon, IngredientImage) — отдаём всем, включая **гостей**. На Этапе 3+ для RecipeStep.Image добавится проверка подписки через `ISubscriptionService`.
- `DataCategory = Personal` (UserAvatar) — оригинал только **авторизованным** пользователям. Миниатюры (UC-MED-003) — всем.

**Файлы со статусом `Deleted` / `Failed`** — 404. Файлы со статусом `Uploaded` / `Processing` — 425 Too Early (или 404, см. Q в открытых вопросах).

##### UC-MED-003 — Получить миниатюру файла

**Тип:** Query. **Статус:** Core. **Этап:** 2.
**Authorization:** POL-002.

`GET /api/media/{id}/thumbnail?size=medium&format=jpeg`. На Этапе 2 — только Medium/JPEG. Если запрошенной миниатюры нет — 404 (не пытаемся сгенерировать налету; см. Q-2).

**Доступ:** миниатюры **отдаются всем** (включая гостей), независимо от `DataCategory`. Это компромисс между приватностью и UX — для аватара пользователя гость видит миниатюру, но не оригинал. На Этапе 3+ для миниатюр Premium-контента может появиться отдельная проверка подписки.

##### UC-MED-004 — Получить метаданные файла

**Тип:** Query. **Статус:** Core. **Этап:** 2.
**Authorization:** POL-002 (метаданные следуют тем же правилам, что и сам файл).

`GET /api/media/{id}/metadata`. Возвращает Width, Height, ContentType, SizeBytes, Status, AttachedTo (EntityType + EntityId). Без бинарного содержимого. Используется UI и другими клиентами.

##### UC-MED-005 — Удалить свой файл (пользователь)

**Тип:** Command. **Статус:** Core. **Этап:** 2.
**Authorization:** POL-003.

Soft delete. Status → Deleted, DeletedAt = now. Если файл всё ещё привязан к сущности (`EntityType IS NOT NULL`) — ошибка `MEDIA.STILL_ATTACHED`: сначала нужно открепить от сущности (или удалить сущность). Физическое удаление — фоновой задачей UC-MED-211.

##### UC-MED-006 — Получить мои файлы

**Тип:** Query. **Статус:** Drafted. **Этап:** 8+.
**Authorization:** Authenticated (только владелец видит свой список).

Список MediaFile пользователя с пагинацией. Фильтры: по EntityType, по статусу. Используется для управления хранилищем («очистить мои файлы»). Также сводная статистика — суммарный объём, кол-во файлов.

---

### 100–199. Админские сценарии

##### UC-MED-101 — Загрузить системный файл (admin)

**Тип:** Command. **Статус:** Core. **Этап:** 2.
**Authorization:** только роль `Admin`.

Отдельный эндпоинт `POST /api/media/system/upload`. Расширенный whitelist MIME: JPEG, PNG, **SVG** (с обязательной sanitization через `HtmlSanitizer`). MaxSize 2 MB. `OwnerUserId = NULL`, `DataCategory = Public`. Используется для иконок категорий, фото ингредиентов.

##### UC-MED-102 — Удалить любой файл (admin)

**Тип:** Command. **Статус:** Core. **Этап:** 2.
**Authorization:** только роль `Admin`.

Soft delete без проверки владельца. Используется для удаления нарушающего контент или системных ошибок. Если файл привязан к сущности — отвязка через `DetachFromEntityAsync` + удаление.

##### UC-MED-103 — Получить файлы пользователя (admin)

**Тип:** Query. **Статус:** Core. **Этап:** 2.
**Authorization:** только роль `Admin`.

`GET /api/media/admin/users/{userId}/files?status=...&entityType=...`. Возвращает все файлы с `OwnerUserId = userId`. Поддерживает пагинацию и фильтры по `Status`, `EntityType`. Используется для:

- Аудита персональных данных пользователя.
- Экспорта PII по запросу пользователя (152-ФЗ).
- Решения проблем поддержки («где мой загруженный файл»).

Реализация дешёвая — один SQL-запрос с фильтром по `OwnerUserId` (есть индекс).

---

### 200–299. Внутренние и системные сценарии

#### Внутренний контракт IMediaService (вызывается из других модулей)

##### UC-MED-200 — Получить метаданные файла (внутренний)

**Тип:** Internal. **Статус:** Core. **Этап:** 2.

`IMediaService.GetMetadataAsync(mediaId)`. Используется другими модулями (Dishes при отдаче карточки), чтобы получить размеры/тип без бинарника.

##### UC-MED-201 — Batch-получение метаданных (внутренний)

**Тип:** Internal. **Статус:** Core. **Этап:** 2.

`IMediaService.GetMetadataBatchAsync(mediaIds)`. Минимизация N+1 при отдаче списков (каталог блюд с MainImage). Один SQL-запрос с `WHERE Id = ANY(@ids)`.

##### UC-MED-202 — Привязать файл к сущности (внутренний)

**Тип:** Internal. **Статус:** Core. **Этап:** 2.

`IMediaService.AttachToEntityAsync(mediaId, actorUserId, entityType, entityId)`. Eager attach из Dishes/Users handler'ов.

**Проверки владения:**
- Существование файла → иначе `MEDIA.NOT_FOUND`.
- `Status = Ready` → иначе `MEDIA.NOT_READY`.
- Файл ещё не привязан → иначе `MEDIA.ALREADY_ATTACHED` (предотвращает «угон» чужого файла).
- **Соответствие владельца:**
  - Если `media.OwnerUserId IS NOT NULL` → `actorUserId == media.OwnerUserId` → иначе `MEDIA.NOT_OWNED`.
  - Если `media.OwnerUserId IS NULL` (системный файл) → `actorUserId` имеет роль `Admin` → иначе `MEDIA.NOT_OWNED`.
- `EntityType` ∈ известный список (`MediaEntityTypes`) → иначе `MEDIA.UNKNOWN_ENTITY_TYPE`.

При успехе: `EntityType = entityType`, `EntityId = entityId`, `ExpiresAt = NULL`, `AttachedAt = now`.

> **Примечание:** эта проверка не выделена в отдельную политику авторизации. Авторизация на уровне UC модуля-вызывающего (например, в UC-DSH-002 проверяется POL-001, и только потом вызывается `AttachToEntityAsync`). Логика владения медиа — часть внутреннего контракта `IMediaService`.

##### UC-MED-203 — Отвязать файл от сущности (внутренний)

**Тип:** Internal. **Статус:** Core. **Этап:** 2.

`IMediaService.DetachFromEntityAsync(mediaId)`. Используется при смене главного фото блюда, удалении шага рецепта, смене аватара. EntityType/EntityId обнуляются, ExpiresAt = now + 24h. Файл становится orphan.

##### UC-MED-204 — Удалить файлы сущности (внутренний)

**Тип:** Internal. **Статус:** Core. **Этап:** 2.

`IMediaService.DeleteByEntityAsync(entityType, entityId)`. Каскадное soft-удаление при удалении Dish, UserProfile и т.п. Все файлы с подходящим `EntityType + EntityId` → Status = Deleted.

#### Фоновые задачи

##### UC-MED-210 — Очистка файлов-сирот (фоновая)

**Тип:** Job. **Статус:** Drafted. **Этап:** 8+.

Hosted Service, запускается каждые 4 часа. Запрос: `MediaFile WHERE EntityType IS NULL AND ExpiresAt < now AND Status = Ready` → soft delete. Используется частичный индекс `(Status, ExpiresAt) WHERE EntityType IS NULL`.

##### UC-MED-211 — Физическое удаление soft-deleted файлов (фоновая)

**Тип:** Job. **Статус:** Drafted. **Этап:** 8+.

Hosted Service, раз в сутки. Запрос: `MediaFile WHERE Status = Deleted AND DeletedAt < now - 7 days`. Удаляет файл из IFileStorage, затем DELETE из БД. 7 дней — окно на восстановление при ошибочном удалении.

##### UC-MED-212 — Проверка целостности кросс-модульных ссылок (фоновая)

**Тип:** Job. **Статус:** Drafted. **Этап:** 8+.

Еженедельно: для каждого MediaFile с непустым EntityType проверяет существование EntityId в целевой схеме. Несуществующие → soft delete. Балансирует отсутствие FK-constraints.

##### UC-MED-213 — Асинхронная генерация thumbnails (фоновая)

**Тип:** Job. **Статус:** Drafted. **Этап:** 8+.

После публикации `MediaUploadedEvent` отдельный consumer генерирует Small/Medium/Large × Jpeg/WebP/Avif. Этап 2 — синхронно в момент upload, только Medium/JPEG.

---

