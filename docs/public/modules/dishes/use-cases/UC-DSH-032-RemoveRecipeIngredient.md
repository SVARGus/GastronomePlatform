# UC-DSH-032: Удалить ингредиент из рецепта

**Version:** 1.0 | **Date:** 2026-06-07

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `RecipeIngredient`.
- Identifier: `Dish.Id` + `RecipeIngredient.Id` — оба path-параметры.
- Action: Delete (с переупорядочиванием оставшихся).

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
DELETE /api/dishes/{id}/recipe/ingredients/{recipeIngredientId}
```

Тело не требуется.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 404 | `DISHES.RECIPE_INGREDIENT_NOT_FOUND` | Позиция отсутствует в рецепте. |

---

## Preconditions

- Блюдо и позиция существуют; доступ по POL-001.

---

## Invariants

- ADR-0016: после удаления маркеры пересчитываются.
- После удаления `Order` оставшихся позиций — `1..n` без пробелов (Domain переупорядочивает).

---

## Main Flow

1. Загрузка блюда с полным рецептом.
2. POL-001.
3. `Dish.RemoveRecipeIngredient(recipeIngredientId, utcNow)` → Domain удаляет позицию, переупорядочивает оставшиеся, поднимает `MarkAsUpdated`.
4. Сбор маркеров по оставшимся catalog-позициям.
5. `Dish.RecalculateDishMarkers(...)`.
6. `SaveChangesAsync` + публикация событий.
7. `204 No Content`.

---

## Edge Cases

- **EC-1: Удаление единственной freeform-позиции.** `HasUnverifiedAllergens` опускается в `false`.
- **EC-2: Удаление ингредиента-аллергена.** `AllergensMask` пересчитывается — может потерять биты, которые приносила только эта позиция.
- **EC-3: Удаление ингредиента, не конфликтующего с диетами.** `DietLabelsMask` не меняется (auto-clear работает только в направлении снятия, не восстановления). При повторной публикации блюдо сохранит свои метки.
- **EC-4: Удаление последней позиции рецепта.** Рецепт остаётся валидным с пустым составом, но повторная публикация будет провалена инвариантом `Recipe.Ingredients.Count > 0` в `Dish.Publish` (UC-DSH-004).
- **EC-5: Двойной `DishUpdatedEvent`.** См. UC-DSH-030 EC-3.

---

## Postconditions

- Запись в `dishes."RecipeIngredients"` удалена; `Order` оставшихся = `1..n`.
- `Dish.UpdatedAt = utcNow`.
- Маркеры аллергенов и диет — пересчитаны.
- `Dish.PublishedVersionData` не изменён.

---

## Non-Functional

- **Idempotency.** Идемпотентен на уровне state (повторное удаление вернёт 404 — это разумное поведение для DELETE).
- **Performance.** Целевое < 80 мс. 2 SELECT (Dish, GetMarkersByIds) + 1 DELETE + 1 UPDATE на оставшиеся (через ChangeTracker) + 1 UPDATE на Dish.UpdatedAt.

---

## Связанные документы

См. UC-DSH-030.
