# UC-DSH-002: Обновить публичную карточку блюда

**Version:** 1.0 | **Date:** 2026-05-24

---

## Actors (Инициаторы)

- Primary: Автор блюда (`Dish.AuthorUserId == ActorUserId`). На Этапе 8+ — также `Moderator` / `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `Dish` — существующий агрегат каталога.
- Identifier: `Id` (`Guid`) — передаётся в path-параметре эндпоинта.
- Action: Update (партикулярные поля карточки).

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required (JWT Bearer в заголовке `Authorization`).

### Authorization (Авторизация)

- Policy: **`AuthorizationPolicies.VALID_ACTOR`** — гарантирует наличие валидного `Guid` в claim `sub` на уровне инфраструктуры. Применяется атрибутом `[Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]`.
- Ownership (POL-001): пользователь может модифицировать **только своё** блюдо. Проверка выполняется прямой сверкой `dish.AuthorUserId == _currentUser.UserId!.Value` в Handler-е. При несовпадении возвращается `DishesErrors.NotDishOwner` (`HTTP 403`).
- Roles: на Этапе 2 — только автор. Расширения для `Admin` / `Moderator` (Этап 8+) — в `POL-001-dish-ownership.md` §5.

### State Constraints (Ограничения по состоянию)

На Этапе 2 — нет ограничений по `Dish.Status`: карточку можно обновлять в любом статусе (`Draft`, `Published`, `Unpublished`, `Archived`).

> Запрет редактирования `Archived`-блюд для всех, кроме `Admin`, появится на Этапе 8+ вместе с моделью статусных ограничений в POL-001 §4.

### Contextual Constraints (Контекстуальные ограничения)

N/A на Этапе 2.

---

## API Contract (Контракт API)

### Endpoint

```
PATCH /api/dishes/{id}
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Headers:**

- `Authorization: Bearer <JWT>` — обязательный.
- `Content-Type: application/json` — обязательный.

**Body (JSON):**

| Поле | Тип | Обяз. | Ограничения | Описание |
|------|-----|-------|-------------|----------|
| `name` | `string` | ✅ | длина 3–200 | Новое название блюда |
| `difficultyLevel` | `string` (enum) | ✅ | `Easy` \| `Medium` \| `Hard` \| `Pro` | Уровень сложности |
| `costEstimate` | `string` (enum) | ✅ | `Budget` \| `Moderate` \| `Expensive` | Оценка стоимости |
| `shortDescription` | `string?` | ✗ | длина ≤ 500 | Краткая подводка. `null` — очистить |
| `description` | `string?` | ✗ | длина ≤ 4000 | Полное описание (markdown). `null` — очистить |

**Не принимаются:**

- `ownerType` — резолвится сервером из ролей текущего пользователя через `OwnerTypeResolver` (приоритет Restaurant > Chef > User). При смене роли автора следующий `UpdateCard` переопределит `OwnerType`.
- `dietLabelsMask` → отдельный сценарий **UC-DSH-009 SetDietLabels**.
- `mainImageId` → отдельный сценарий **UC-DSH-011 ChangeDishMainImage**.
- `historyText` → отдельный сценарий **UC-DSH-010 SetHistory**.
- `slug` — не меняется автоматически даже при смене `Name`. Регенерация — отдельный admin-only сценарий (UC-DSH-140, Этап 8+).

### Response (Ответ)

**Success:**

- Status: `204 No Content`.
- Body: нет.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | Провал FluentValidation: `DishId` пустой, `Name` вне диапазона 3–200, неизвестный enum, `Description` > 4000, `ShortDescription` > 500 |
| 401 | — | JWT отсутствует, просрочен или невалиден |
| 403 | — | Политика `VALID_ACTOR` не пропустила запрос (claim `sub` отсутствует или невалиден) |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не является автором блюда |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо с указанным `Id` не существует |

---

## Preconditions (Предусловия)

- Пользователь аутентифицирован (валидный JWT в заголовке `Authorization`).
- Политика `VALID_ACTOR` пропустила запрос: `_currentUser.UserId` гарантированно содержит `Guid`.
- Блюдо с указанным `Id` существует в БД.
- `dish.AuthorUserId == _currentUser.UserId.Value` (POL-001).

---

## Invariants (Инварианты домена)

Гарантируются доменной моделью и методом `Dish.UpdateCard(...)`:

- `Dish.AuthorUserId` не меняется — иммутабельно после создания.
- `Dish.Status` не меняется — переходы статусов выполняются отдельными методами (`Publish`, `Unpublish`, `Archive`).
- `Dish.Slug` не меняется — независимо от изменения `Name`.
- `Dish.MainImageId` не меняется — отдельный метод `ChangeMainImage`.
- `Dish.DietLabelsMask`, `Dish.HistoryText` не меняются — отдельные методы `SetDietLabels`, `UpdateHistory`.
- `Dish.PublishedVersionData` и `Dish.PublishedAt` / `Dish.PublishedVersionUpdatedAt` не меняются — публичная версия обновляется только через явный `Publish`.
- `Dish.UpdatedAt = utcNow` — фиксирует время правки. Условие `Dish.UpdatedAt > Dish.PublishedAt` после операции — индикатор «есть несохранённые правки относительно публичной версии» для UI.
- Поднимается доменное событие `DishUpdatedEvent { DishId, AuthorUserId }`.

---

## Main Flow (Основной поток)

1. **Запрос.** Клиент отправляет `PATCH /api/dishes/{id}` с JWT и JSON-body.
2. **Аутентификация.** ASP.NET Core Authentication middleware валидирует JWT → заполняет `HttpContext.User` claims-ами.
3. **Авторизация — Policy.** Политика `VALID_ACTOR` проверяет, что claim `sub` парсится в `Guid`. Если нет — `403 Forbidden`, Handler не вызывается.
4. **Контроллер.** `DishesController.UpdateCardAsync(Guid id, UpdateDishCardRequest body, CancellationToken ct)`:
   1. Собирает `UpdateDishCardCommand` (DishId из path, остальные поля из body).
   2. Делегирует MediatR через `ISender.Send(command, ct)`.
5. **Валидация.** `ValidationBehavior<UpdateDishCardCommand, Result>` запускает `UpdateDishCardCommandValidator`:
   - `DishId`: `NotEmpty`.
   - `Name`: `NotEmpty`, `MinimumLength(3)`, `MaximumLength(200)`.
   - `DifficultyLevel`, `CostEstimate`: `IsInEnum`.
   - `ShortDescription`: `MaximumLength(500)` (если задано).
   - `Description`: `MaximumLength(4000)` (если задано).
6. **Handler — `UpdateDishCardCommandHandler.Handle(...)`:**
   1. **Загрузка.** `Dish? dish = await _dishRepository.GetByIdAsync(request.DishId, ct)`. `GetByIdAsync` — без рецепта (правка карточки не трогает Recipe и связанные сущности).
   2. **NotFound.** Если `dish is null` → `Result.Failure(DishesErrors.DishNotFound)` → `404`.
   3. **Ownership (POL-001).** `actorUserId = _currentUser.UserId!.Value`. Если `dish.AuthorUserId != actorUserId` → `Result.Failure(DishesErrors.NotDishOwner)` → `403`.
   4. **OwnerType.** `ownerType = OwnerTypeResolver.ResolveFromRoles(_currentUser.Roles)` (приоритет Restaurant > Chef > User).
   5. **utcNow.** `var utcNow = _clock.UtcNow`.
   6. **Применение изменений.** `dish.UpdateCard(name, shortDescription, description, difficultyLevel, costEstimate, ownerType, utcNow)`. Domain поднимает `DishUpdatedEvent`.
   7. **Сохранение.** `await _dishRepository.SaveChangesAsync(ct)` — один транзакционный коммит (Unit of Work). Изменённый dish уже tracked, отдельный `Add` не нужен.
   8. **Доменные события.** После сохранения собранные `dish.DomainEvents` публикуются вручную через `IPublisher`, затем `dish.ClearDomainEvents()`.
   9. **Результат.** `return Result.Success()`.
7. **Маппинг ответа.** `ApiController.MapResult(Result)` → `204 No Content` при успехе, `400` / `403` / `404` при ошибках по правилам `MapError`.

---

## Alternative Flows (Альтернативные потоки)

N/A — нет альтернативных путей успешного завершения.

---

## Edge Cases (Граничные случаи)

- **EC-1. Concurrent updates.** Два запроса A и B обновляют одно и то же блюдо параллельно. Оба загружают `dish`, оба применяют `UpdateCard`, оба коммитят. Один из них «перепишет» изменения другого — last-write-wins. На Этапе 2 это **принятое поведение** (не используется оптимистичная блокировка через `RowVersion`). На Этапе 4+ можно добавить `RowVersion` в `Dish` для обнаружения конкурентных правок и возврата `409 Conflict`.
- **EC-2. `_currentUser.UserId is null`.** Теоретически невозможно: политика `VALID_ACTOR` отклонит запрос до Handler-а. Handler использует `_currentUser.UserId!.Value` без проверки.
- **EC-3. Блюдо в статусе `Archived`.** На Этапе 2 редактирование разрешено — Handler не проверяет `Status`. Запрет появится на Этапе 8+ вместе с моделью статусных ограничений (POL-001 §4).
- **EC-4. Изменение `Name` на существующее у другого блюда.** Допускается — уникальность гарантируется только `Slug`, а `Slug` при `UpdateCard` не пересчитывается. Два разных блюда могут иметь одинаковое `Name`.
- **EC-5. Все опциональные поля = `null`.** Допускается. `dish.UpdateCard(...)` явно очистит `ShortDescription` и `Description` в БД.
- **EC-6. Команда не содержит изменений (все поля совпадают с текущими).** Handler не сравнивает значения с текущими — вызов `UpdateCard` происходит безусловно, `UpdatedAt` обновляется, событие поднимается. Это намеренное поведение: API не «угадывает» намерение клиента, а выполняет ровно то, что запрошено.

---

## Postconditions (Постусловия)

При успехе (Status 204):

- В таблице `dishes.Dishes` поля `Name`, `ShortDescription`, `Description`, `DifficultyLevel`, `CostEstimate`, `OwnerType`, `UpdatedAt` обновлены.
- `Dish.UpdatedAt = utcNow` (одно время для всех изменённых полей).
- Прочие поля `Dish` не изменились (`AuthorUserId`, `Slug`, `Status`, `MainImageId`, `DietLabelsMask`, `HistoryText`, `PublishedVersionData`, счётчики и т.п.).
- Поднято доменное событие `DishUpdatedEvent { DishId, AuthorUserId }` (publish через `IPublisher`).
- На Этапе 2 нет подписчиков на это событие — оно «выстреливает вхолостую». На Этапе 5+ появятся handlers (инвалидация кэшей, переиндексация в поиске).

При неуспехе (любой не-2xx):

- Никаких изменений в БД — `SaveChangesAsync` не вызвался (ошибка возникла до него), либо EF Core отбросит tracked entity вместе со scope DbContext.

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Идемпотентно при одинаковом body: повторный запрос приведёт к тому же состоянию `Dish` (поля те же), но `UpdatedAt` обновится каждый раз и `DishUpdatedEvent` будет подниматься повторно. Для полной идемпотентности нужен `Idempotency-Key` header — Этап 4+.
- **Rate Limit.** Не реализован на Этапе 2. На Этапе 4+ — общий лимит на `/api/*` через `AddRateLimiter()`.
- **Performance.** Целевое < 50 мс. Профиль:
  - 1 SELECT по PK (`GetByIdAsync` без рецепта).
  - 1 UPDATE.
  - 1 коммит транзакции.
- **Consistency.** Strong consistency в рамках одной транзакции — карточка обновляется атомарно. `PublishedVersionData` не трогается, посетители продолжают видеть прежнюю публичную версию до явного `Publish`.
- **Audit.** Логирование через Serilog (`ILogger<UpdateDishCardCommandHandler>` — добавится при реальной потребности). `DishUpdatedEvent` фиксирует факт правки.

---

## Связанные документы

- `docs/public/modules/dishes/domain-model.md` — определение агрегата `Dish` и метода `Dish.UpdateCard(...)`.
- `docs/public/policies/POL-001-dish-ownership.md` — правила авторизации модификации блюд.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC модуля.
- `docs/public/modules/dishes/use-cases/UC-DSH-001-CreateDishDraft.md` — создание блюда (для сопоставления с обновлением).
