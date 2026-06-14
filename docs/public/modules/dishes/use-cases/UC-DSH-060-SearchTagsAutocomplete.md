# UC-DSH-060: Поиск тегов с автокомплитом

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Возвращает теги, чей `NormalizedName` начинается с присланной подстроки (после нормализации). Ранжирование — по `UsageCount` убыванию, при равенстве — по имени. Для автокомплита при наборе тегов в UI (UC-DSH-008 SetTags).

## Actors

- Любой пользователь. Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/tags/search?query={text}&limit={n}`

**Request:**

- `query` — `string`, 1..50 символов до нормализации.
- `limit` — `int`, 1..50. По умолчанию 10.

**Response:** `200 OK` со списком `TagDto`: `Id`, `Name`, `Slug`, `UsageCount`, `IsVerified`. Пустой массив — допустимый результат.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR` | Пустой `query`, длина > 50, `limit` вне 1..50. |

## Реализация

- Нормализация ввода через `TagNameNormalizer.Normalize` (та же, что в UC-DSH-008).
- `ITagRepository.SearchByNormalizedNamePrefixAsync` — `WHERE NormalizedName ILIKE prefix%` + `ORDER BY UsageCount DESC, NormalizedName ASC`.
- Если префикс после нормализации пуст (например, `query = "   "`) — возвращается пустой список без обращения к БД.

## Отложено

- Уровень тегов — на Этапе 2 поиск проходит по всем тегам, не только верифицированным. Если возникнет потребность скрыть невалидированный спам — добавится фильтр `IsVerified` через параметр.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-008-SetTags.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-061-GetPopularTags.md`
