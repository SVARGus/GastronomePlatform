# UC-DSH-041: Установить выход рецепта

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` (POL-001).

---

## Resource (Ресурс)

- Entity: `Yield` — часть агрегата `Dish`, 1:1 с `Recipe`.
- Identifier: `Dish.Id` (`Guid`).
- Action: Replace всех полей `Yield`.

---

## Security (Безопасность)

### Authentication

Required (`VALID_ACTOR`).

### Authorization — POL-001

- Roles: `Author` или `Admin` (`PlatformRoles.ADMIN`).
- Ownership: `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)`.

---

## API Contract

### Endpoint

```
PUT /api/dishes/{id}/recipe/yield
```

### Request

**Body (JSON):**

```json
{
  "quantityTotal": 1.5,
  "yieldUnit": "Kilograms",
  "servingsCount": 4,
  "gramsPerServing": 375.0
}
```

- `quantityTotal` — `decimal`, ≥ 0.
- `yieldUnit` — `YieldUnit` enum (`Grams`, `Kilograms`, `Milliliters`, `Liters`, `Pieces`, `Servings`).
- `servingsCount` — `int`, ≥ 1.
- `gramsPerServing` — `decimal?`, ≥ 0 если задано.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | Валидация не пройдена. |
| 401  | —                      | Нет JWT. |
| 403  | `DISHES.NOT_DISH_OWNER`| Не автор и не Admin. |
| 404  | `DISHES.DISH_NOT_FOUND`| Блюдо отсутствует. |
| 409  | `DISHES.INVALID_YIELD` | Domain поймал нарушение инварианта (defense-in-depth). |

---

## Preconditions

- Запрос аутентифицирован.
- Блюдо существует.
- Пользователь — автор или Admin.

---

## Invariants (Инварианты домена)

- `Yield.QuantityTotal ≥ 0`.
- `Yield.ServingsCount ≥ 1`.
- `Yield.GramsPerServing` — либо `null`, либо `≥ 0`.
- `Dish.UpdatedAt = utcNow`.
- Поднимается `DishUpdatedEvent`.

---

## Main Flow

1. Автор задаёт «4 порции по 375 г» и сохраняет.
2. `PUT /api/dishes/{id}/recipe/yield` с JSON.
3. `SetYieldCommandValidator` проверяет диапазоны.
4. `SetYieldCommandHandler` грузит блюдо (`GetByIdWithRecipeAsync`) и проверяет POL-001.
5. `dish.UpdateYield(...)` делегирует в `Yield.Update`; Domain возвращает `Result`.
6. `SaveChangesAsync` + `DispatchAsync` → `204`.

---

## Edge Cases

- **EC-1: `servingsCount = 0`.** Валидатор → `400`.
- **EC-2: `yieldUnit = 999`.** `IsInEnum()` → `400`.
- **EC-3: `gramsPerServing = null`.** Допустимо; в БД сохранится `NULL`. UI впоследствии не сможет показать «на порцию», а UC-DSH-056 RecalculateIngredients для пересчёта на N порций по массе откажет.
- **EC-4: `quantityTotal = 0, yieldUnit = Pieces, servingsCount = 1`.** Технически валидно, но логически странно. Бизнес-проверки на согласованность — на UI; сервер минимально ограничивает.

---

## Postconditions

При успехе:

- Поля `Yield` заменены.
- `Dish.UpdatedAt = utcNow`.
- `DishUpdatedEvent` отправлено.
- `PublishedVersionData` не изменено.

---

## Non-Functional

- **Idempotency.** Идемпотентен.
- **Performance.** `< 50 мс`.
- **Consistency.** Read Committed, одна транзакция.

---

## Реализация Этапа 2

### Реализовано

- Command + Validator (`.WithMessage`) + Handler.
- Endpoint `PUT /api/dishes/{id:guid}/recipe/yield`.
- POL-001 (Author + Admin).
- Domain-инвариант `Yield.Update` как defense-in-depth.

### Отложено

- **Семантические бизнес-правила** (например, `yieldUnit = Servings` ⇒ `quantityTotal = servingsCount`). Сейчас не проверяются — оставлено на UI.

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md`.
- `docs/public/modules/dishes/domain-model.md` — `Yield`, `YieldUnit`, `Dish.UpdateYield`.
- `docs/public/modules/dishes/use-cases/UC-DSH-056-RecalculateIngredients.md` — использует `GramsPerServing` для пересчёта (будет реализован позже).
- `docs/public/modules/dishes/use-cases/UC-DSH-040-SetTiming.md`, `UC-DSH-042-SetNutrition.md`.
