# UC-DSH-104: Переместить категорию в иерархии (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin меняет `Category.ParentId`. Проверяется:
- отсутствие циклов (`NewParentId` не должен быть в поддереве категории),
- соблюдение `Category.MAX_DEPTH` (3) с учётом глубины перемещаемого поддерева,
- активность нового родителя.

## Actors

- Администратор. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `PUT /api/categories/{id}/move`

**Body:**

```json
{ "newParentId": "...-...-..." }
```

или `{ "newParentId": null }` для перемещения в корень.

**Response:** `204 No Content`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`                       | `Guid.Empty`, `newParentId == id`. |
| 401  | —                                        | Нет JWT. |
| 403  | —                                        | Не Admin. |
| 404  | `DISHES.CATEGORY_NOT_FOUND`              | Перемещаемая категория не существует. |
| 404  | `DISHES.CATEGORY_PARENT_NOT_FOUND`       | Новый родитель не существует / деактивирован. |
| 409  | `DISHES.CATEGORY_MOVE_TO_OWN_DESCENDANT` | `newParentId` лежит в поддереве категории. |
| 409  | `DISHES.CATEGORY_DEPTH_EXCEEDED`         | Суммарная глубина после перемещения > 3. |

## Реализация

- `ListAllAsync` → словарь категорий.
- Проверка существования + активность нового родителя.
- `CategoryHierarchyValidator.CollectDescendants` → 409 при попадании `newParentId` в поддерево.
- Проверка глубины: уровень нового родителя (для прямого ребёнка) + глубина поддерева - 1 ≤ 3.
- `Category.Move(newParentId)` + `SaveChangesAsync`.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-101-CreateCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-102-UpdateCategory.md`
