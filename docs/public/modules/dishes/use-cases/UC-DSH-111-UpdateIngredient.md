# UC-DSH-111: Обновить ингредиент (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin изменяет любые редактируемые поля справочника. Флаг `IsActive` через этот UC не меняется (для активации/деактивации — UC-DSH-112).

## Actors

- Администратор платформы. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `PUT /api/ingredients/{id}`

**Body:** идентичен UC-DSH-110, но без поля `id` (приходит из path).

**Response:** `204 No Content`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`              | Лимиты, условные инварианты. |
| 401  | —                               | Нет JWT. |
| 403  | —                               | Не Admin. |
| 404  | `DISHES.INGREDIENT_NOT_FOUND`   | Запись отсутствует. |
| 404  | `DISHES.MEASURE_UNIT_NOT_FOUND` | Новая единица измерения не существует. |
| 409  | `DISHES.INGREDIENT_NAME_TAKEN`  | Новое имя уже занято другой записью. |

## Реализация

- `GetByIdAsync` → 404 при `null`.
- `IMeasureUnitRepository.GetByIdAsync` → 404.
- Проверка уникальности имени: `GetByNameAsync` → 409, **только** если найденная запись отличается по Id (та же запись с тем же именем — это «без изменений»).
- `Ingredient.Update(...)` (Domain) + `SaveChangesAsync`.

## Известное ограничение

При изменении `AllergenType` или `DietConflictsMask` существующие `Dish.AllergensMask` и `Dish.DietLabelsMask` блюд, содержащих этот ингредиент, **не пересчитываются**. Массовая инвалидация (через журнал `DishSnapshotInvalidation` или background-задачу) — задача Этапа 4+/8+.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-110-CreateIngredient.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-112-DeactivateIngredient.md`
