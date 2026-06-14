# UC-DSH-008: Установить теги блюда

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` (POL-001).

---

## Resource (Ресурс)

- Entity: `Dish` — связки `DishTag` (M:M) и счётчик `Tag.UsageCount` каждого затронутого тега.
- Identifier: `Dish.Id` (`Guid`).
- Action: Replace полного набора `DishTag` с автоматическим find-or-create в справочнике `Tag` и пересчётом `UsageCount`.

---

## Security (Безопасность)

### Authentication

Required (`AuthorizationPolicies.VALID_ACTOR`).

### Authorization — POL-001

- Roles: `Author` или `Admin` (`PlatformRoles.ADMIN`).
- Ownership: `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)`.

---

## API Contract

### Endpoint

```
PUT /api/dishes/{id}/tags
```

### Request

**Body (JSON):**

```json
{
  "tagNames": ["Веган", "Без глютена", "веганское"]
}
```

- `tagNames` — `string[]`, до 100 элементов на входе.
- Каждое имя после нормализации (`Trim + lowercase + collapse-whitespace`):
  - не пусто,
  - длиной 1..50 символов.
- Дубликаты по нормализованной форме схлопываются. Например, `["Веган", "веган", "Веган "]` → один тег `"веган"`.
- После дедупликации число уникальных тегов ≤ 20 (Domain-лимит).
- Пустой массив `[]` — снять все теги.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`           | Пустое имя, длина > 50, > 100 элементов на входе, `null` массив. |
| 401  | —                            | Нет JWT. |
| 403  | `DISHES.NOT_DISH_OWNER`      | Не автор и не Admin. |
| 404  | `DISHES.DISH_NOT_FOUND`      | Блюдо отсутствует. |
| 409  | `DISHES.TAG_LIMIT_EXCEEDED`  | После дедупликации > 20 уникальных тегов. |
| 500  | `DISHES.SLUG_GENERATION_EXHAUSTED` | Не удалось подобрать уникальный slug за 30 попыток (защитный лимит). |

---

## Preconditions

- Запрос аутентифицирован.
- Блюдо существует.
- Пользователь — автор или Admin.

---

## Invariants (Инварианты домена)

- `Dish.Tags.Count` ≤ 20.
- В `Dish.Tags` нет дубликатов по `TagId`.
- `Tag.NormalizedName` уникален в справочнике (БД-индекс).
- `Tag.Slug` уникален в справочнике (БД-индекс).
- `Tag.UsageCount` ≥ 0 (защита в `Tag.DecrementUsage`).
- `Dish.UpdatedAt = utcNow`.
- `DishTagPublished` не трогается — обновится при следующем `Publish`.
- `PublishedVersionData` не изменяется.
- Поднимается `DishUpdatedEvent`.

---

## Main Flow

1. Автор вводит теги через автокомплит (UC-DSH-060) и/или новые имена, нажимает «Сохранить».
2. Клиент шлёт `PUT /api/dishes/{id}/tags` с массивом имён.
3. `DishesController.SetTagsAsync` создаёт `SetTagsCommand`.
4. Валидатор проверяет: `DishId` не пуст, список не `null`, ≤ 100 элементов, каждое имя 1..50 символов после нормализации.
5. Handler:
   - Грузит `Dish` через `IDishRepository.GetByIdWithTagsAsync`.
   - Проверяет POL-001.
   - Нормализует имена, dedup по `NormalizedName`. Для каждой уникальной нормализованной формы запоминает первое (приоритетное) оригинальное написание — оно станет `Tag.Name` для новых тегов.
   - `ITagRepository.ListByNormalizedNamesAsync(distinctNormalized)` — batch-резолв существующих.
   - Для отсутствующих: генерирует уникальный slug через `ISlugGenerator.Generate(name)` + retry с суффиксом `-N` (до 30 попыток); создаёт `Tag.Create(...)` и `_tagRepository.AddAsync(tag)`.
   - Вычисляет дельту по `TagId`: `addedIds`, `removedIds`.
   - Для добавленных дёргает `Tag.IncrementUsage()` (на объектах из `finalTags`).
   - Для удалённых — `ITagRepository.ListByIdsAsync(removedIds)` + `Tag.DecrementUsage()`.
   - `dish.SetTags(newTagIds, utcNow)` — Domain очищает `_tags` и заполняет заново, поднимает `DishUpdatedEvent` через `MarkAsUpdated`.
   - `SaveChangesAsync` (один транзакционный коммит) + `DispatchAsync`.
6. `204 No Content`.

---

## Edge Cases

- **EC-1: Пустой массив `[]`.** Все связки `DishTag` удаляются, у затронутых тегов `UsageCount−−`.
- **EC-2: Дубликаты с разным регистром.** `["Веган", "веган"]` → silent dedup → один тег. Нет ошибки.
- **EC-3: Имя длиной > 50.** Валидатор → `400` «Имя тега не должно превышать 50 символов.».
- **EC-4: Имя из одних пробелов / эмодзи.** После нормализации пусто → `400` «Имя тега не может быть пустым.».
- **EC-5: > 20 уникальных тегов.** Domain → `409 TAG_LIMIT_EXCEEDED`.
- **EC-6: > 100 элементов на входе.** Валидатор → `400` (защита от payload-bomb).
- **EC-7: Существующий тег с тем же `NormalizedName`.** Find-or-create переиспользует существующий, новый `Tag` не создаётся. Slug существующего не изменяется.
- **EC-8: Race condition при одновременном создании одного и того же тега двумя командами.** Уникальный индекс `NormalizedName` отвергнет вторую вставку (`DbUpdateException`). На Этапе 2 ловить не пробуем — `SaveChangesAsync` прокинет исключение наверх, GlobalExceptionHandler вернёт `500`. Клиент может повторить запрос — повторно вызовется find-or-create и найдёт уже созданный другим запросом тег. Retry с явной обработкой — в техдолге.
- **EC-9: Имя из эмодзи или нелатинского текста, который `ISlugGenerator` не транслитерирует.** Fallback на slug вида `tag-{12-знаков-Guid}`.
- **EC-10: Коллизия slug** (несколько имён транслитерируются в одинаковую форму). Суффикс `-2`, `-3`, …. До 30 попыток.

---

## Postconditions

При успехе:

- Записи `DishTag` для блюда переписаны под новый набор.
- Для каждого добавленного `TagId` — `UsageCount++` соответствующего `Tag`.
- Для каждого удалённого `TagId` — `UsageCount−−` (но не ниже 0).
- Новые `Tag` сохранены в справочнике с автогенерированным `Slug`.
- `Dish.UpdatedAt = utcNow`.
- `DishUpdatedEvent` отправлено.
- `DishTagPublished` и `PublishedVersionData` не изменены.

При неуспехе: состояние БД не меняется (один транзакционный коммит).

---

## Non-Functional

- **Idempotency.** Идемпотентен по составу тегов: повторный PUT с теми же именами вернёт `204`, состав связок не изменится. `UpdatedAt` обновится повторно (повторный `DishUpdatedEvent`).
- **Performance.** Целевое < 200 мс. SQL: `SELECT Dish + Include(Tags)` + `SELECT Tags WHERE NormalizedName IN (…)` + `SELECT Tags WHERE Id IN (removed)` + `INSERT Tags` (по новому) + `INSERT/DELETE DishTags` + `UPDATE Tags.UsageCount`.
- **Consistency.** Read Committed. Одна транзакция: либо весь набор изменений применён, либо ничего.
- **Race conditions.** См. EC-8.

---

## Реализация Этапа 2

### Реализовано

- Расширение Domain `Tag`: поле `Slug`, константы `MIN_NAME_LENGTH`, `MAX_NAME_LENGTH`, `MAX_SLUG_LENGTH`, методы `IncrementUsage`/`DecrementUsage` (internal).
- Расширение `TagConfiguration`: колонка `Slug` (`HasMaxLength(80)`, NOT NULL), уникальный индекс на `Slug`.
- `ITagRepository`: новые методы `ListByNormalizedNamesAsync`, `ListByIdsAsync`, `SlugExistsAsync`.
- `TagRepository.cs` (Infrastructure) — полная реализация + DI-регистрация.
- `IDishRepository.GetByIdWithTagsAsync` — целевая подгрузка под replace-семантику.
- `TagNameNormalizer` (Application.Helpers) — `Trim + lowercase + collapse-whitespace`.
- Application: `SetTagsCommand` / `Validator` (с `.WithMessage`) / `Handler` с find-or-create, дельтой `UsageCount`, retry slug.
- Endpoint `PUT /api/dishes/{id:guid}/tags` с XML-doc.
- POL-001 (Author + Admin).
- Миграция БД — добавление колонки `Slug` + уникального индекса.

### Отложено

- **EC-8 retry на DbUpdateException** — текущая Этап-2 реализация прокидывает исключение наверх; повтор пользователем устранит ошибку (find-or-create найдёт уже созданный тег). Адекватный retry — техдолг.
- **POL-001 §4.1 (`Archived` → только Admin)** — общий долг по handler-ам модификации Dishes.
- **Admin-управление тегами (UC-DSH-130/131/132 MergeTags)** — отдельные admin-UC.
- **Сложные правила нормализации** (например, схлопывание дефисов в пробелы) — при появлении бизнес-потребности.
- **Транслитерация для дедупликации** (например, `"вегетарианский"` ↔ `"vegetarianskiy"`) — слияние таких тегов — задача `UC-DSH-132 MergeTags` (Drafted, Этап 8+).

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md`.
- `docs/public/modules/dishes/domain-model.md` — `Tag`, `DishTag`, `DishTagPublished`.
- `docs/public/modules/dishes/use-cases/UC-DSH-007-SetCategories.md` — параллельный UC для M:M-связок категорий.
- `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — синхронизация `DishTagPublished` происходит там.
- `docs/public/modules/dishes/use-cases/UC-DSH-060-...` — автокомплит тегов (для UI).
- `docs/public/modules/dishes/use-cases/UC-DSH-130-...` — admin-верификация тегов.
- `docs/public/modules/dishes/use-cases/UC-DSH-132-...` — admin-слияние тегов (Drafted, Этап 8+).
