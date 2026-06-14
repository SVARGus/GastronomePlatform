# UC-DSH-054: Поиск и фильтрация блюд

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Каталожный поиск опубликованных блюд для главной страницы и страниц категорий/тегов. Все фильтры опциональны; обязательный фильтр — `Dish.PublishedVersionData IS NOT NULL`.

## Actors

- Любой пользователь (включая гостей). Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/dishes/search`

**Query Parameters (все опциональны):**

| Параметр | Тип | Описание |
|----------|-----|----------|
| `text` | `string?` | Подстрока поиска. ILIKE по `Name` + `ShortDescription`. До 200 символов. |
| `categoryIds` | `Guid[]` | Категории (OR). Резолв через `DishCategoryPublished`. До 50 элементов. |
| `tagIds` | `Guid[]` | Теги (OR). Резолв через `DishTagPublished`. До 50 элементов. |
| `dietLabelsMask` | `DietLabels?` | Битовая маска (AND): блюдо имеет **все** запрошенные метки. `None`/`null` — без фильтра. |
| `difficulties` | `DifficultyLevel[]` | Уровни сложности (IN). |
| `costs` | `CostEstimate[]` | Оценки стоимости (IN). |
| `minRating` | `decimal?` | Минимальный `RatingAvg` (0..5). |
| `sortBy` | `DishSearchSortBy` | `Newest` (дефолт) / `RatingDesc` / `ViewsDesc`. |
| `page` | `int` | ≥ 1. Дефолт 1. |
| `pageSize` | `int` | 1..25. Дефолт 12. |

**Response:** `200 OK` с `SearchDishesResult`:

```json
{
  "items": [DishCardListItemDto, ...],
  "totalCount": 137,
  "page": 1,
  "pageSize": 12
}
```

`DishCardListItemDto` — тот же, что в UC-DSH-055 (`Id`, `AuthorUserId`, `Slug`, `Name`, `ShortDescription`, `MainImageId`, `DifficultyLevel`, `CostEstimate`, `DietLabelsMask`, `AllergensMask`, `HasUnverifiedAllergens`, `RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount`, `PublishedAt`, `CreatedAt`).

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR` | `text` > 200 символов; список фильтров > 50; `minRating` вне 0..5; `sortBy` вне enum; `page < 1`; `pageSize` вне 1..25. |

## Реализация

- `IDishRepository.SearchPublishedAsync` собирает фильтры через цепочку `Where`-вызовов на `IQueryable<Dish>`.
- Базовый фильтр: `PublishedVersionData IS NOT NULL`.
- Поиск по тексту: `EF.Functions.ILike` по `Name` и `ShortDescription`.
- Категории / теги: `EXISTS` subquery по `DishCategoryPublished` / `DishTagPublished`.
- `dietLabelsMask`: битовый AND — `(d.DietLabelsMask & mask) == mask`.
- `difficulties` / `costs`: `IN` через `Contains`.
- `minRating`: `RatingAvg >= threshold`.
- Сортировка: `switch` по `sortBy`, with secondary `ThenByDescending(PublishedAt)` для стабильности.
- Маппинг `Dish` → `DishCardListItemDto` из основных таблиц (паттерн UC-DSH-055).

## Edge Cases

- Все параметры пусты → возвращаются все опубликованные блюда, отсортированные по `PublishedAt` убыванию.
- `categoryIds` содержит несуществующий Guid → блюдо просто не найдётся; запрос валиден.
- `dietLabelsMask = None` → фильтр не применяется (как если бы параметр не был передан).
- `text` из пробелов → `string.IsNullOrWhiteSpace` → фильтр не применяется.
- Сортировка по `RatingDesc` для блюд без оценок (`RatingCount = 0`, `RatingAvg = 0`) → они окажутся в конце, но среди себя — по `PublishedAt`.

## Известные ограничения Этапа 2

- **Поиск по тексту работает по основным таблицам**, не по jsonb-snapshot. Если автор опубликовал блюдо, потом изменил `Name` в рабочей копии (без перепубликации) — поиск найдёт блюдо по новому имени, хотя в каталоге показывается старое (из snapshot). Это сознательный компромисс; миграция на jsonb-поиск или полнотекстовый индекс — Этап 8+.
- **Нет фильтра `maxTotalTimeMinutes`.** Требует JOIN с `Recipe.Timing`, либо денормализации `TotalTimeMinutes` на `Dish`. Отложено.
- **Карточки списка** берут денормализованные поля (`DietLabelsMask`, `AllergensMask`) из основных таблиц — те же причины, что в UC-DSH-055.
- **Нет кэша HTTP** (`Cache-Control` / ETag) — Этап 4+.
- **Нет facet-ов** (количество блюд по каждому значению фильтра) — Этап 4+.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-055-GetDishesByAuthor.md` — пагинированный список по автору, использует тот же DTO.
- `docs/public/modules/dishes/use-cases/UC-DSH-057-GetCategoryTree.md`, `UC-DSH-060-SearchTagsAutocomplete.md`, `UC-DSH-061-GetPopularTags.md` — справочники для построения фильтров на UI.
- `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — формирование `DishCategoryPublished` / `DishTagPublished`.
- `docs/public/modules/dishes/domain-model.md` — обоснование двухслойной модели (working / published).
