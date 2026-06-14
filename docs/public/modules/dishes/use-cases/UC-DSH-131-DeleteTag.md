# UC-DSH-131: Удалить тег (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Hard delete тега. Каскадно удаляются связки `DishTag` (рабочая копия) и `DishTagPublished` (опубликованная). Используется для очистки спама/мата.

У всех блюд, к которым был привязан удаляемый тег в рабочей копии, обновляется `Dish.UpdatedAt` — индикатор «есть несохранённые правки» сработает, автор узнает о расхождении при следующем заходе в редактор.

## Actors

- Администратор платформы. Роль `PlatformRoles.ADMIN`.

## Authorization

- `[Authorize(Roles = PlatformRoles.ADMIN)]` на эндпоинте.

## API Contract

**Endpoint:** `DELETE /api/tags/{id}`

**Request:** `id` — `Guid` (path); тело не требуется.

**Response:** `204 No Content`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | `Guid.Empty`. |
| 401  | —                      | Нет JWT. |
| 403  | —                      | Пользователь без роли `Admin`. |
| 404  | `DISHES.TAG_NOT_FOUND` | Тег не существует. |

## Реализация

1. `ITagRepository.GetByIdAsync` → 404 при `null`.
2. `ITagRepository.RemoveWithLinksAsync(tagId)`:
   - Считывает `DishTag.DishId` затронутых блюд (для шага 3).
   - `ExecuteDeleteAsync` по `DishTag WHERE TagId = ?`.
   - `ExecuteDeleteAsync` по `DishTagPublished WHERE TagId = ?`.
   - `ExecuteDeleteAsync` по `Tags WHERE Id = ?`.
3. Если есть затронутые блюда — `IDishRepository.BulkMarkAsUpdatedAsync(dishIds, utcNow)` через `ExecuteUpdateAsync` (одним SQL).

## Не реализовано / Компромиссы

- **`DishUpdatedEvent` не поднимается** для затронутых блюд. На Этапе 2 подписчиков нет, а грузить десятки агрегатов ради события — дорого. При появлении обработчиков (Этап 5+ — счётчики, инвалидация кэшей) потребуется механизм рассылки событий после batch-update.
- **`DishTagPublished` для опубликованных блюд:** удаляется, но `PublishedVersionData` (jsonb-snapshot) остаётся прежней и в каталоге продолжает показывать имя тега. Перерасчёт snapshot — отдельная фоновая задача (UC-DSH-132 MergeTags / журнал `DishSnapshotInvalidation`, Этап 8+).
- **Транзакция:** все `ExecuteDeleteAsync` и `BulkMarkAsUpdatedAsync` идут отдельными SQL без явной транзакции — при ошибке между шагами возможно частичное состояние. Допустимо для admin-операции редкой частоты.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-008-SetTags.md` — пользовательское изменение набора тегов.
- `docs/public/modules/dishes/use-cases/UC-DSH-130-VerifyTag.md` — admin-верификация.
- `docs/public/modules/dishes/use-cases/UC-DSH-132-...` (Drafted, Этап 8+) — admin-объединение тегов с пересборкой `PublishedVersionData`.
