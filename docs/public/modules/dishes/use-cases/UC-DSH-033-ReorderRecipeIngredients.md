# UC-DSH-033: Переупорядочить ингредиенты рецепта

**Version:** 1.0 | **Date:** 2026-06-07

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.

---

## Resource (Ресурс)

- Entity: коллекция `Recipe.Ingredients` (поле `Order` каждой `RecipeIngredient`).
- Identifier: `Dish.Id` — path; список `RecipeIngredient.Id` в желаемом порядке — body.
- Action: Update (массовая правка `Order`).

---

## Security (Безопасность)

### Authentication

Required. `[Authorize(Policy = VALID_ACTOR)]`.

### Authorization

POL-001: автор или Admin.

---

## API Contract (Контракт API)

### Endpoint

```
PUT /api/dishes/{id}/recipe/ingredients/order
```

**Body:**

```json
{
  "orderedIngredientIds": [
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002",
    "00000000-0000-0000-0000-000000000003"
  ]
}
```

Список должен содержать **все** `Id` позиций рецепта без дубликатов и без пропусков.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 400 | `VALIDATION.ERROR` | Пустой список, `Guid.Empty` в списке. |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 404 | `DISHES.RECIPE_INGREDIENT_NOT_FOUND` | В списке есть `Id`, отсутствующий в рецепте. |
| 409 | `DISHES.INVALID_INGREDIENT_ORDER` | Длина списка не совпадает с количеством позиций или есть дубликаты. |

---

## Preconditions

- Блюдо существует; доступ по POL-001.
- Список совпадает по объёму с `Recipe.Ingredients.Count` и содержит ровно те же `Id`.

---

## Invariants

- После операции `Order` позиций — `1..n` строго в соответствии с переданным порядком.
- Состав не меняется — маркеры и диет-метки **не пересчитываются**.

---

## Main Flow

1. FluentValidation → 400.
2. Загрузка блюда с полным рецептом.
3. POL-001.
4. `Dish.ReorderRecipeIngredients(orderedIngredientIds, utcNow)` →
   - `Recipe.ReorderIngredients` валидирует длину, отсутствие дубликатов, полное покрытие;
   - перенумерация позиций `1..n` в порядке списка;
   - `MarkAsUpdated`.
5. `SaveChangesAsync` + публикация событий.
6. `204 No Content`.

---

## Edge Cases

- **EC-1: Список без изменений (тот же порядок, что сейчас).** Операция формально выполняется; `Order` записей перезаписываются теми же значениями. `Dish.UpdatedAt` всё равно обновляется.
- **EC-2: Конкурентный Add + Reorder.** Если параллельный Add добавил новую позицию между загрузкой и сохранением — `SaveChangesAsync` отработает оптимистичной/обычной транзакцией. Возможный исход — `Order` новой позиции окажется `max+1`, не вписанной в порядок. На Этапе 2 не защищаемся; полноценная защита — отдельный ETag/RowVersion.

---

## Postconditions

- Поле `Order` всех позиций — `1..n` в соответствии с запросом.
- `Dish.UpdatedAt = utcNow`.
- `AllergensMask`, `DietLabelsMask`, `HasUnverifiedAllergens` — без изменений.
- `Dish.PublishedVersionData` не изменён.

---

## Non-Functional

- **Idempotency.** Идемпотентен — повторный вызов с тем же списком в стабильно отсортированном рецепте оставляет состояние неизменным (кроме `UpdatedAt`).
- **Performance.** Целевое < 70 мс. 1 SELECT + N UPDATE на `Order` + 1 UPDATE на `Dish.UpdatedAt`.

---

## Связанные документы

См. UC-DSH-030.
