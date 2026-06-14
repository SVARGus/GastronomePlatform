# UC-DSH-063: Получить ингредиент по идентификатору

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Карточка ингредиента из справочника. Используется на странице «Энциклопедия ингредиентов» и при предзаполнении формы добавления в рецепт.

## Actors

- Любой пользователь. Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/ingredients/{id}`

**Request:** `id` — `Guid` (path).

**Response:** `200 OK` с `IngredientDto` (см. UC-DSH-062). Возвращается и для неактивных ингредиентов — флаг `IsActive` приходит в DTO; UI решает, как отображать.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`           | `Guid.Empty`. |
| 404  | `DISHES.INGREDIENT_NOT_FOUND`| Запись отсутствует. |

## Реализация

- `IIngredientRepository.GetByIdAsync` — один SQL по PK.
- Фильтр `IsActive` не накладывается: ингредиент может быть деактивирован, но при этом всё ещё ссылаться из существующих рецептов (`RecipeIngredient` с `IngredientId` на неактивную позицию) — карточка должна быть доступна для отображения.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-062-SearchIngredients.md`
