# UC-DSH-042: Установить КБЖУ рецепта

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` (POL-001).

---

## Resource (Ресурс)

- Entity: `Nutrition` — часть агрегата `Dish`, 1:1 с `Recipe` (Optional: `Recipe.Nutrition` может быть `null` до первой установки).
- Identifier: `Dish.Id` (`Guid`).
- Action: Upsert (Create-or-Update) всех полей `Nutrition`.

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
PUT /api/dishes/{id}/recipe/nutrition
```

### Request

**Body (JSON):**

```json
{
  "calcMethod": "Per100g",
  "calories": 250,
  "proteins": 8.5,
  "fats": 12.0,
  "saturatedFats": 4.0,
  "carbs": 28.0,
  "sugar": 6.0,
  "fiber": 3.5,
  "salt": 1.2
}
```

- `calcMethod` — `NutritionCalcMethod` enum (`Per100g`, `PerServing`).
- `calories`, `proteins`, `fats`, `carbs` — `decimal`, обязательные, ≥ 0.
- `saturatedFats`, `sugar`, `fiber`, `salt` — `decimal?`, опциональные, ≥ 0.
- Согласованность: `saturatedFats ≤ fats`, `sugar ≤ carbs`.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | Валидация не пройдена (отрицательное значение, нарушение согласованности, неверный enum). |
| 401  | —                      | Нет JWT. |
| 403  | `DISHES.NOT_DISH_OWNER`| Не автор и не Admin. |
| 404  | `DISHES.DISH_NOT_FOUND`| Блюдо отсутствует. |

> Этот UC — единственный среди UC-DSH-040..042, где Domain (`Nutrition.Update`) **не дублирует** валидацию: единственный источник правды для значений КБЖУ — `SetNutritionCommandValidator`. Это явно зафиксировано в XML-doc `Nutrition`.

---

## Preconditions

- Запрос аутентифицирован.
- Блюдо существует.
- Пользователь — автор или Admin.

---

## Invariants (Инварианты домена)

- Все значения КБЖУ ≥ 0.
- `SaturatedFats ≤ Fats`, `Sugar ≤ Carbs` (если заданы) — обеспечиваются валидатором.
- При первой установке: `Recipe.Nutrition` создаётся через `Nutrition.Create`, `Recipe.NutritionId` присваивается.
- При повторной установке: существующий `Nutrition.Update` перезаписывает поля; Id записи сохраняется.
- `Dish.UpdatedAt = utcNow`.
- Поднимается `DishUpdatedEvent`.

---

## Main Flow

1. Автор заполняет калькулятор КБЖУ и сохраняет.
2. `PUT /api/dishes/{id}/recipe/nutrition` с JSON.
3. `SetNutritionCommandValidator` проверяет неотрицательность и согласованность `Sugar ≤ Carbs` / `SaturatedFats ≤ Fats`.
4. `SetNutritionCommandHandler` грузит блюдо (`GetByIdWithRecipeAsync`) и проверяет POL-001.
5. `dish.UpdateNutrition(...)` делегирует в `Recipe.UpdateNutrition`:
   - Если `Nutrition is null` — создаётся новая запись (`Nutrition.Create`).
   - Иначе — `Nutrition.Update` перезаписывает поля.
6. `SaveChangesAsync` + `DispatchAsync` → `204`.

---

## Edge Cases

- **EC-1: Несуществующее блюдо.** → `404`.
- **EC-2: `saturatedFats = 5, fats = 3`.** Валидатор → `400`.
- **EC-3: `sugar = null, carbs = 0`.** Допустимо.
- **EC-4: Первая установка vs повторная.** Прозрачно для клиента — оба сценария возвращают `204`. Внутренне Domain различает по `Recipe.Nutrition is null`.
- **EC-5: `calcMethod = Per100g` без `gramsPerServing` в `Yield`.** Технически допустимо, но при пересчёте КБЖУ на порцию UI не сможет посчитать значения — это семантическая проблема, серверной защиты нет.
- **EC-6: Отрицательная калорийность.** Валидатор → `400`.

---

## Postconditions

При успехе:

- `Recipe.Nutrition` существует и содержит присланные значения.
- `Recipe.NutritionId` синхронизирован.
- `Dish.UpdatedAt = utcNow`.
- `DishUpdatedEvent` отправлено.
- `PublishedVersionData` не изменено.

---

## Non-Functional

- **Idempotency.** Идемпотентен.
- **Performance.** `< 50 мс`. При первой установке — `INSERT` записи `Nutritions`, при повторной — `UPDATE`.
- **Consistency.** Read Committed, одна транзакция.

---

## Реализация Этапа 2

### Реализовано

- Command + Validator (`.WithMessage`) + Handler.
- Endpoint `PUT /api/dishes/{id:guid}/recipe/nutrition`.
- POL-001 (Author + Admin).
- Upsert-семантика через `Recipe.UpdateNutrition`.

### Отложено

- **Автоматический расчёт КБЖУ из ингредиентов.** Сейчас автор вводит вручную. При появлении базы КБЖУ ингредиентов (`Ingredient.DefaultNutrition`) можно добавить отдельный UC «пересчитать КБЖУ по составу».
- **Удаление записи КБЖУ.** Нет UC для возврата `Recipe.Nutrition` в `null`. При появлении — отдельная Command `ClearNutrition`.

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md`.
- `docs/public/modules/dishes/domain-model.md` — `Nutrition`, `Recipe.Nutrition`, `Dish.UpdateNutrition`.
- `docs/public/modules/dishes/use-cases/UC-DSH-040-SetTiming.md`, `UC-DSH-041-SetYield.md`.
- `docs/public/adr/` — будущий ADR об автоматическом расчёте КБЖУ (когда созреет).
