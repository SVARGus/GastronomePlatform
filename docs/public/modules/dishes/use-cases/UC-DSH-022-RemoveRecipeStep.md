# UC-DSH-022: Удалить шаг рецепта

**Version:** 1.0 | **Date:** 2026-06-13

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.

---

## Resource (Ресурс)

- Entity: `RecipeStep`.
- Identifier: `Dish.Id` + `RecipeStep.Id` (path-параметры).
- Action: Delete (hard delete внутри агрегата).

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
DELETE /api/dishes/{id}/recipe/steps/{stepId}
```

**Path Parameters:** `id`, `stepId` — `Guid`.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 404 | `DISHES.STEP_NOT_FOUND` | Шаг не принадлежит рецепту. |

---

## Preconditions

- Блюдо существует, шаг принадлежит его рецепту.
- POL-001 пройден.

---

## Invariants

- После удаления оставшиеся шаги переупорядочиваются: `Order = 1..N` в текущем порядке хранения коллекции.

---

## Main Flow

1. Клиент шлёт `DELETE /api/dishes/{id}/recipe/steps/{stepId}`.
2. Handler загружает блюдо с полным рецептом.
3. POL-001 — `403` при провале.
4. Вызов `Dish.RemoveRecipeStep(stepId, utcNow)`. Domain через `Recipe.RemoveStep`:
   - находит шаг → `404` `STEP_NOT_FOUND`;
   - удаляет из коллекции;
   - пересчитывает `Order = i+1` для оставшихся.
5. `Dish.UpdatedAt = utcNow`, `DishUpdatedEvent`.
6. `SaveChangesAsync` + публикация событий.
7. Ответ `204`.

---

## Alternative Flows

Нет.

---

## Edge Cases

- **EC-1: Последний шаг.** Удалить можно. На Этапе 2 нет инварианта «у Recipe ≥ 1 шаг» в обычном состоянии (Draft). Инвариант проверяется только в `Dish.CheckCanPublish` перед публикацией.
- **EC-2: Detach `ImageMediaId`.** В модуле Media не выполняется (`IMediaService` не реализован). Медиа становится orphan и обнаружится фоновой задачей UC-MED-210 в будущем.

---

## Postconditions

- Шаг удалён из `dishes."RecipeSteps"`.
- `Order` оставшихся шагов нормализован до `1..N`.
- `Dish.UpdatedAt = utcNow`.
- Поднят `DishUpdatedEvent`.

---

## Non-Functional

- **Idempotency.** Не идемпотентен — повторный вызов даст `404`.
- **Performance.** Целевое < 100 мс.
- **Consistency.** Один `SaveChangesAsync`.

---

## Связанные документы

- POL-001 — Dish Ownership Policy.
- UC-DSH-020, 021, 023 — другие операции над шагами.
- UC-MED-210 — фоновая очистка orphan-медиа (этап 8+).
