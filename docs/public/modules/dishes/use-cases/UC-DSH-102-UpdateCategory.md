# UC-DSH-102: Обновить категорию (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin меняет `Name`, `Order`, `IconMediaId`, `IsActive`. Slug и `ParentId` через этот UC не меняются (отдельные UC-DSH-105 и UC-DSH-104).

## Actors

- Администратор. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `PUT /api/categories/{id}`

**Body:**

```json
{
  "name": "Первые блюда",
  "order": 1,
  "iconMediaId": null,
  "isActive": true
}
```

**Response:** `204 No Content`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`         | Лимиты `Name`, `order < 0`. |
| 401  | —                          | Нет JWT. |
| 403  | —                          | Не Admin. |
| 404  | `DISHES.CATEGORY_NOT_FOUND` | Категория не существует. |

## Реализация

- `GetByIdAsync` → 404.
- `Category.Update(...)` + `Activate()` / `Deactivate()` по флагу `IsActive`.
- `SaveChangesAsync`.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-101-CreateCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-104-MoveCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-105-RegenerateCategorySlug.md`
