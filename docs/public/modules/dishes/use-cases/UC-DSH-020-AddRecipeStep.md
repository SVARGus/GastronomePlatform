# UC-DSH-020: Добавить шаг рецепта

**Version:** 1.0 | **Date:** 2026-06-13

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `RecipeStep` (часть агрегата `Dish` → `Recipe`).
- Identifier: `Dish.Id` (`Guid`) — path-параметр; `RecipeStep.Id` (`Guid`) — генерируется при создании, возвращается в `Location` и теле ответа.
- Action: Create.

---

## Security (Безопасность)

### Authentication

Required. Эндпоинт защищён `[Authorize(Policy = VALID_ACTOR)]`.

### Authorization

POL-001 Dish Ownership: автор блюда (`Dish.AuthorUserId == _currentUser.UserId`) или роль `Admin`.

### State Constraints

- Без отдельной проверки `Dish.Status` — согласно соглашению Этапа 2 (см. UC-DSH-002/003/009).

---

## API Contract (Контракт API)

### Endpoint

```
POST /api/dishes/{id}/recipe/steps
```

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Body:**

```json
{
  "description": "Залейте бульон холодной водой и доведите до кипения.",
  "title": "Бульон",
  "imageMediaId": null,
  "videoUrl": "https://youtube.com/watch?v=xxx",
  "temperatureCelsius": 100,
  "timerMinutes": 45
}
```

| Поле | Тип | Обязательно | Ограничения |
|---|---|---|---|
| `description` | `string` | Да | 10..4000 символов, не пробельный. |
| `title` | `string?` | Нет | До 200 символов. |
| `imageMediaId` | `Guid?` | Нет | ≠ `Guid.Empty`. На текущем этапе attach через `IMediaService` не выполняется (см. Postconditions). |
| `videoUrl` | `string?` | Нет | До 500 символов; валидный absolute URI со схемой `http`/`https`. |
| `temperatureCelsius` | `int?` | Нет | −30..300. |
| `timerMinutes` | `int?` | Нет | 1..1440. |

### Response

- Status: `201 Created`.
- Headers: `Location: /api/dishes/{id}/recipe/steps/{newStepId}`.
- Body: `{ "id": "<newStepId>" }`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 400 | `VALIDATION.ERROR` | Нарушены структурные ограничения (FluentValidation). |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 409 | `DISHES.INVALID_TEMPERATURE` | Температура вне диапазона (defense-in-depth с уровнем команды). |
| 409 | `DISHES.INVALID_TIMER_MINUTES` | Время таймера вне диапазона. |

---

## Preconditions

- Блюдо существует и доступно текущему пользователю по POL-001.

---

## Invariants

- `Order = max(Order)+1` для нового шага — поддерживается `Recipe.AddStep`.
- Диапазоны `TemperatureCelsius` и `TimerMinutes` дублируются в Domain (`RecipeStep.Update`) как defense-in-depth.

---

## Main Flow

1. Клиент шлёт `POST /api/dishes/{id}/recipe/steps` с телом запроса.
2. FluentValidation → `400` при структурных ошибках; `Uri.TryCreate` + scheme-check для `videoUrl`.
3. Handler загружает блюдо с полным рецептом (нужны существующие шаги для назначения `Order`).
4. POL-001: автор или Admin.
5. Вызов `Dish.AddRecipeStep` — Domain создаёт `RecipeStep` с `Order = max+1`, `Dish.UpdatedAt = utcNow`; поднят `DishUpdatedEvent`.
6. `SaveChangesAsync` + публикация доменных событий через `IDomainEventDispatcher`.
7. Ответ `201 Created` с `Location` и `{ id }`.

---

## Alternative Flows

Нет.

---

## Edge Cases

- **EC-1: Пустой `videoUrl`.** `When(!string.IsNullOrWhiteSpace)` пропускает проверку URL — пустая строка и `null` обрабатываются как «не задано». Это поведение симметрично `preparationNote` в UC-DSH-030.
- **EC-2: Конкурентные Add от автора и Admin.** Каждый запрос — своя транзакция; `Order` назначается атомарно по `max+1`. Возможна минимальная гонка `Order` при одновременной вставке (нет уникального индекса на `(RecipeId, Order)`).

---

## Postconditions

- В `dishes."RecipeSteps"` появилась новая запись с `Order = max+1`.
- `Dish.UpdatedAt = utcNow`.
- `Dish.PublishedVersionData` не изменён (двухслойная модель).
- Поднят `DishUpdatedEvent`.
- Attach `ImageMediaId` к `RecipeStep` через межмодульный `IMediaService` — **не выполняется** на текущем этапе. Реализация перенесена в отдельный UC при доработке `IMediaService`. Сохранённая ссылка может остаться «болтающейся» при удалении медиафайла в модуле Media.

---

## Non-Functional

- **Idempotency.** Не идемпотентен — каждый успешный вызов создаёт новый шаг.
- **Performance.** Целевое < 100 мс. Один SELECT (`GetByIdWithFullRecipeAsync`) + один INSERT + один UPDATE на `Dish.UpdatedAt`.
- **Consistency.** Один `SaveChangesAsync` — добавление шага и обновление `Dish.UpdatedAt` в одной транзакции.
- **Audit.** Стандартное HTTP-логирование (Serilog). `DishUpdatedEvent` — будущая основа аудита изменений.

---

## Связанные документы

- POL-001 — Dish Ownership Policy.
- `docs/public/modules/dishes/domain-model.md` — `Recipe`, `RecipeStep`, `Dish.AddRecipeStep`.
- UC-DSH-021, 022, 023 — другие операции над шагами рецепта.
