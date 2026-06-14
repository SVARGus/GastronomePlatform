# UC-DSH-064: Получить список единиц измерения

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Возвращает полный справочник единиц измерения (15–20 записей) для заполнения dropdown-ов в UI — например, при добавлении ингредиента в рецепт.

## Actors

- Любой пользователь. Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/measure-units`

**Request:** без параметров.

**Response:** `200 OK` со списком `MeasureUnitDto`: `Id`, `Code`, `NameRu`, `Type` (`Mass` / `Volume` / `Pinch` / `Piece`), `ConversionToBase`, `IsBase`. Сортировка: по `Type`, затем по `ConversionToBase`, затем по `Code` — стабильный детерминированный порядок для UI.

## Реализация

- `IMeasureUnitRepository.ListAllAsync` — один SQL.
- На Этапе 2 единицы измерения наполняются seed-данными; admin-API для них появится позже (UC-DSH-120/121, Drafted, Этап 8+).
- Идеальный кандидат для HTTP-кэша (`Cache-Control: public, max-age=3600`) — данные меняются редко. На Этапе 2 кэш не настраиваем.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-030-AddIngredientToRecipe.md`
