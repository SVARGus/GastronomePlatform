# UC-DSH-059: Получить категорию по slug

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Симметричен UC-DSH-058, но разрешение происходит по уникальному `Category.Slug` — для SEO-friendly URL вида `/catalog/supy`.

## Actors

- Любой пользователь. Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/categories/by-slug/{slug}`

**Request:** `slug` — `string` (path), до 220 символов.

**Response:** `200 OK` с `CategoryDetailDto` (см. UC-DSH-058).

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`         | Пустой или слишком длинный slug. |
| 404  | `DISHES.CATEGORY_NOT_FOUND`| Категория не существует или `IsActive = false`. |

## Реализация

- `ICategoryRepository.GetBySlugAsync` + `ListActiveAsync` для детей.
- Slug в БД в lowercase (`ISlugGenerator` всегда производит lower); UI должен передавать как есть.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-058-GetCategoryById.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-057-GetCategoryTree.md`
