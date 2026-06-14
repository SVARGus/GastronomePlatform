# UC-DSH-057: Получить дерево категорий

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Возвращает полное дерево активных категорий (`Category.IsActive = true`) для построения навигации. Иерархия группируется по `Category.ParentId` на стороне Application.

## Actors

- Любой пользователь (включая гостей). Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/categories/tree`

**Request:** без параметров.

**Response:** `200 OK` со списком `CategoryNodeDto`. Каждый узел: `Id`, `Name`, `Slug`, `Order`, `IconMediaId`, `Children` (рекурсивный список). Сортировка — по `Order`, при равенстве по `Name`.

## Реализация

- Один SQL-запрос: `ICategoryRepository.ListActiveAsync`.
- In-memory сборка иерархии через словарь `ParentId → List<children>` за O(n).
- Категории с `IsActive = false` пропускаются полностью; их дочерние ветки также не отображаются (даже если активны).

## Edge Cases

- Пустой справочник → `200 OK` с пустым массивом.
- Висячие ссылки (`ParentId` указывает на неактивную/удалённую категорию) — узел становится фактически корневым с точки зрения отображения, но в выдачу не попадает (его не достать из словаря корней). При появлении такого сценария — admin-проблема целостности; на Этапе 2 не обрабатывается.

## Отложено

- HTTP-кэш с `Cache-Control: public, max-age=N` — Этап 4+.
- Локализация имён — Этап 5+.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-058-GetCategoryById.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-059-GetCategoryBySlug.md`
- `docs/public/modules/dishes/domain-model.md` — сущность `Category`.
