# UC-DSH-006: Архивировать блюдо

**Version:** 1.0 | **Date:** 2026-06-14

---

## Actors (Инициаторы)

- Primary: автор блюда — аутентифицированный пользователь, владелец блюда (`Dish.AuthorUserId == ActorUserId`).
- Secondary: `Admin` — пользователь с ролью `PlatformRoles.ADMIN` (POL-001).

---

## Resource (Ресурс)

- Entity: `Dish` — корень агрегата каталога.
- Identifier: `Dish.Id` (`Guid`) — path-параметр.
- Action: State transition (`Draft | Published | Unpublished → Archived`) — мягкое удаление.

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required. Endpoint защищён политикой `AuthorizationPolicies.VALID_ACTOR`: запрос должен содержать валидный JWT с claim `sub`, парсящимся в `Guid`. Без JWT — `401`.

### Authorization (Авторизация) — POL-001

- Policy: POL-001 Dish Ownership.
- Roles: `Author` или `Admin` (`PlatformRoles.ADMIN`).
- Ownership: `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)`. При нарушении — `403 DISHES.NOT_DISH_OWNER`.

### State Constraints (Ограничения по состоянию)

- Допустимы исходные статусы `Draft`, `Published`, `Unpublished`. Из `Archived` — `409 DISHES.DISH_ALREADY_ARCHIVED` (инвариант `Dish.Archive`).

### Contextual Constraints (Контекстуальные ограничения)

Нет.

---

## API Contract (Контракт API)

### Endpoint

```
POST /api/dishes/{id}/archive
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Headers:**

- `Authorization: Bearer <JWT>` — обязательно.

**Body:**

Не требуется.

### Response (Ответ)

- Status: `204 No Content`.
- Body: отсутствует.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена            | Условие                                                            |
|-------------|------------------------------|--------------------------------------------------------------------|
| 400         | `VALIDATION.ERROR`           | `DishId = Guid.Empty`.                                             |
| 401         | —                            | Отсутствует / невалиден JWT.                                       |
| 403         | `DISHES.NOT_DISH_OWNER`      | Пользователь не автор. POL-001.                                    |
| 404         | `DISHES.DISH_NOT_FOUND`      | Блюдо с указанным `Id` отсутствует.                                |
| 409         | `DISHES.DISH_ALREADY_ARCHIVED` | Блюдо уже находится в статусе `Archived`.                        |

---

## Preconditions (Предусловия)

- HTTP-маршрут `POST /api/dishes/{id:guid}/archive` корректно резолвится.
- Запрос содержит валидный JWT (политика `VALID_ACTOR` пройдена).
- В системе существует блюдо с переданным `Id`.
- Текущий пользователь — автор блюда **или** имеет роль `Admin`.
- Блюдо НЕ в статусе `Archived`.

---

## Invariants (Инварианты домена)

- `Dish.Status = Archived` после успешного выполнения.
- `Dish.PublishedVersionData IS NULL` (обнуляется, если было).
- `Dish.PublishedAt IS NULL`, `Dish.PublishedVersionUpdatedAt IS NULL`.
- `DishCategoryPublished` / `DishTagPublished` для этого блюда — пусто.
- `Dish.UpdatedAt = utcNow`.
- `Dish.CreatedAt` — не изменяется.
- `Dish.MainImageId`, `RecipeStep.ImageMediaId`, `Dish.HistoryText` и все основные данные агрегата остаются нетронутыми.
- Поднимается доменное событие `DishArchivedEvent(DishId, AuthorUserId)`.

---

## Main Flow (Основной поток)

1. Автор открыл страницу управления блюдом и нажал «Архивировать».
2. Клиент шлёт `POST /api/dishes/{id}/archive` с заголовком `Authorization`.
3. `DishesController.ArchiveAsync` создаёт `ArchiveDishCommand(DishId = id)` и отправляет в MediatR.
4. `ArchiveDishCommandValidator` проверяет `DishId != Guid.Empty`.
5. `ArchiveDishCommandHandler` загружает `Dish` через `IDishRepository.GetByIdAsync`.
6. Проверка владения: `dish.AuthorUserId == _currentUser.UserId || _currentUser.IsInRole(PlatformRoles.ADMIN)` — иначе `403 NOT_DISH_OWNER`.
7. Domain-метод `dish.Archive(utcNow)`:
   - Проверяет `Status != Archived`; иначе — `409 DISH_ALREADY_ARCHIVED`.
   - Переключает `Status` в `Archived`.
   - Обнуляет `PublishedAt`, `PublishedVersionData`, `PublishedVersionUpdatedAt`.
   - Очищает `_categoriesPublished` и `_tagsPublished`.
   - Обновляет `UpdatedAt`.
   - Поднимает `DishArchivedEvent`.
8. `_dishRepository.SaveChangesAsync` — один транзакционный коммит.
9. `_eventDispatcher.DispatchAsync` — публикация доменных событий (Этап 2: подписчиков нет).
10. Контроллер возвращает `204 No Content`.

---

## Alternative Flows (Альтернативные потоки)

Нет — у UC только один успешный путь.

---

## Edge Cases (Граничные случаи)

- **EC-1: Блюдо отсутствует.** `GetByIdAsync` вернёт `null` → `404 DISH_NOT_FOUND`.
- **EC-2: Пользователь не автор и не Admin.** `dish.AuthorUserId != actorUserId && !isAdmin` → `403 NOT_DISH_OWNER`. Domain не вызывается. Если у пользователя есть роль `Admin`, проверка проходит.
- **EC-3: Блюдо уже `Archived`.** Domain → `409 DISH_ALREADY_ARCHIVED`. UI должен скрывать кнопку, но серверная защита обязательна.
- **EC-4: Блюдо в `Draft` без MainImage / шагов.** Архивируется без проблем: инварианты публикации к Archive не применяются. Архив — это конечное состояние, а не подготовка к публикации.
- **EC-5: Блюдо в `Published` с заполненным `PublishedVersionData`.** Снепшот обнуляется, `*Published`-связки очищаются — посетители каталога перестают видеть блюдо. Связь `Orders → DishSnapshot` (Этап 6+) сохраняется через собственный снепшот в OrderItem.
- **EC-6: Восстановление из `Archived`.** На Этапе 2 для автора недоступно. Admin-UC появится на Этапе 8+ (см. `private_TODO-будущие-этапы.md`).
- **EC-7: Hard delete.** Никаких физических удалений данных. Hard delete блюда — Этап 8+ (`UC-DSH-XXX`, отдельный admin-UC); тогда же будет каскад на медиа через `IMediaService.DeleteByEntityAsync`.

---

## Postconditions (Постусловия)

При успешном выполнении:

- `Dish.Status = Archived`.
- Поля `PublishedVersionData`, `PublishedAt`, `PublishedVersionUpdatedAt` обнулены.
- `DishCategoryPublished` / `DishTagPublished` для этого блюда удалены.
- Основные таблицы агрегата сохранены неизменными (`Recipe`, `RecipeStep`, `RecipeIngredient`, `DishCategory`, `DishTag`).
- Привязанные медиафайлы остаются в `EntityType + EntityId`, в статусе `Ready` — для целостности будущих снепшотов модуля Orders и возможного восстановления.
- `Dish.UpdatedAt` обновлено.
- Доменное событие `DishArchivedEvent` отправлено в `IDomainEventDispatcher`.
- Каталог / поиск (UC-DSH-054) не показывают блюдо.
- Прямая ссылка `GET /api/dishes/{id}` возвращает `404` всем — даже автору на Этапе 2 (статус `Archived` всегда отдаётся как 404).

При неуспехе (400 / 401 / 403 / 404 / 409):

- Состояние БД не меняется.
- Доменные события не отправляются.

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Не идемпотентен: повторный вызов вернёт `409 DISH_ALREADY_ARCHIVED`. С точки зрения публичных эффектов — повторный запрос ничего не меняет.
- **Performance.** Целевое < 50 мс. Один `SELECT` корневого `Dish` + `UPDATE` + `DELETE`-ы по `*Published`-таблицам.
- **Consistency.** Read Committed. Одна транзакция охватывает все изменения.
- **Audit.** Стандартное HTTP-логирование. Отдельный аудит факта архивирования / журнал «архивированных автором» — не реализован, может появиться на Этапе 8+ для админских отчётов.

---

## Реализация Этапа 2 — что в наличии и что отложено

### Реализовано

- Command + Validator + Handler.
- Endpoint `POST /api/dishes/{id:guid}/archive` с политикой `VALID_ACTOR`.
- Доменный метод `Dish.Archive(utcNow)` с проверкой инварианта статуса.
- Событие `DishArchivedEvent(DishId, AuthorUserId)`.
- POL-001 (Author + Admin).
- Поведение «связанные медиа остаются» — без вызова `IMediaService`.
- Поведение `Archived → 404` для всех — реализовано в UC-DSH-050 / UC-DSH-052.

### Отложено

- **Восстановление из Archived автору.** Не реализуем. Возможно появление admin-UC на Этапе 8+.
- **Hard delete блюда с каскадом по медиа.** Этап 8+. Используется `IMediaService.DeleteByEntityAsync`.
- **Подписчики `DishArchivedEvent`.** Этап 5+ — Notifications (уведомление подписчиков о том, что блюдо снято автором).
- **Доступ admin к архивированным блюдам.** Этап 8+ — `GET /api/dishes/{id}?includeArchived=true` или отдельный admin-эндпоинт.

---

## Связанные документы

- `docs/public/policies/POL-001-dish-ownership.md` — политика авторизации (включая правило «Archived → только Admin» на Этапе 8+).
- `docs/public/modules/dishes/domain-model.md` — `Dish.Archive`, `DishArchivedEvent`.
- `docs/public/modules/dishes/use-cases/UC-DSH-005-UnpublishDish.md` — параллельный сценарий обнуления публичной версии.
- `docs/public/modules/dishes/use-cases/UC-DSH-050-GetDishById.md` — реализует `Archived → 404` поведение.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC.
