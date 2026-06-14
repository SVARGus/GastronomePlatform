# UC-DSH-011: Изменить главное фото блюда

**Version:** 1.0 | **Date:** 2026-06-13

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.

---

## Resource (Ресурс)

- Entity: `Dish` (поле `MainImageId`).
- Identifier: `Dish.Id` (path-параметр).
- Action: Update (одно поле + межмодульная синхронизация Media).

---

## Security (Безопасность)

### Authentication

Required. `[Authorize(Policy = VALID_ACTOR)]`.

### Authorization

POL-001 Dish Ownership: автор блюда или роль `Admin`.

Дополнительно: указанный медиафайл должен принадлежать пользователю (`MediaFile.OwnerUserId == actorUserId`), либо быть системным с `Admin`-вызовом. Проверяется внутри `IMediaService.AttachToEntityAsync`.

---

## API Contract

### Endpoint

```
PATCH /api/dishes/{id}/main-image
```

**Path Parameters:** `id` — `Guid` блюда.

**Body:**

```json
{ "mainImageId": "00000000-0000-0000-0000-000000000000" }
```

или для очистки:

```json
{ "mainImageId": null }
```

| Поле | Тип | Обязательно | Ограничения |
|---|---|---|---|
| `mainImageId` | `Guid?` | Нет | Если задан — не `Guid.Empty`; медиафайл должен существовать, быть в статусе `Ready`, не привязан к другой сущности, принадлежать пользователю (POL-003). |

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 400 | `VALIDATION.ERROR` | Структурные ошибки (например, `Guid.Empty`). |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 403 | `MEDIA.NOT_OWNED` | Медиафайл принадлежит другому пользователю (или системный без Admin). |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо не существует. |
| 404 | `MEDIA.NOT_FOUND` | Медиафайл не существует или удалён. |
| 409 | `MEDIA.NOT_READY` | Медиафайл в статусе `Uploaded` / `Processing` / `Failed`. |
| 409 | `MEDIA.ALREADY_ATTACHED` | Медиафайл уже привязан к другой сущности. |

---

## Preconditions

- Блюдо существует, POL-001 пройден.
- (Если `mainImageId != null`) медиафайл существует, в статусе `Ready`, не привязан к другому, принадлежит пользователю.

---

## Invariants

- Главное фото — отдельная семантика от полей карточки. UC-DSH-002 (`UpdateDishCard`) **не** трогает `MainImageId`.
- При смене старый файл становится orphan (привязка снимается, `ExpiresAt` восстанавливается).
- Поле `MainImageId` не привязано к `PublishedVersionData` — изменение не каскадирует на публичный снепшот до повторной публикации (`UC-DSH-004`).

---

## Main Flow

1. Клиент шлёт `PATCH /api/dishes/{id}/main-image` с телом.
2. FluentValidation → `400` при структурных ошибках.
3. Handler загружает блюдо.
4. POL-001 — `403` при провале.
5. Запоминается `oldMainImageId`.
6. Domain `Dish.ChangeMainImage(newMainImageId, utcNow)` — поле обновлено, `UpdatedAt` сдвинут, поднят `DishUpdatedEvent`.
7. Синхронизация Media через `IMediaService` по разнице old/new:
   - `old != null && new == null` → detach старого.
   - `old == null && new != null` → attach нового.
   - `old != null && new != null && разные` → detach старого + attach нового.
   - Равенство → пропуск.
8. Любая ошибка Media → `return Failure` (Dishes ещё не сохранён).
9. `SaveChangesAsync` + публикация событий через `IDomainEventDispatcher`.
10. Ответ `204`.

---

## Alternative Flows

Нет.

---

## Edge Cases

- **EC-1: Очистка `mainImageId`.** При смене с непустого на `null` старый файл становится orphan; новой привязки не происходит. Domain не возвращает ошибку — поле опционально на уровне сущности.
- **EC-2: Повторный вызов с тем же значением.** `oldMainImageId == newMainImageId` → Media-операции пропускаются. Domain всё равно вызывается и сдвигает `UpdatedAt` + поднимает `DishUpdatedEvent` (по дизайну Domain не различает «изменение поля» и «повторное присвоение того же»).
- **EC-3: Consistency Этапа 2.** Если attach Media успешен, но `SaveChanges` Dishes падает — медиафайл остаётся привязан к Dish, который сам в БД не изменился. Известный долг; orphan-cleanup (UC-MED-210, Этап 8+).

---

## Postconditions

- `Dish.MainImageId` равен значению команды.
- `Dish.UpdatedAt = utcNow`.
- Поднят `DishUpdatedEvent`.
- В Media: предыдущий медиафайл (если был) — orphan; новый (если был задан) — `EntityType = "Dish"`, `EntityId = DishId`, `AttachedAt = utcNow`, `ExpiresAt = NULL`.
- `Dish.PublishedVersionData` не изменён.

---

## Non-Functional

- **Idempotency.** Идемпотентен по конечному состоянию: повторный одинаковый запрос даёт тот же state Dishes и Media.
- **Performance.** Целевое < 150 мс. До 3 запросов: GET Dish, attach Media (включая GET MediaFile + UPDATE), SaveChanges Dishes.
- **Consistency.** Две БД-транзакции (Dishes и Media). Согласованность — best-effort; orphan-cleanup для висячих ссылок.

---

## Связанные документы

- POL-001 — Dish Ownership Policy.
- POL-003 — Media Ownership Policy (UC-MED-200 IMediaService.AttachToEntityAsync).
- `docs/public/modules/dishes/domain-model.md` — `Dish.ChangeMainImage`, `Dish.CheckCanPublish` (требование `MainImageId` для публикации).
- `docs/public/modules/media/domain-model.md` — `MediaFile.AttachToEntity`, `DetachFromEntity`.
- UC-DSH-004 — `PublishDish` (требует `MainImageId IS NOT NULL`).
