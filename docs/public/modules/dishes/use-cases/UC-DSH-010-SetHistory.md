# UC-DSH-010: Установить историко-культурное описание блюда

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` (POL-001).

---

## Resource (Ресурс)

- Entity: `Dish` — поле `Dish.HistoryText`.
- Identifier: `Dish.Id` (`Guid`) — path-параметр.
- Action: Replace одного текстового поля.

---

## Security (Безопасность)

### Authentication

Required (`AuthorizationPolicies.VALID_ACTOR`).

### Authorization — POL-001

- Roles: `Author` или `Admin` (`PlatformRoles.ADMIN`).
- Ownership: проверяется в Handler — `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)`.

---

## API Contract

### Endpoint

```
PATCH /api/dishes/{id}/history
```

### Request

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Body (JSON):**

```json
{
  "historyText": "Борщ — традиционное блюдо восточнославянской кухни..."
}
```

- `historyText` — `string?`, до `Dish.MAX_HISTORY_TEXT_LENGTH` (4000) символов. `null` — очистить поле.

### Response

- Status: `204 No Content`.

### Errors

| HTTP | Код | Условие |
|------|-----|---------|
| 400  | `VALIDATION.ERROR`     | `DishId = Guid.Empty` или текст длиннее лимита. |
| 401  | —                      | Нет валидного JWT. |
| 403  | `DISHES.NOT_DISH_OWNER`| Пользователь не автор и не Admin. |
| 404  | `DISHES.DISH_NOT_FOUND`| Блюдо не существует. |

---

## Preconditions

- Запрос аутентифицирован.
- Блюдо существует.
- Пользователь — автор или Admin.

---

## Invariants (Инварианты домена)

- `Dish.HistoryText` ≤ 4000 символов (проверяется валидатором, лимит — единый источник `Dish.MAX_HISTORY_TEXT_LENGTH`).
- `Dish.UpdatedAt = utcNow`.
- Поднимается `DishUpdatedEvent`.
- `PublishedVersionData` не изменяется.

---

## Main Flow

1. Автор открывает «Историю блюда», правит текст, нажимает «Сохранить».
2. Клиент шлёт `PATCH /api/dishes/{id}/history` с JSON-телом.
3. `DishesController.SetHistoryAsync` создаёт `SetHistoryCommand` и отправляет в MediatR.
4. `SetHistoryCommandValidator` проверяет `DishId` и длину текста.
5. `SetHistoryCommandHandler` грузит `Dish` через `GetByIdAsync`.
6. POL-001: `author || admin` — иначе `403`.
7. `dish.UpdateHistory(historyText, utcNow)` — Domain заменяет поле и поднимает событие.
8. `SaveChangesAsync` + `DispatchAsync` → `204`.

---

## Edge Cases

- **EC-1: `historyText = null`.** Поле очищается. Валидное действие.
- **EC-2: `historyText = ""` (пустая строка).** Сохраняется как пустая строка. По бизнес-смыслу UI может приравнивать к очистке, но сервер сохраняет как есть.
- **EC-3: 4001 символ.** Валидатор → `400` «Историческое описание не должно превышать 4000 символов.».
- **EC-4: Не автор и не Admin.** → `403 NOT_DISH_OWNER`.
- **EC-5: Архивное блюдо.** Domain не проверяет статус для `UpdateHistory`. Author технически может править архивное блюдо (хотя POL-001 §4.1 это запрещает). Defense — будет добавлена при обработке POL-001 §4.1 на уровне Application пакетной правкой по всем модификаторам.

---

## Postconditions

При успехе:

- `Dish.HistoryText` обновлено.
- `Dish.UpdatedAt = utcNow`.
- `DishUpdatedEvent` отправлено.
- `PublishedVersionData` не изменено.

При неуспехе: состояние БД не меняется.

---

## Non-Functional

- **Idempotency.** Идемпотентен.
- **Performance.** `< 50 мс`. Один `SELECT` корневого `Dish` + `UPDATE`.
- **Consistency.** Read Committed, одна транзакция.

---

## Реализация Этапа 2

### Реализовано

- Command + Validator (`.WithMessage`) + Handler.
- Endpoint `PATCH /api/dishes/{id:guid}/history`.
- POL-001 (Author + Admin).

### Отложено

- **Вынесение `HistoryText` в отдельную сущность `DishHistory`** (Этап 8+, см. `domain-model.md`). Когда появятся версионирование текста и хронология правок.
- **Markdown / HTML рендеринг.** Сейчас сервер хранит как plain text. UI решает, что отображать.
- **Полнотекстовый поиск по `HistoryText`.** Этап 8+ при появлении расширенного поиска.

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md`.
- `docs/public/modules/dishes/domain-model.md` — поле `Dish.HistoryText`, метод `Dish.UpdateHistory`.
- `docs/public/modules/dishes/use-cases/UC-DSH-002-UpdateDishCard.md` — основной UC карточки (не включает `HistoryText`).
- `docs/public/modules/dishes/use-cases/README.md`.
