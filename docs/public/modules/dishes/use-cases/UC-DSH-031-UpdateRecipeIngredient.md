# UC-DSH-031: Обновить ингредиент в рецепте

**Version:** 1.0 | **Date:** 2026-06-07

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `RecipeIngredient` (часть агрегата `Dish` → `Recipe`).
- Identifier: `Dish.Id` (`Guid`) + `RecipeIngredient.Id` (`Guid`) — оба path-параметры.
- Action: Update (full replace полей одним запросом). Допускает смену источника catalog↔freeform.

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required. `[Authorize(Policy = VALID_ACTOR)]`.

### Authorization (Авторизация)

POL-001: автор блюда или Admin.

### State Constraints (Ограничения по состоянию)

См. UC-DSH-030.

---

## API Contract (Контракт API)

### Endpoint

```
PUT /api/dishes/{id}/recipe/ingredients/{recipeIngredientId}
```

**Body:**

```json
{
  "ingredientId": "00000000-0000-0000-0000-000000000000",
  "ingredientSpecId": null,
  "freeformText": null,
  "quantity": 150.0,
  "measureUnitId": "00000000-0000-0000-0000-000000000000",
  "isOptional": false,
  "preparationNote": "комнатной температуры"
}
```

Ровно одно из `ingredientId` или `freeformText` должно быть задано (XOR). `ingredientSpecId` допустим только при заполненном `ingredientId`.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 400 | `VALIDATION.ERROR` | XOR-нарушение, `Quantity <= 0`, длина текстовых полей, пустые `Guid`. |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 404 | `DISHES.RECIPE_INGREDIENT_NOT_FOUND` | Позиция отсутствует в рецепте. |
| 404 | `DISHES.INGREDIENT_NOT_FOUND` | Новый ингредиент отсутствует. |
| 404 | `DISHES.INGREDIENT_SPEC_NOT_FOUND` | Спецификация отсутствует. |
| 404 | `DISHES.MEASURE_UNIT_NOT_FOUND` | Единица измерения отсутствует. |
| 409 | `DISHES.INGREDIENT_INACTIVE` | Ингредиент деактивирован. |
| 409 | `DISHES.INGREDIENT_SPEC_MISMATCH` | Спецификация принадлежит другому ингредиенту. |
| 409 | `DISHES.INVALID_INGREDIENT_COMPOSITION` | Domain-валидация XOR провалена (если каким-то образом обошла валидатор). |
| 409 | `DISHES.INVALID_QUANTITY` | Domain-валидация `Quantity > 0` провалена. |

---

## Preconditions (Предусловия)

- Блюдо и позиция существуют и доступны текущему пользователю по POL-001.
- Новые источники (Ingredient, Spec, MeasureUnit) существуют и активны (где применимо).

---

## Invariants (Инварианты домена)

- XOR `IngredientId` ↔ `FreeformText`.
- `IngredientSpecId` только при заполненном `IngredientId`.
- `Quantity > 0`.
- ADR-0016 (см. UC-DSH-030).

---

## Main Flow (Основной поток)

1. FluentValidation → 400.
2. Загрузка блюда с полным рецептом.
3. POL-001.
4. Проверка наличия позиции с `RecipeIngredientId` в `Recipe.Ingredients`.
5. Проверка справочников для новых значений (Ingredient + Spec + MeasureUnit).
6. `Dish.UpdateRecipeIngredient(...)` → Domain валидирует XOR и `Quantity`, обновляет поля позиции; `MarkAsUpdated`.
7. Сбор словаря маркеров по текущему составу (catalog-позиции).
8. `Dish.RecalculateDishMarkers(...)` → пересчёт `AllergensMask`, `HasUnverifiedAllergens`, авто-clear конфликтующих диет-меток.
9. `SaveChangesAsync` + публикация событий.
10. `204 No Content`.

---

## Edge Cases

- **EC-1: Смена catalog → freeform.** Позиция теряет связку с `IngredientId` и `IngredientSpecId`; в `RecalculateDishMarkers` она больше не вносит вклад в `AllergensMask` и поднимает `HasUnverifiedAllergens`.
- **EC-2: Смена freeform → catalog.** Симметрично — позиция получает справочные маркеры; `HasUnverifiedAllergens` может опуститься, если других freeform-позиций нет.
- **EC-3: Двойной `DishUpdatedEvent`.** См. UC-DSH-030 EC-3.

---

## Postconditions

- Позиция `RecipeIngredient` обновлена; `Order` не меняется.
- `Dish.UpdatedAt = utcNow`.
- `Dish.AllergensMask`, `Dish.HasUnverifiedAllergens`, `Dish.DietLabelsMask` пересчитаны.
- `Dish.PublishedVersionData` не изменён.

---

## Non-Functional

- **Idempotency.** Идемпотентен относительно одинакового запроса (полная замена полей — повторный вызов с теми же данными ничего не меняет, кроме `UpdatedAt`).
- **Performance.** Целевое < 100 мс. До 5 SELECT-запросов + 1 UPDATE + 1 UPDATE на `Dish.UpdatedAt`.

---

## Связанные документы

См. UC-DSH-030.
