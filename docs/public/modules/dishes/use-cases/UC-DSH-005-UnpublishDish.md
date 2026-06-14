# UC-DSH-005: Снять блюдо с публикации

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда — аутентифицированный пользователь, владелец блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` — пользователь с ролью `PlatformRoles.ADMIN` (POL-001).

---

## Resource (Ресурс)

- Entity: `Dish` — корень агрегата каталога.
- Identifier: `Dish.Id` (`Guid`) — path-параметр.
- Action: State transition (`Published → Unpublished`) с обнулением публичной версии.

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required. Endpoint защищён политикой `AuthorizationPolicies.VALID_ACTOR`: запрос должен содержать валидный JWT с claim `sub`, парсящимся в `Guid`. Без JWT — `401`.

### Authorization (Авторизация) — POL-001

- Policy: POL-001 Dish Ownership.
- Roles: `Author` или `Admin` (`PlatformRoles.ADMIN`).
- Ownership: проверяется в Handler: `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)`. При нарушении — `403 DISHES.NOT_DISH_OWNER`.

### State Constraints (Ограничения по состоянию)

- Снять с публикации можно **только** блюдо в статусе `Published`. Для `Draft` / `Unpublished` / `Archived` — `409 DISHES.DISH_NOT_PUBLISHED` (инвариант `Dish.Unpublish`).

### Contextual Constraints (Контекстуальные ограничения)

Нет.

---

## API Contract (Контракт API)

### Endpoint

```
POST /api/dishes/{id}/unpublish
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Headers:**

- `Authorization: Bearer <JWT>` — обязательно.

**Body:**

Не требуется (переход состояния без параметров).

### Response (Ответ)

- Status: `204 No Content`.
- Body: отсутствует.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена         | Условие                                                                                  |
|-------------|---------------------------|------------------------------------------------------------------------------------------|
| 400         | `VALIDATION.ERROR`        | `DishId = Guid.Empty`. Защита FluentValidation на уровне команды.                        |
| 401         | —                         | Отсутствует / невалиден JWT.                                                             |
| 403         | `DISHES.NOT_DISH_OWNER`   | `dish.AuthorUserId != _currentUser.UserId`. POL-001.                                     |
| 404         | `DISHES.DISH_NOT_FOUND`   | Блюдо с указанным `Id` отсутствует.                                                      |
| 409         | `DISHES.DISH_NOT_PUBLISHED` | Блюдо не находится в статусе `Published` (Draft / Unpublished / Archived).             |

---

## Preconditions (Предусловия)

- HTTP-маршрут `POST /api/dishes/{id:guid}/unpublish` корректно резолвится.
- Запрос содержит валидный JWT (политика `VALID_ACTOR` пройдена).
- В системе существует блюдо с переданным `Id`.
- Блюдо находится в статусе `Published`.
- Текущий пользователь — автор блюда **или** имеет роль `Admin`.

---

## Invariants (Инварианты домена)

- `Dish.Status = Unpublished` после успешного выполнения.
- `Dish.PublishedVersionData IS NULL`.
- `Dish.PublishedAt IS NULL`.
- `Dish.PublishedVersionUpdatedAt IS NULL`.
- `DishCategoryPublished` / `DishTagPublished` для этого блюда — пусто.
- `Dish.UpdatedAt = utcNow` (обновляется Domain-методом).
- `Dish.CreatedAt` — не изменяется.
- Основные таблицы (`Dish`, `Recipe`, `RecipeStep`, `RecipeIngredient`, `DishCategory`, `DishTag`) не затрагиваются.
- Поднимается доменное событие `DishUnpublishedEvent(DishId, AuthorUserId)`.

---

## Main Flow (Основной поток)

1. Автор открыл страницу управления блюдом и нажал «Снять с публикации».
2. Клиент шлёт `POST /api/dishes/{id}/unpublish` с заголовком `Authorization`.
3. `DishesController.UnpublishAsync` создаёт `UnpublishDishCommand(DishId = id)` и отправляет в MediatR.
4. `UnpublishDishCommandValidator` проверяет `DishId != Guid.Empty`.
5. `UnpublishDishCommandHandler` загружает `Dish` через `IDishRepository.GetByIdAsync` (только корневые поля).
6. Проверка владения: `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)` — иначе `403 NOT_DISH_OWNER`.
7. Domain-метод `dish.Unpublish(utcNow)`:
   - Проверяет `Status == Published`; иначе — `409 DISH_NOT_PUBLISHED`.
   - Переключает `Status` в `Unpublished`.
   - Обнуляет `PublishedAt`, `PublishedVersionData`, `PublishedVersionUpdatedAt`.
   - Очищает `_categoriesPublished` и `_tagsPublished` (EF Core загрузит коллекции lazy при обращении).
   - Обновляет `UpdatedAt`.
   - Поднимает `DishUnpublishedEvent`.
8. `_dishRepository.SaveChangesAsync` — один транзакционный коммит.
9. `_eventDispatcher.DispatchAsync` — публикация доменных событий (на Этапе 2 подписчиков нет).
10. Контроллер возвращает `204 No Content`.

---

## Alternative Flows (Альтернативные потоки)

Нет — у UC только один успешный путь.

---

## Edge Cases (Граничные случаи)

- **EC-1: Блюдо отсутствует.** `GetByIdAsync` вернёт `null`. Handler → `404 DISHES.DISH_NOT_FOUND`.
- **EC-2: Пользователь не автор и не Admin.** `dish.AuthorUserId != actorUserId && !isAdmin` → `403 DISHES.NOT_DISH_OWNER`. Domain не вызывается. Если у пользователя есть роль `Admin`, проверка проходит и операция выполняется как для автора.
- **EC-3: Блюдо в статусе `Draft`.** Не было опубликовано — Domain возвращает `DISH_NOT_PUBLISHED` (409). UI рекомендуется скрывать кнопку, но серверная защита обязательна.
- **EC-4: Блюдо уже `Unpublished`.** Симметричный случай EC-3: `409 DISH_NOT_PUBLISHED`.
- **EC-5: Блюдо `Archived`.** `409 DISH_NOT_PUBLISHED`. Восстановление из архива — отдельный UC (Drafted, Этап 8+).
- **EC-6: Конкурентные запросы Unpublish + Publish.** Каждая команда читает агрегат и сохраняет в своей транзакции. PostgreSQL не использует `RowVersion` на уровне Dish — последний коммит выигрывает. Для Этапа 2 это приемлемо; OCC через `xmin` / `RowVersion` — кандидат в техдолг при появлении конкурентного редактирования.
- **EC-7: Блюдо с привязанными медиа.** `MainImageId` и `RecipeStep.ImageMediaId` не отвязываются — это явное решение UC-006 (которое распространяется и на Unpublish): медиа остаются как `Ready` на блюде, при повторной публикации не требуется заново загружать.

---

## Postconditions (Постусловия)

При успешном выполнении:

- `Dish.Status = Unpublished`.
- `Dish.PublishedVersionData`, `Dish.PublishedAt`, `Dish.PublishedVersionUpdatedAt` обнулены.
- `DishCategoryPublished` / `DishTagPublished` для этого блюда удалены.
- Основные таблицы агрегата (рабочая копия) сохранены неизменными.
- `Dish.UpdatedAt` обновлено.
- Доменное событие `DishUnpublishedEvent` отправлено в `IDomainEventDispatcher`.
- Поиск / каталог (UC-DSH-054) перестают показывать блюдо.
- Прямая ссылка `GET /api/dishes/{id}` возвращает `404` для гостей; автор по-прежнему видит рабочую версию.

При неуспехе (400 / 401 / 403 / 404 / 409):

- Состояние БД не меняется.
- Доменные события не отправляются.

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Не идемпотентен: повторный вызов на уже `Unpublished` блюде вернёт `409 DISH_NOT_PUBLISHED`. С точки зрения публичных эффектов — повторный запрос ничего не меняет, что и требуется.
- **Performance.** Целевое < 50 мс. Один `SELECT` корневого `Dish` + `UPDATE` + `DELETE`-ы по `*Published`-таблицам в одной транзакции. Recipe / подколлекции не грузятся.
- **Consistency.** Read Committed (стандарт PostgreSQL). Одна транзакция охватывает изменение `Dish` и очистку `*Published`-таблиц — частичных состояний не бывает.
- **Audit.** Стандартное HTTP-логирование (Serilog). Отдельный аудит факта снятия — на Этапе 8+ вместе с антиабьюз-аналитикой по причинам снятия (UC-006-style).

---

## Реализация Этапа 2 — что в наличии и что отложено

### Реализовано

- Command + Validator + Handler.
- Endpoint `POST /api/dishes/{id:guid}/unpublish` с политикой `VALID_ACTOR`.
- Доменный метод `Dish.Unpublish(utcNow)` с проверкой инварианта статуса.
- Событие `DishUnpublishedEvent(DishId, AuthorUserId)`.
- POL-001 (Author + Admin).

### Отложено

- **Опциональное поле «причина снятия».** UC-006-style антиабьюз-аналитика — Этап 8+. Сейчас причина не передаётся.
- **Каскадная инвалидация кэшей.** Появится на Этапе 4+ вместе с самим кэшем.
- **Подписчики `DishUnpublishedEvent`.** Появятся на Этапе 5+ (Social для пересчёта счётчиков, Notifications для оповещения подписчиков).

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md` — политика авторизации.
- `docs/public/modules/dishes/domain-model.md` — `Dish.Unpublish`, `DishUnpublishedEvent`.
- `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — обратный переход (`Unpublished → Published`).
- `docs/public/modules/dishes/use-cases/UC-DSH-006-ArchiveDish.md` — параллельный сценарий мягкого удаления.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC.
