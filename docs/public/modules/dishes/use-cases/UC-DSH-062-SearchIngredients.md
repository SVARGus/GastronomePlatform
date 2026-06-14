# UC-DSH-062: Поиск ингредиентов по справочнику

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Возвращает активные ингредиенты, у которых `Name` начинается с присланного префикса (case-insensitive). Для UI добавления ингредиента в рецепт (UC-DSH-030).

## Actors

- Любой пользователь. Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/ingredients/search?query={text}&limit={n}`

**Request:**

- `query` — `string`, 1..200 символов.
- `limit` — `int`, 1..50. По умолчанию 20.

**Response:** `200 OK` со списком `IngredientDto`. Поля включают `Id`, `Name`, `PluralName`, `Description`, `ImageMediaId`, `IsLiquid`, `DensityApprox`, `IsAllergen`, `AllergenType`, `DietConflictsMask`, `BaseMeasureUnitId`, `DefaultNutritionId`, `IsActive`. Сортировка — по `Name` алфавиту.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR` | Пустой `query`, длина > 200, `limit` вне 1..50. |

## Реализация

- `IIngredientRepository.SearchActiveByNamePrefixAsync` — `WHERE IsActive = true AND Name ILIKE prefix%` (PostgreSQL `EF.Functions.ILike`).
- Только активные (`IsActive = true`) ингредиенты — UI не должен предлагать деактивированные.
- Пустой query после `Trim()` — пустой результат без обращения к БД.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-030-AddIngredientToRecipe.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-063-GetIngredientById.md`
