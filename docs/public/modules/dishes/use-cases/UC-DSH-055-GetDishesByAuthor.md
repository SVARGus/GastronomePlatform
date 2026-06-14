# UC-DSH-055: Получить блюда автора

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Постраничный список **опубликованных** блюд указанного автора. Используется на странице профиля автора в публичной части каталога.

## Actors

- Любой пользователь (включая гостей). Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/dishes/by-author/{authorUserId}?page=1&pageSize=12`

**Request:**

- `authorUserId` — `Guid` (path).
- `page` — `int`, ≥ 1 (query). По умолчанию 1.
- `pageSize` — `int`, 1..25 (query). По умолчанию 12.

**Response:** `200 OK` с `GetDishesByAuthorResult`:

```json
{
  "items": [DishCardListItemDto, ...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 12
}
```

`DishCardListItemDto`: `Id`, `AuthorUserId`, `Slug`, `Name`, `ShortDescription`, `MainImageId`, `DifficultyLevel`, `CostEstimate`, `DietLabelsMask`, `AllergensMask`, `HasUnverifiedAllergens`, `RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount`, `PublishedAt`, `CreatedAt`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR` | `Guid.Empty`, `page < 1`, `pageSize` вне 1..25. |

## Реализация

- `IDishRepository.ListPublishedByAuthorAsync` — два SQL-запроса (Count + Items) с фильтром `AuthorUserId = ? AND PublishedVersionData IS NOT NULL`, сортировка `ORDER BY PublishedAt DESC`.
- Маппинг `Dish` → `DishCardListItemDto`: поля берутся из основных таблиц (паттерн UC-DSH-053 GetMyDrafts), без парсинга snapshot.

## Edge Cases

- Автор без публикаций → `200 OK` с пустым `items` и `totalCount = 0`.
- `authorUserId` несуществующего пользователя → то же поведение (404 не возвращаем — каталог не знает, есть ли такой пользователь в Users).
- Snapshot vs основные поля: если опубликованное блюдо имеет правки в рабочем слое — карточка показывает основные поля, не snapshot. Это согласовано на Этапе 2 (см. лог сессии); для строгой snapshot-семантики появится отдельный путь.
- Удалённый аккаунт автора (`Users.IsDeleted = true`, Этап 5+) — список всё равно показывается; UI решает, отображать ли «Автор удалил аккаунт» в шапке.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-053-GetMyDrafts.md` — симметричный UC для собственных черновиков.
- `docs/public/modules/dishes/use-cases/UC-DSH-050-GetDishById.md` — детальная карточка по Id.
- `docs/public/modules/dishes/use-cases/UC-DSH-054-...` — общий каталожный поиск (отдельный UC).
