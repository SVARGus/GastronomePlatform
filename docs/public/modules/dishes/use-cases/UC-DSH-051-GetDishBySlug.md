# UC-DSH-051: Получить публичную карточку блюда по slug

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: любой пользователь — гость или аутентифицированный. Анонимный публичный эндпоинт.
- Secondary: автор/`Admin` — получают дополнительный флаг `HasUnsavedChanges`.

---

## Resource (Ресурс)

- Entity: `Dish` — публичная карточка.
- Identifier: `Dish.Slug` (`string`) — path-параметр.
- Action: Read из jsonb-снепшота (`PublishedVersionData`).

---

## Security (Безопасность)

### Authentication

None. Эндпоинт публичный (без `[Authorize]`). Если передан валидный JWT, текущий пользователь учитывается для расчёта `HasUnsavedChanges`, но на видимость не влияет.

### Authorization

Нет ролевых ограничений. Видимость определяется наличием `PublishedVersionData` и статусом блюда.

### State Constraints

- `Status = Archived` → `404` всем.
- `PublishedVersionData IS NULL` → `404` всем (включая автора). Slug привязан к публичной версии; рабочая копия по slug не отдаётся.

---

## API Contract

### Endpoint

```
GET /api/dishes/by-slug/{slug}
```

### Request

**Path Parameters:**

- `slug` — `string`, до 220 символов. ASCII-латиница + цифры + дефисы (как генерирует `ISlugGenerator`).

**Headers:**

- `Authorization: Bearer <JWT>` — опционально. Влияет только на `HasUnsavedChanges` в ответе.

### Response

- Status: `200 OK`.
- Body: `DishDetailDto` (тот же, что у UC-DSH-050).
- Поля:
  - `IsPublishedVersion = true` — всегда (snapshot-only ветка).
  - `HasUnsavedChanges` — `true|false` для автора/admin при наличии правок в рабочем слое; `null` для прочих.

### Errors

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | Пустой slug или длина > 220. |
| 404  | `DISHES.DISH_NOT_FOUND`| Блюдо не существует, архивировано или не опубликовано (`PublishedVersionData IS NULL`). |

---

## Preconditions

- HTTP-маршрут резолвится.
- В системе существует блюдо с переданным `Slug` (или нет — определяет 404).

---

## Invariants (Инварианты домена)

- Snapshot отдаётся «как есть» из `Dish.PublishedVersionData`. Парсинг — через `IPublishedDishSnapshotReader`.
- `IsPublishedVersion = true` — гарантировано (другая ветка не достижима по UC-DSH-051).
- `HasUnsavedChanges` рассчитывается как `dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value`.
- Чтение не меняет состояние БД и не поднимает доменных событий.

---

## Main Flow

1. Гость переходит по ссылке `https://example.com/dishes/borsch-ukrainskij`.
2. Фронт делает `GET /api/dishes/by-slug/borsch-ukrainskij`.
3. `DishesController.GetBySlugAsync` создаёт `GetDishBySlugQuery` и отправляет в MediatR.
4. `GetDishBySlugQueryValidator` проверяет slug (`NotEmpty` + длина).
5. `GetDishBySlugQueryHandler`:
   - `IDishRepository.GetBySlugAsync(slug)`.
   - Если null / `Archived` / `PublishedVersionData IS NULL` → `404`.
   - Парсит snapshot через `IPublishedDishSnapshotReader`.
   - Рассчитывает `HasUnsavedChanges` для автора/admin.
   - Возвращает `DishDetailDto` (`MapFromSnapshot`).
6. Контроллер возвращает `200` с DTO.

---

## Alternative Flows

Нет — у UC только один успешный путь.

---

## Edge Cases

- **EC-1: Slug несуществующего блюда.** `GetBySlugAsync` вернёт `null` → `404`.
- **EC-2: Блюдо `Draft` или `Unpublished`.** `PublishedVersionData IS NULL` → `404` всем. Автор для редактирования использует UC-DSH-050 по Id.
- **EC-3: Блюдо `Archived`.** `404` (даже для admin на Этапе 2 — admin-доступ к архиву Этап 8+).
- **EC-4: Slug с верхним регистром (`Borsch-Ukrainskij`).** `GetBySlugAsync` ищет точное совпадение через EF — кириллицы там нет, но регистр различается. Если в БД slug в нижнем регистре, верхний регистр в запросе не найдёт. По дизайну `ISlugGenerator` всегда генерит lowercase — в БД они в lowercase. UI должен передавать как есть; canonical URL — lowercase.
- **EC-5: Slug длиннее 220 символов.** Валидатор → `400`. Защита от DDoS на БД-запрос.
- **EC-6: Параллельный вызов UC-DSH-070 IncrementViews.** Не вызывается из этого UC — клиент шлёт отдельный пинг после рендера карточки.
- **EC-7: Гость и `HasUnsavedChanges`.** Поле возвращается как `null` — приватная инфа о состоянии редактирования не утекает.

---

## Postconditions

При успехе:

- Клиент получил `DishDetailDto` с `IsPublishedVersion = true`.
- Состояние БД не изменилось.

При неуспехе (400/404):

- Состояние БД не изменилось.

---

## Non-Functional

- **Idempotency.** Идемпотентен.
- **Performance.** Целевое < 50 мс. Один `SELECT` по уникальному индексу `Dish.Slug` + парсинг jsonb.
- **Caching.** Не реализовано на Этапе 2. Snapshot-карточка — идеальный кандидат для HTTP-кэша через `Cache-Control` + ETag (Этап 4+).
- **SEO.** Slug — это публичный URL. Изменение генерации алгоритма slug — breaking change для старых ссылок; на Этапе 2 алгоритм заморожен.

---

## Реализация Этапа 2

### Реализовано

- Query + Validator (с `.WithMessage`) + Handler.
- Endpoint `GET /api/dishes/by-slug/{slug}` (без `[Authorize]`).
- Snapshot-only ветка: 404 при отсутствии `PublishedVersionData`.
- Парсинг snapshot через существующий `IPublishedDishSnapshotReader` (общий с UC-DSH-050).
- DTO `DishDetailDto` переиспользуется.

### Отложено

- **HTTP-кэш / ETag** — Этап 4+ при появлении общей кэш-инфраструктуры.
- **Параметр `?version=working`** для автора — отложено в UC-DSH-083 (Drafted, Этап 5 или 8+).
- **301-редирект при переименовании slug** — пока slug блюда после создания не меняется; admin-перегенерация (UC-DSH-140, Drafted, Этап 8+) потребует журнала старых slug.

---

## Связанные документы

- `docs/public/modules/dishes/use-cases/UC-DSH-050-GetDishById.md` — родственный UC по Id.
- `docs/public/modules/dishes/use-cases/UC-DSH-052-GetDishRecipe.md` — получение рецепта (по Id, не по slug).
- `docs/public/modules/dishes/use-cases/UC-DSH-070-IncrementDishViews.md` — клиент пингует после рендера карточки.
- `docs/public/modules/dishes/use-cases/README.md`.
