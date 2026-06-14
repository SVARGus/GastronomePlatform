# UC-DSH-058: Получить категорию по идентификатору

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Карточка категории + список непосредственных дочерних категорий (один уровень вниз).

## Actors

- Любой пользователь (включая гостей). Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/categories/{id}`

**Request:** `id` — `Guid` (path).

**Response:** `200 OK` с `CategoryDetailDto`: базовые поля категории + `Children: IReadOnlyList<CategoryDto>` (непосредственные дети, отсортированы по `Order`).

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`         | `Guid.Empty`. |
| 404  | `DISHES.CATEGORY_NOT_FOUND`| Категория не существует или `IsActive = false`. |

## Реализация

- `ICategoryRepository.GetByIdAsync` + `ListActiveAsync` (фильтр детей в памяти).
- Неактивная категория → `404` (одно сообщение «не найдено», без раскрытия того, что запись существует но скрыта).

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-057-GetCategoryTree.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-059-GetCategoryBySlug.md`
