# UC-DSH-105: Перегенерировать slug категории (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Admin принудительно перегенерирует slug категории из её текущего `Name`. **Опасная операция** — ломает существующие публичные ссылки `/catalog/{slug}` и SEO. Выделена в отдельный эндпоинт, чтобы UI требовал явное подтверждение.

## Actors

- Администратор. Роль `PlatformRoles.ADMIN`.

## API Contract

**Endpoint:** `POST /api/categories/{id}/regenerate-slug`

**Response:** `200 OK` с

```json
{ "newSlug": "supy-2" }
```

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`               | `Guid.Empty`. |
| 401  | —                                | Нет JWT. |
| 403  | —                                | Не Admin. |
| 404  | `DISHES.CATEGORY_NOT_FOUND`      | Категория не существует. |
| 500  | `DISHES.SLUG_GENERATION_EXHAUSTED` | Не удалось подобрать уникальный slug за 30 попыток. |

## Реализация

1. `GetByIdAsync` → 404.
2. `ISlugGenerator.Generate(category.Name)` → base slug.
3. Retry с суффиксом `-N` через `SlugExistsAsync`. Текущий slug категории конфликтом не считается — это сама себя.
4. `Category.RegenerateSlug(newSlug)` + `SaveChangesAsync`.

## Известное ограничение

При смене slug не сохраняется журнал старых значений → 301-редирект для старых ссылок невозможен. Журнал `CategorySlugHistory` — задача Этапа 8+.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-101-CreateCategory.md`
- `docs/public/modules/dishes/use-cases/UC-DSH-102-UpdateCategory.md`
