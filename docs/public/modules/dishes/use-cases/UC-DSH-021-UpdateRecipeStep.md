# UC-DSH-021: Обновить шаг рецепта

**Version:** 1.0 | **Date:** 2026-06-13

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.

---

## Resource (Ресурс)

- Entity: `RecipeStep`.
- Identifier: `Dish.Id` + `RecipeStep.Id` (path-параметры).
- Action: Update (атомарный, все поля одной операцией).

---

## Security (Безопасность)

### Authentication

Required. `[Authorize(Policy = VALID_ACTOR)]`.

### Authorization

POL-001 Dish Ownership: автор или Admin.

### State Constraints

- Без отдельной проверки `Dish.Status` — соглашение Этапа 2.

---

## API Contract

### Endpoint

```
PUT /api/dishes/{id}/recipe/steps/{stepId}
```

**Path Parameters:** `id`, `stepId` — `Guid`.

**Body:**

```json
{
  "description": "Залейте бульон холодной водой и доведите до кипения.",
  "title": "Бульон",
  "imageMediaId": null,
  "videoUrl": null,
  "temperatureCelsius": 100,
  "timerMinutes": null
}
```

| Поле | Тип | Обязательно | Ограничения |
|---|---|---|---|
| `description` | `string` | Да | 10..4000 символов, не пробельный. |
| `title` | `string?` | Нет | До 200 символов. `null` — очистить. |
| `imageMediaId` | `Guid?` | Нет | ≠ `Guid.Empty`. `null` — очистить. |
| `videoUrl` | `string?` | Нет | До 500 символов; валидный http(s) URI. `null` — очистить. |
| `temperatureCelsius` | `int?` | Нет | −30..300. `null` — очистить. |
| `timerMinutes` | `int?` | Нет | 1..1440. `null` — очистить. |

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 400 | `VALIDATION.ERROR` | Нарушены структурные ограничения. |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 404 | `DISHES.STEP_NOT_FOUND` | Шаг с указанным `stepId` не принадлежит рецепту. |
| 409 | `DISHES.INVALID_TEMPERATURE` | Температура вне диапазона. |
| 409 | `DISHES.INVALID_TIMER_MINUTES` | Время таймера вне диапазона. |

---

## Preconditions

- Блюдо существует, шаг принадлежит его рецепту.
- POL-001 пройден.

---

## Invariants

- `Order` шага не изменяется (для смены порядка — UC-DSH-023).
- Все поля присваиваются атомарно. `null` означает «очистить».

---

## Main Flow

1. Клиент шлёт `PUT /api/dishes/{id}/recipe/steps/{stepId}` с телом.
2. FluentValidation → `400` при ошибках.
3. Handler загружает блюдо с полным рецептом.
4. POL-001 — `403` при провале.
5. Вызов `Dish.UpdateRecipeStep(stepId, ...)`. Domain через `Recipe.UpdateStep`:
   - находит шаг → `404` `STEP_NOT_FOUND` при отсутствии;
   - валидирует диапазоны → `409` `INVALID_TEMPERATURE` / `INVALID_TIMER_MINUTES`;
   - присваивает поля, `Dish.UpdatedAt = utcNow`, поднимает `DishUpdatedEvent`.
6. `SaveChangesAsync` + публикация доменных событий.
7. Ответ `204`.

---

## Alternative Flows

Нет.

---

## Edge Cases

- **EC-1: Очистка `imageMediaId`.** При смене с непустого на `null` Domain просто присваивает `null`. Detach в модуле Media через `IMediaService` — не выполняется (см. Postconditions UC-DSH-020).
- **EC-2: Пустой `videoUrl`.** Аналогично UC-DSH-020 — пропускает проверку URL.

---

## Postconditions

- Поля шага обновлены в `dishes."RecipeSteps"`.
- `Dish.UpdatedAt = utcNow`.
- Поднят `DishUpdatedEvent`.

---

## Non-Functional

- **Idempotency.** Идемпотентен на уровне Domain — повторный одинаковый запрос даёт тот же конечный state.
- **Performance.** Целевое < 100 мс.
- **Consistency.** Один `SaveChangesAsync`.

---

## Связанные документы

- POL-001 — Dish Ownership Policy.
- UC-DSH-020, 022, 023 — другие операции над шагами.
