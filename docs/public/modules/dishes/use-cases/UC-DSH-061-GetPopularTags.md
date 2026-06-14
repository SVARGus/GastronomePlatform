# UC-DSH-061: Получить популярные теги

**Version:** 1.0 | **Date:** 2026-06-14

---

## Назначение

Топ-N верифицированных тегов по `UsageCount` для облака тегов на главной странице.

## Actors

- Любой пользователь. Анонимный публичный эндпоинт.

## API Contract

**Endpoint:** `GET /api/tags/popular?limit={n}`

**Request:**

- `limit` — `int`, 1..50. По умолчанию 20.

**Response:** `200 OK` со списком `TagDto` (см. UC-DSH-060). Отсортированы по `UsageCount` убыванию.

**Errors:**

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR` | `limit` вне 1..50. |

## Реализация

- `ITagRepository.ListTopVerifiedByUsageAsync` — `WHERE IsVerified = true ORDER BY UsageCount DESC, NormalizedName ASC LIMIT N`.
- Фильтр `IsVerified = true` — admin-одобренные теги исключают шум.

## Отложено

- Кэширование (Этап 4+ с общей кэш-инфраструктурой).
- Параметр временного окна (`?windowDays=30` — теги, популярные за последний месяц) — Этап 5+.

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-060-SearchTagsAutocomplete.md`
