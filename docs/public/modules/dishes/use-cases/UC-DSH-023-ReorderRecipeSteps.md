# UC-DSH-023: Переупорядочить шаги рецепта

**Version:** 1.0 | **Date:** 2026-06-13

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.

---

## Resource (Ресурс)

- Entity: коллекция `RecipeStep` внутри `Recipe`.
- Identifier: `Dish.Id` (path-параметр) + полный список `RecipeStep.Id` в теле.
- Action: Update (только поле `Order` всех шагов).

---

## Security (Безопасность)

### Authentication

Required. `[Authorize(Policy = VALID_ACTOR)]`.

### Authorization

POL-001 Dish Ownership: автор или Admin.

---

## API Contract

### Endpoint

```
PUT /api/dishes/{id}/recipe/steps/order
```

**Path Parameters:** `id` — `Guid` блюда.

**Body:**

```json
{
  "orderedStepIds": [
    "00000000-0000-0000-0000-000000000001",
    "00000000-0000-0000-0000-000000000002",
    "00000000-0000-0000-0000-000000000003"
  ]
}
```

| Поле | Тип | Обязательно | Ограничения |
|---|---|---|---|
| `orderedStepIds` | `IReadOnlyList<Guid>` | Да | Непустой; нет `Guid.Empty`; должен содержать все шаги рецепта без дубликатов (проверяется в Domain). |

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 400 | `VALIDATION.ERROR` | Список пуст, содержит `Guid.Empty`, или сам параметр `null`. |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 404 | `DISHES.STEP_NOT_FOUND` | Какой-то `stepId` из списка не принадлежит рецепту. |
| 409 | `DISHES.INVALID_STEP_ORDER` | Размер списка не совпадает с числом шагов рецепта или содержит дубликаты. |

---

## Preconditions

- Блюдо существует, POL-001 пройден.
- В рецепте есть хотя бы один шаг (иначе нечего переупорядочивать; см. EC-1).

---

## Invariants

- После выполнения `RecipeStep.Order` принимает значения 1..N в порядке элементов списка.
- Состав не меняется — UC изменяет только `Order`.
- `Dish.RecalculateDishMarkers` **не** вызывается (зависит только от состава ингредиентов).

---

## Main Flow

1. Клиент шлёт `PUT /api/dishes/{id}/recipe/steps/order` со списком в теле.
2. FluentValidation → `400` при структурных ошибках.
3. Handler загружает блюдо с полным рецептом.
4. POL-001 — `403` при провале.
5. Вызов `Dish.ReorderRecipeSteps(orderedStepIds, utcNow)`. Domain через `Recipe.ReorderSteps`:
   - размер списка ≠ числу шагов или дубликаты → `409 INVALID_STEP_ORDER`;
   - какой-то `Id` не принадлежит рецепту → `404 STEP_NOT_FOUND`;
   - переназначает `Order = i+1` шагам в указанном порядке.
6. `Dish.UpdatedAt = utcNow`, `DishUpdatedEvent`.
7. `SaveChangesAsync` + публикация событий.
8. Ответ `204`.

---

## Alternative Flows

Нет.

---

## Edge Cases

- **EC-1: Пустой рецепт.** Если в `Recipe.Steps` 0 шагов, клиент пришлёт пустой список — отсекается FluentValidation (`Must Count > 0`). Если клиент пришлёт непустой список — Domain вернёт `INVALID_STEP_ORDER`.
- **EC-2: Тот же порядок.** Запрос с уже актуальным порядком — успешный no-op, `Order`-ы не меняются. `Dish.UpdatedAt` всё равно сдвигается.

---

## Postconditions

- `Order` шагов соответствует порядку в `orderedStepIds`.
- `Dish.UpdatedAt = utcNow`.
- Поднят `DishUpdatedEvent`.

---

## Non-Functional

- **Idempotency.** Идемпотентен — повторный одинаковый запрос даёт тот же state.
- **Performance.** Целевое < 100 мс.
- **Consistency.** Один `SaveChangesAsync`.

---

## Связанные документы

- POL-001 — Dish Ownership Policy.
- UC-DSH-020, 021, 022 — другие операции над шагами.
- UC-DSH-033 — симметричный UC для ингредиентов.
