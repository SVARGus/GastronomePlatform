# UC-DSH-056: Пересчитать ингредиенты рецепта на N порций

**Version:** 1.1 (Premium-гейт по гранту `PortionCalculator`) | **Date:** 2026-07-18

---

## Назначение

Возвращает список ингредиентов опубликованного рецепта с количествами, пересчитанными на запрошенное число порций. Линейное масштабирование `quantity * (servings / servingsDefault)`.

## Actors

- Аутентифицированный пользователь (`AuthorizationPolicies.VALID_ACTOR`). Гости — `401`.

## Authorization

- Гости — `401` (политика `VALID_ACTOR` на эндпоинте).
- Автор блюда и `Admin` — доступ без проверки подписки.
- Остальным авторизованным требуется грант `PortionCalculator` (POL-004 §4.4), проверка через `ISubscriptionAccessService.HasFeatureAsync`. При отсутствии гранта — `403` с кодом `DISHES.PREMIUM_REQUIRED`.

Порядок проверок в обработчике: валидация параметра `servings` → видимость блюда (`404` для отсутствующего снепшота — всем, включая автора) → грант. То есть по несуществующему или неопубликованному блюду возвращается `404`, а не `403`: наличие подписки не должно менять ответ там, где ресурс недоступен по другой причине.

## API Contract

**Endpoint:** `GET /api/dishes/{id}/recipe/scaled?servings={n}`

**Request:**

- `id` — `Guid` (path).
- `servings` — `int`, 1..1000 (query).

**Response:** `200 OK` с `GetScaledRecipeIngredientsResult`:

```json
{
  "servingsDefault": 4,
  "servingsRequested": 6,
  "multiplier": 1.5,
  "ingredients": [
    {
      "id": "...",
      "order": 1,
      "type": "catalog",
      "ingredientId": "...",
      "ingredientSpecId": null,
      "freeformText": null,
      "originalQuantity": 200,
      "scaledQuantity": 300,
      "measureUnitId": "...",
      "isOptional": false,
      "preparationNote": "мелко нарезанный"
    },
    {
      "id": "...",
      "order": 2,
      "type": "freeform",
      "ingredientId": null,
      "ingredientSpecId": null,
      "freeformText": "укроп от соседки",
      "originalQuantity": 10,
      "scaledQuantity": 15,
      "measureUnitId": "...",
      "isOptional": true,
      "preparationNote": null
    }
  ]
}
```

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | `Guid.Empty`, `servings` вне 1..1000. |
| 401  | —                      | Нет JWT. |
| 404  | `DISHES.DISH_NOT_FOUND`| Блюдо не существует, `Archived` или `PublishedVersionData IS NULL`. |

## Реализация

- `IDishRepository.GetByIdAsync` (без подгрузки `Recipe` — он в snapshot).
- 404 при `null` / `Archived` / `PublishedVersionData IS NULL`.
- Парсинг snapshot через `IPublishedDishSnapshotReader`.
- Расчёт `multiplier = (decimal)servings / servingsDefault`.
- Маппинг каждой позиции snapshot (полиморфно `catalog`/`freeform`) в плоский `ScaledIngredientDto` с дискриминатором `type`.

## Edge Cases

- **EC-1: `servings == servingsDefault`.** `multiplier = 1.0`, `scaledQuantity == originalQuantity`. Допустимо — клиент может всегда дёргать этот эндпоинт.
- **EC-2: Дробный multiplier (`1.5`, `0.333…`).** Без округления; клиент решает, как форматировать.
- **EC-3: `servings = 1000`, исходное `200 г`.** `scaledQuantity = 200000`. Большое, но допустимое.
- **EC-4: Блюдо в `Draft` или `Unpublished`.** `404`, slug привязан к публичной версии. Для редактирования рабочей версии — будущий UC-DSH-083.
- **EC-5: Snapshot без ингредиентов.** Невозможно — `Dish.Publish` требует хотя бы одного `RecipeIngredient`. Если каким-то образом случилось — вернётся пустой массив.
- **EC-6: Запрос для гостя.** `401` от Authorization middleware.

## Не реализовано

- **Конвертация единиц** (Mass ↔ Volume через `Ingredient.DensityApprox`). Клиент получает тот же `MeasureUnitId`, что в snapshot. Реализация — будущий UC/ADR при появлении бизнес-потребности.
- **Округление до «удобных» количеств** (например, `1.5 шт яйца` → `2 шт`). Этап 4+.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-052-GetDishRecipe.md` — родственный UC получения полного рецепта.
- `docs/public/modules/dishes/use-cases/UC-DSH-050-GetDishById.md` — карточка блюда.
- `docs/public/modules/dishes/use-cases/UC-DSH-064-GetMeasureUnits.md` — справочник единиц (резолв `measureUnitId`).
- `docs/public/adr/ADR-0012-recipe-ingredient-discriminated-union.md` — обоснование полиморфного формата snapshot.
