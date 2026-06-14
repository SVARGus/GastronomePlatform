# UC-DSH-110: Создать ингредиент (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin создаёт новую запись в справочнике ингредиентов.

## Actors

- Администратор платформы. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `POST /api/ingredients`

**Body (JSON):**

```json
{
  "name": "Мука пшеничная",
  "pluralName": "муки пшеничной",
  "description": "Высший сорт.",
  "imageMediaId": null,
  "isLiquid": false,
  "densityApprox": null,
  "isAllergen": true,
  "allergenType": "Gluten",
  "dietConflictsMask": "None",
  "baseMeasureUnitId": "...",
  "defaultNutritionId": null
}
```

Поля:
- `name` — 2..200 символов, уникальное.
- `pluralName` — до 200 символов, опционально.
- `description` — до 4000 символов, опционально.
- `isLiquid = true` ⇒ `densityApprox > 0`.
- `isAllergen = true` ⇒ `allergenType` указан.

**Response:** `201 Created` с `{ "id": "..." }` и заголовком `Location: /api/ingredients/{id}`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`         | Лимиты длин, условные инварианты, отрицательная плотность. |
| 401  | —                          | Нет JWT. |
| 403  | —                          | Не Admin. |
| 404  | `DISHES.MEASURE_UNIT_NOT_FOUND` | `baseMeasureUnitId` не существует. |
| 409  | `DISHES.INGREDIENT_NAME_TAKEN`  | Ингредиент с таким именем уже есть. |

## Реализация

- `IMeasureUnitRepository.GetByIdAsync` → 404.
- `IIngredientRepository.GetByNameAsync` → 409 при коллизии.
- `Ingredient.Create(...)` + `AddAsync` + `SaveChangesAsync`.
- Существование `defaultNutritionId` не проверяется — FK-constraint в БД ловит несуществующие значения через `DbUpdateException` (защищён только редкий admin-сценарий).

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-111-UpdateIngredient.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-112-DeactivateIngredient.md`
