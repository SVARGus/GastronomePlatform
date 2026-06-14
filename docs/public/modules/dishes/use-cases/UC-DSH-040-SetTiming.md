# UC-DSH-040: Установить тайминг рецепта

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` (POL-001).

---

## Resource (Ресурс)

- Entity: `Timing` — часть агрегата `Dish`, 1:1 с `Recipe`.
- Identifier: `Dish.Id` (`Guid`) — path-параметр.
- Action: Replace всех полей `Timing`.

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required (политика `AuthorizationPolicies.VALID_ACTOR`).

### Authorization (Авторизация) — POL-001

- Policy: POL-001 Dish Ownership.
- Roles: `Author` или `Admin` (`PlatformRoles.ADMIN`).
- Ownership: проверка в Handler через `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)`.

### State Constraints

Ограничений по `Dish.Status` нет: тайминг можно править в `Draft`, `Published`, `Unpublished`. В `Archived` — формально допустимо для Admin (Author там не имеет права модификации согласно POL-001 §4.1, но текущая реализация одна на все статусы).

---

## API Contract

### Endpoint

```
PUT /api/dishes/{id}/recipe/timing
```

### Request

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Body (JSON):**

```json
{
  "prepTimeMinutes": 10,
  "cookTimeMinutes": 30,
  "restTimeMinutes": 5,
  "activeTimeMinutes": 15,
  "totalTimeMinutes": 45,
  "isTotalManual": true
}
```

- `prepTimeMinutes`, `cookTimeMinutes`, `restTimeMinutes`, `activeTimeMinutes` — `int?`, неотрицательные.
- `totalTimeMinutes` — `int`, неотрицательное. Используется только при `isTotalManual = true`.
- `isTotalManual` — `bool`. Если `false`, сервер вычисляет `total = (prep ?? 0) + (cook ?? 0) + (rest ?? 0)`, присланное значение игнорируется.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | `DishId = Guid.Empty` или отрицательное время. |
| 401  | —                      | Нет валидного JWT. |
| 403  | `DISHES.NOT_DISH_OWNER`| Пользователь не автор и не Admin. |
| 404  | `DISHES.DISH_NOT_FOUND`| Блюдо не существует. |
| 409  | `DISHES.INVALID_TIMING`| Domain поймал отрицательное значение (защита defense-in-depth). |

---

## Preconditions

- Запрос аутентифицирован.
- Блюдо существует.
- Пользователь — автор или Admin.

---

## Invariants (Инварианты домена)

- `Timing.TotalTimeMinutes ≥ 0`.
- Все опциональные времена — либо `null`, либо `≥ 0`.
- Если `IsTotalManual = false`: `TotalTimeMinutes = (Prep ?? 0) + (Cook ?? 0) + (Rest ?? 0)`.
- `Dish.UpdatedAt` обновлён через `Dish.MarkAsUpdated(utcNow)`.
- Поднимается `DishUpdatedEvent`.

---

## Main Flow

1. Автор открыл форму тайминга и нажал «Сохранить».
2. Клиент шлёт `PUT /api/dishes/{id}/recipe/timing` с JSON-телом.
3. `DishesController.SetTimingAsync` создаёт `SetTimingCommand` и отправляет в MediatR.
4. `SetTimingCommandValidator` проверяет `DishId` и неотрицательность времён.
5. `SetTimingCommandHandler` грузит `Dish` через `GetByIdWithRecipeAsync`.
6. POL-001: `author || admin` — иначе `403`.
7. `dish.UpdateTiming(...)` делегирует в `Timing.UpdateTimes`; Domain снова проверяет отрицательность и возвращает `Result`.
8. `SaveChangesAsync` + `DispatchAsync` → `204`.

---

## Edge Cases

- **EC-1: Несуществующее блюдо.** → `404`.
- **EC-2: Не автор и не Admin.** → `403 NOT_DISH_OWNER`.
- **EC-3: Отрицательное `prepTimeMinutes`.** Валидатор → `400 VALIDATION.ERROR`. До Domain дело не доходит.
- **EC-4: `isTotalManual = false`.** Сервер игнорирует присланный `totalTimeMinutes`, вычисляет сумму prep + cook + rest. `activeTimeMinutes` в сумму не входит.
- **EC-5: Все опциональные значения `null` + `isTotalManual = false`.** `Total` становится `0`. Это валидно, но блюдо потом нельзя опубликовать (UC-DSH-004 требует `TotalTimeMinutes > 0`).
- **EC-6: Очень большие значения.** Лимит `int.MaxValue` минут — теоретический. Бизнес-лимит UI-валидации может быть жёстче (например, 7 дней = 10080 минут); серверной защиты на «слишком большое» нет.

---

## Postconditions

При успехе:

- `Timing.PrepTimeMinutes`, `CookTimeMinutes`, `RestTimeMinutes`, `ActiveTimeMinutes`, `TotalTimeMinutes`, `IsTotalManual` заменены.
- `Dish.UpdatedAt = utcNow`.
- `DishUpdatedEvent` отправлено.
- `PublishedVersionData` не изменено.

При неуспехе: состояние БД не меняется.

---

## Non-Functional

- **Idempotency.** Идемпотентен: повторный вызов с теми же значениями даёт тот же результат, единственное побочное действие — повторное `MarkAsUpdated` и повторный `DishUpdatedEvent`.
- **Performance.** `< 50 мс`. `SELECT` + 1:1-join по `Timing` + `UPDATE`.
- **Consistency.** Read Committed. Одна транзакция.

---

## Реализация Этапа 2

### Реализовано

- Command + Validator (`.WithMessage`) + Handler.
- Endpoint `PUT /api/dishes/{id:guid}/recipe/timing`.
- POL-001 (Author + Admin).
- Domain-инварианты `Timing.UpdateTimes` + Domain-ошибка `DishesErrors.InvalidTiming` как defense-in-depth.

### Отложено

- **Бизнес-лимит на максимальное время.** Сейчас только `≥ 0`. При появлении UX-требования — добавить ограничение сверху.

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md`.
- `docs/public/modules/dishes/domain-model.md` — `Timing`, `Dish.UpdateTiming`.
- `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — инвариант `TotalTimeMinutes > 0` для публикации.
- `docs/public/modules/dishes/use-cases/UC-DSH-041-SetYield.md`, `UC-DSH-042-SetNutrition.md` — соседние UC по частям рецепта.
