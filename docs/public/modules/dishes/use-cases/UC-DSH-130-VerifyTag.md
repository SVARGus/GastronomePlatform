# UC-DSH-130: Верифицировать тег (admin)

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Администратор помечает тег как одобренный (`Tag.IsVerified = true`). После этого тег появляется в облаке популярных (UC-DSH-061 фильтрует `IsVerified = true`) и в общем автокомплите без оглядки на `UsageCount`.

## Actors

- Администратор платформы. Роль `PlatformRoles.ADMIN`.

## Authorization

- `[Authorize(Roles = PlatformRoles.ADMIN)]` на эндпоинте.

## API Contract

**Endpoint:** `POST /api/tags/{id}/verify`

**Request:** `id` — `Guid` (path); тело не требуется.

**Response:** `204 No Content`.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | `Guid.Empty`. |
| 401  | —                      | Нет JWT. |
| 403  | —                      | Пользователь без роли `Admin`. |
| 404  | `DISHES.TAG_NOT_FOUND` | Тег не существует. |

## Реализация

- `ITagRepository.GetByIdAsync` → 404 при `null`.
- `Tag.Verify()` → `IsVerified = true`. Domain-метод идемпотентен: повторный вызов на уже верифицированном теге — без эффекта (то же значение).
- `SaveChangesAsync`.

## Идемпотентность

Повторный вызов на уже верифицированном теге также возвращает `204` — состояние не меняется.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-061-GetPopularTags.md` — потребитель `IsVerified`.
- `docs/public/modules/dishes/use-cases/UC-DSH-131-DeleteTag.md` — admin-удаление.
