# UC-DSH-112: Деактивировать ингредиент (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin переключает `Ingredient.IsActive = false`. Мягкое удаление: существующие `RecipeIngredient` сохраняются и работают, но в автокомплите при добавлении в новые рецепты (UC-DSH-062 `SearchActiveByNamePrefixAsync`) ингредиент не появляется.

## Actors

- Администратор платформы. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `POST /api/ingredients/{id}/deactivate`

**Request:** `id` — `Guid` (path); тело не требуется.

**Response:** `204 No Content`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`            | `Guid.Empty`. |
| 401  | —                             | Нет JWT. |
| 403  | —                             | Не Admin. |
| 404  | `DISHES.INGREDIENT_NOT_FOUND` | Запись отсутствует. |

## Реализация

- `GetByIdAsync` → 404 при `null`.
- `Ingredient.Deactivate()` → `IsActive = false`.
- `SaveChangesAsync`.

Domain-метод идемпотентен: повторный вызов на уже неактивном ингредиенте также возвращает `204`.

## Реактивация

Через этот UC невозможна. Domain-метод `Ingredient.Activate()` готов; отдельный admin-эндпоинт появится при появлении бизнес-потребности (например, `POST /{id}/activate` симметрично deactivate).

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-110-CreateIngredient.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-111-UpdateIngredient.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-062-SearchIngredients.md` — фильтр `IsActive = true`.
