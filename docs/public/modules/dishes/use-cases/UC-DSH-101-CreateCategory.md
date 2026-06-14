# UC-DSH-101: Создать категорию (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin создаёт новую категорию каталога. Slug генерируется сервером из `Name` через `ISlugGenerator`; при коллизии добавляется суффикс `-N`. Проверяется глубина иерархии (≤ 3 уровня).

## Actors

- Администратор платформы. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `POST /api/categories`

**Body:**

```json
{
  "name": "Супы",
  "parentId": null,
  "order": 0,
  "iconMediaId": null
}
```

**Response:** `201 Created` с `{ "id": "...", "slug": "supy" }` и заголовком `Location`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`              | Лимиты, `order < 0`. |
| 401  | —                               | Нет JWT. |
| 403  | —                               | Не Admin. |
| 404  | `DISHES.CATEGORY_PARENT_NOT_FOUND` | `parentId` не существует или деактивирован. |
| 409  | `DISHES.CATEGORY_DEPTH_EXCEEDED`  | Уровень нового узла > `Category.MAX_DEPTH` (3). |

## Реализация

- `ICategoryRepository.ListAllAsync` → словарь `byId` для проверок.
- Проверка `parentId` → 404.
- `CategoryHierarchyValidator.EnsureChildDepthWithinLimit` → 409.
- Генерация уникального slug + retry с суффиксом (до 30 попыток).
- `Category.Create(...)` + `AddAsync` + `SaveChangesAsync`.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-102-UpdateCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-103-DeleteOrDeactivateCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-104-MoveCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-105-RegenerateCategorySlug.md`
