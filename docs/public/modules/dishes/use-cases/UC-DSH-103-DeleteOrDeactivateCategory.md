# UC-DSH-103: Удалить или деактивировать категорию (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin удаляет категорию. Сервер сам решает между hard delete и soft delete:

- **Hard delete** возможен, если у категории **нет** дочерних категорий и **нет** связок `DishCategory` / `DishCategoryPublished`.
- Иначе — **soft delete**: переключение `IsActive = false`. Связки и дети остаются.

## Actors

- Администратор. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `DELETE /api/categories/{id}`

**Response:** `200 OK` с

```json
{ "wasDeleted": true }
```

или `{ "wasDeleted": false }` (soft delete).

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`         | `Guid.Empty`. |
| 401  | —                          | Нет JWT. |
| 403  | —                          | Не Admin. |
| 404  | `DISHES.CATEGORY_NOT_FOUND` | Категория не существует. |

## Реализация

1. `GetByIdAsync` → 404.
2. `HasChildrenAsync` + `HasDishLinksAsync` (две проверки).
3. Если есть дети или связки → `category.Deactivate()` + `SaveChangesAsync` → `{ wasDeleted: false }`.
4. Иначе → `ICategoryRepository.DeleteAsync` (через `ExecuteDeleteAsync`) → `{ wasDeleted: true }`.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-101-CreateCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-102-UpdateCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-104-MoveCategory.md`
