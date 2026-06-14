# UC-DSH-007: Установить категории блюда

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` (POL-001).

---

## Resource (Ресурс)

- Entity: `Dish` — связки `DishCategory` (M:M).
- Identifier: `Dish.Id` (`Guid`).
- Action: Replace полного набора `DishCategory` (0–3 категории).

---

## Security (Безопасность)

### Authentication

Required (`AuthorizationPolicies.VALID_ACTOR`).

### Authorization — POL-001

- Roles: `Author` или `Admin` (`PlatformRoles.ADMIN`).
- Ownership: `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)`.

### State Constraints

Ограничений по `Dish.Status` нет: править рабочий слой категорий можно в любом статусе кроме `Archived` (формальное правило POL-001 §4.1, пока не вынесено в код).

---

## API Contract

### Endpoint

```
PUT /api/dishes/{id}/categories
```

### Request

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Body (JSON):**

```json
{
  "categoryIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6", "..."]
}
```

- `categoryIds` — `Guid[]`, 0–3 идентификатора активных категорий, без дубликатов.
- Пустой массив `[]` — снять все категории.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`                | Превышен лимит, дубликаты, `Guid.Empty`, `null`. |
| 401  | —                                 | Нет JWT. |
| 403  | `DISHES.NOT_DISH_OWNER`           | Не автор и не Admin. |
| 404  | `DISHES.DISH_NOT_FOUND`           | Блюдо отсутствует. |
| 404  | `DISHES.CATEGORY_NOT_FOUND`       | Одна или несколько категорий не найдены или неактивны. |
| 409  | `DISHES.CATEGORY_LIMIT_EXCEEDED`  | Defense-in-depth от Domain (валидатор не пропустил, но Domain поймал). |
| 409  | `DISHES.DUPLICATE_CATEGORY_ID`    | Аналогично. |

---

## Preconditions

- Запрос аутентифицирован.
- Блюдо существует.
- Пользователь — автор или Admin.
- Все `categoryIds` существуют в справочнике и активны (`Category.IsActive = true`).

---

## Invariants (Инварианты домена)

- 0 ≤ `Dish.Categories.Count` ≤ 3.
- В `Dish.Categories` нет дубликатов по `CategoryId`.
- `Dish.UpdatedAt = utcNow` — устанавливается явным `Dish.MarkAsUpdated`, потому что `SaveChangesInterceptor` не отслеживает изменения связующих таблиц.
- `Dish.CategoriesPublished` **не** трогается — обновится только при следующем `Publish` (UC-DSH-004).
- `PublishedVersionData` не изменяется.
- Поднимается `DishUpdatedEvent`.

---

## Main Flow

1. Автор выбирает 1–3 категории в форме блюда и сохраняет.
2. Клиент шлёт `PUT /api/dishes/{id}/categories` с массивом `categoryIds`.
3. `DishesController.SetCategoriesAsync` создаёт `SetCategoriesCommand` и отправляет в MediatR.
4. `SetCategoriesCommandValidator` проверяет: `DishId` не пуст, `categoryIds` не `null`, размер ≤ 3, нет дубликатов и `Guid.Empty`.
5. `SetCategoriesCommandHandler`:
   - Грузит `Dish` через `IDishRepository.GetByIdWithCategoriesAsync` (`Include(d => d.Categories)`) — без подгрузки EF не отследит удаление связок при `Clear()`.
   - Проверяет POL-001.
   - Если набор непустой — `ICategoryRepository.ListByIdsAsync(uniqueIds, ct)`. Если `foundCategories.Count != uniqueIds.Count` → `CategoryNotFound`.
   - Вызывает `dish.SetCategories(categoryIds, utcNow)`. Domain очищает `_categories` и заполняет заново, поднимает `DishUpdatedEvent` через `MarkAsUpdated`.
   - `SaveChangesAsync` + `DispatchAsync`.
6. `204 No Content`.

---

## Edge Cases

- **EC-1: Пустой массив `[]`.** Все связки `DishCategory` удаляются. Валидное действие.
- **EC-2: 4 категории.** Валидатор → `400` «У блюда может быть не более 3 категорий.».
- **EC-3: Дубликаты в массиве.** Валидатор → `400` «Список категорий не должен содержать дубликатов.».
- **EC-4: `Guid.Empty` в массиве.** Валидатор → `400` «Идентификаторы категорий не могут быть пустыми.».
- **EC-5: Существующая, но неактивная категория.** `ListByIdsAsync` фильтрует по `IsActive = true` — неактивная не попадёт в результат, `Count` не совпадёт → `404 CATEGORY_NOT_FOUND`. UI должен скрывать неактивные категории.
- **EC-6: Не автор и не Admin.** `403 NOT_DISH_OWNER`.
- **EC-7: Несуществующая категория.** Аналогично EC-5 → `404 CATEGORY_NOT_FOUND`. Единый код — не различаем «не существует» и «неактивна».
- **EC-8: Опубликованное блюдо.** Изменения применяются к рабочей копии. В каталоге блюдо продолжает показывать прежний набор категорий — до `UC-DSH-004 Publish`.
- **EC-9: Конкурентные SetCategories.** Последний коммит выигрывает; OCC не реализован на Этапе 2.

---

## Postconditions

При успехе:

- Записи `DishCategory` для этого блюда переписаны под новый список.
- `Dish.UpdatedAt = utcNow`.
- `DishUpdatedEvent` отправлено.
- `DishCategoryPublished` и `PublishedVersionData` не изменены.

При неуспехе: состояние БД не меняется.

---

## Non-Functional

- **Idempotency.** Идемпотентен: повторный PUT с тем же массивом → повторный `MarkAsUpdated` (новый `UpdatedAt`) и `DishUpdatedEvent`. Состав связок не меняется.
- **Performance.** Целевое < 100 мс. `SELECT` Dish + `Include(Categories)` + `SELECT` Categories по `IN (ids)` + `DELETE/INSERT` в DishCategory.
- **Consistency.** Read Committed, одна транзакция.

---

## Реализация Этапа 2

### Реализовано

- Command + Validator (`.WithMessage`) + Handler.
- Endpoint `PUT /api/dishes/{id:guid}/categories`.
- POL-001 (Author + Admin).
- Новая ошибка `DishesErrors.CategoryNotFound`.
- `IDishRepository.GetByIdWithCategoriesAsync` — целевая подгрузка для replace-семантики.
- `ICategoryRepository.ListByIdsAsync` + полная реализация `CategoryRepository` в Infrastructure (создан вместе с этим UC).
- DI-регистрация `ICategoryRepository → CategoryRepository`.
- Domain-инварианты `Dish.SetCategories` (лимит, дубликаты) — defense-in-depth.

### Отложено

- **OCC через `xmin` / `RowVersion`** — Этап 8+ при появлении конкурентного редактирования.
- **Запрет на правку Archived для Author** — общий долг POL-001 §4.1, выносится отдельным шагом по всем handler-ам модификации Dishes.

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md`.
- `docs/public/modules/dishes/domain-model.md` — `Dish.SetCategories`, `DishCategory`, `DishCategoryPublished`.
- `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — синхронизация `DishCategoryPublished` происходит там.
- `docs/public/modules/dishes/use-cases/UC-DSH-008-SetTags.md` — параллельный UC для M:M-связок тегов (будет реализован следующим).
- `docs/public/modules/dishes/use-cases/UC-DSH-057-...` — чтение справочника категорий.
