# UC-DSH-003: Обновить рецепт

**Version:** 1.0 | **Date:** 2026-05-28

---

## Actors (Инициаторы)

- Primary: Автор блюда (`Dish.AuthorUserId == ActorUserId`). На Этапе 8+ — также `Moderator` / `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `Recipe` — часть агрегата `Dish` (1:1). Доступ только через корень агрегата.
- Identifier: `Dish.Id` (`Guid`) — передаётся в path-параметре эндпоинта. Сам `Recipe.Id` клиенту не нужен.
- Action: Update (атомарный бандл простых полей рецепта).

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required (JWT Bearer в заголовке `Authorization`).

### Authorization (Авторизация)

- Policy: **`AuthorizationPolicies.VALID_ACTOR`** — гарантирует наличие валидного `Guid` в claim `sub` на уровне инфраструктуры. Применяется атрибутом `[Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]`.
- Ownership (POL-001): пользователь может модифицировать **только своё** блюдо. Проверка выполняется прямой сверкой `dish.AuthorUserId == _currentUser.UserId!.Value` в Handler-е. При несовпадении возвращается `DishesErrors.NotDishOwner` (`HTTP 403`).
- Roles: на Этапе 2 — только автор. Расширения для `Admin` / `Moderator` (Этап 8+) — в `POL-001-dish-ownership.md` §5.

### State Constraints (Ограничения по состоянию)

На Этапе 2 — нет ограничений по `Dish.Status`: рецепт можно обновлять в любом статусе (`Draft`, `Published`, `Unpublished`, `Archived`).

> Запрет редактирования `Archived`-блюд для всех, кроме `Admin`, появится на Этапе 8+ вместе с моделью статусных ограничений в POL-001 §4.

### Contextual Constraints (Контекстуальные ограничения)

N/A на Этапе 2.

---

## API Contract (Контракт API)

### Endpoint

```
PUT /api/dishes/{id}/recipe
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда (корень агрегата).

**Headers:**

- `Authorization: Bearer <JWT>` — обязательный.
- `Content-Type: application/json` — обязательный.

**Body (JSON):**

| Поле | Тип | Обяз. | Ограничения | Описание |
|------|-----|-------|-------------|----------|
| `introductionText` | `string?` | ✗ | длина ≤ 4000 | Вводный текст рецепта. `null` — очистить |
| `servingsDefault` | `int` | ✅ | `≥ 1` | Количество порций по умолчанию |
| `isAlcoholic` | `bool` | ✅ | — | Признак содержания алкоголя |
| `authorTips` | `string?` | ✗ | длина ≤ 4000 | Советы автора по приготовлению. `null` — очистить |
| `servingSuggestions` | `string?` | ✗ | длина ≤ 4000 | Рекомендации по сервировке. `null` — очистить |
| `notes` | `string?` | ✗ | длина ≤ 4000 | Дополнительные заметки. `null` — очистить |

**Не принимаются (отдельные UC):**

- Шаги рецепта (`RecipeStep`) → **UC-DSH-020..023**.
- Ингредиенты (`RecipeIngredient`) → **UC-DSH-030..033**.
- Тайминг (`Timing`) → **UC-DSH-040 SetTiming**.
- Выход (`Yield`) → **UC-DSH-041 SetYield**.
- КБЖУ (`Nutrition`) → **UC-DSH-042 SetNutrition**.

### Response (Ответ)

**Success:**

- Status: `204 No Content`.
- Body: нет.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | Провал FluentValidation: `DishId` пустой, `ServingsDefault < 1`, любое текстовое поле > 4000 |
| 400 | `DISHES.INVALID_SERVINGS_DEFAULT` | Доменный инвариант сработал (теоретически — валидатор отсекает раньше) |
| 401 | — | JWT отсутствует, просрочен или невалиден |
| 403 | — | Политика `VALID_ACTOR` не пропустила запрос |
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

Гарантируются доменной моделью и методом `Dish.UpdateRecipe(...)`:

- `Recipe.ServingsDefault ≥ 1` — проверяется первым делом через `Recipe.SetServingsDefault(...)`. При нарушении ни одно поле не применяется (атомарность).
- `Recipe.DishId` не меняется — иммутабельно после создания.
- `Recipe.Steps`, `Recipe.Ingredients`, `Recipe.Timing`, `Recipe.Yield`, `Recipe.Nutrition` не меняются — отдельные UC и Domain-методы.
- `Dish.PublishedVersionData`, `Dish.PublishedAt`, `Dish.PublishedVersionUpdatedAt` не меняются — публичная версия обновляется только через явный `Publish`.
- `Dish.UpdatedAt = utcNow` — фиксируется через `MarkAsUpdated(utcNow)` в конце успешного обновления. Условие `Dish.UpdatedAt > Dish.PublishedAt` после операции — индикатор «есть несохранённые правки относительно публичной версии» для UI.
- Поднимается ровно **одно** доменное событие `DishUpdatedEvent { DishId, AuthorUserId }`, независимо от количества фактически изменённых полей.

---

## Main Flow (Основной поток)

1. **Запрос.** Клиент отправляет `PUT /api/dishes/{id}/recipe` с JWT и JSON-body.
2. **Аутентификация.** ASP.NET Core Authentication middleware валидирует JWT → заполняет `HttpContext.User`.
3. **Авторизация — Policy.** Политика `VALID_ACTOR` проверяет валидность claim `sub`. Если нет — `403 Forbidden`.
4. **Контроллер.** `DishesController.UpdateRecipeAsync(Guid id, UpdateRecipeRequest body, CancellationToken ct)`:
   1. Собирает `UpdateRecipeCommand(DishId, IntroductionText, ServingsDefault, IsAlcoholic, AuthorTips, ServingSuggestions, Notes)` из path + body.
   2. Делегирует MediatR через `ISender.Send(command, ct)`.
5. **Валидация.** `ValidationBehavior<UpdateRecipeCommand, Result>` запускает `UpdateRecipeCommandValidator`:
   - `DishId`: `NotEmpty`.
   - `ServingsDefault`: `GreaterThanOrEqualTo(1)`.
   - `IntroductionText`, `AuthorTips`, `ServingSuggestions`, `Notes`: `MaximumLength(4000)` (если заданы).
6. **Handler — `UpdateRecipeCommandHandler.Handle(...)`:**
   1. **Загрузка.** `Dish? dish = await _dishRepository.GetByIdWithRecipeAsync(request.DishId, ct)`. `GetByIdWithRecipeAsync` подгружает `Recipe` с его 1:1-связками (`Timing`, `Yield`, `Nutrition`), но без подколлекций `Steps`/`Ingredients`, которые здесь не используются.
   2. **NotFound.** Если `dish is null` → `return DishesErrors.DishNotFound` → `404`.
   3. **Ownership (POL-001).** `actorUserId = _currentUser.UserId!.Value`. Если `dish.AuthorUserId != actorUserId` → `return DishesErrors.NotDishOwner` → `403`.
   4. **utcNow.** `var utcNow = _clock.UtcNow`.
   5. **Применение изменений.** `Result updateResult = dish.UpdateRecipe(introductionText, servingsDefault, isAlcoholic, authorTips, servingSuggestions, notes, utcNow)`. Внутри: `Recipe.SetServingsDefault(...)` первой — при `IsFailure` ни одно поле не применяется; затем простые setter-ы `Recipe`; в конце `MarkAsUpdated(utcNow)`, который поднимает `DishUpdatedEvent`.
   6. **Защита от доменной ошибки.** Если `updateResult.IsFailure` → возврат ошибки без сохранения.
   7. **Сохранение.** `await _dishRepository.SaveChangesAsync(ct)` — один транзакционный коммит.
   8. **Доменные события.** Собранные `dish.DomainEvents` публикуются вручную через `IPublisher`, затем `dish.ClearDomainEvents()`.
   9. **Результат.** `return Result.Success()`.
7. **Маппинг ответа.** `ApiController.MapResult(Result)` → `204 No Content` при успехе.

---

## Alternative Flows (Альтернативные потоки)

N/A — нет альтернативных путей успешного завершения.

---

## Edge Cases (Граничные случаи)

- **EC-1. Concurrent updates.** Два запроса A и B параллельно обновляют рецепт одного блюда. Оба загружают `dish` с `Recipe`, оба применяют `UpdateRecipe`, оба коммитят. Один из них «перепишет» изменения другого — last-write-wins. На Этапе 2 это **принятое поведение** (нет оптимистичной блокировки). На Этапе 4+ — возможен `RowVersion` в `Dish` для `409 Conflict`.
- **EC-2. `_currentUser.UserId is null`.** Теоретически невозможно: политика `VALID_ACTOR` отклонит запрос до Handler-а. Handler использует `_currentUser.UserId!.Value`.
- **EC-3. Блюдо в статусе `Archived`.** На Этапе 2 редактирование разрешено. Запрет появится на Этапе 8+.
- **EC-4. `ServingsDefault = 0` или отрицательное.** Валидатор отдаст `400 VALIDATION.ERROR` до Handler-а. Доменный инвариант `Recipe.SetServingsDefault` — defense-in-depth.
- **EC-5. Все опциональные поля = `null`.** Допускается. Текстовые поля будут очищены в БД.
- **EC-6. Команда не содержит изменений (все поля совпадают с текущими).** Handler не сравнивает значения с текущими — вызов `UpdateRecipe` происходит безусловно, `UpdatedAt` обновляется, событие поднимается. Это намеренное поведение.
- **EC-7. Блюдо без существующего `Recipe`.** Теоретически невозможно: `Recipe` создаётся вместе с `Dish` в фабрике `Dish.Create(...)`. Если кто-то вручную удалит запись из БД — Handler упадёт с `NullReferenceException` при `Recipe.SetServingsDefault`. Это считается инвариантом данных, не сценарием для обработки.

---

## Postconditions (Постусловия)

При успехе (Status 204):

- В таблице `dishes.Recipes` поля `IntroductionText`, `ServingsDefault`, `IsAlcoholic`, `AuthorTips`, `ServingSuggestions`, `Notes` обновлены.
- В таблице `dishes.Dishes` поле `UpdatedAt = utcNow` (через `MarkAsUpdated`, либо через `SaveChangesInterceptor` по факту изменения связанной сущности `Recipe`).
- Прочие поля `Recipe` (`DishId`, `NutritionId`, `Steps`, `Ingredients`) и связки `Timing` / `Yield` / `Nutrition` не изменились.
- `PublishedVersionData`, `PublishedAt`, `PublishedVersionUpdatedAt` не изменились.
- Поднято одно событие `DishUpdatedEvent { DishId, AuthorUserId }` через `IPublisher`. На Этапе 2 подписчиков нет; на Этапе 5+ — инвалидация кэшей, переиндексация.

При неуспехе (любой не-2xx):

- Никаких изменений в БД.

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Идемпотентно при одинаковом body: повторный запрос приведёт к тому же состоянию `Recipe`, но `UpdatedAt` обновится и `DishUpdatedEvent` будет подниматься повторно. Для полной идемпотентности нужен `Idempotency-Key` — Этап 4+.
- **Rate Limit.** Не реализован на Этапе 2.
- **Performance.** Целевое < 50 мс. Профиль:
  - 1 SELECT `Dish` + `Recipe` + 1:1-связки (через `GetByIdWithRecipeAsync`).
  - 1 UPDATE `Recipe` + 1 UPDATE `Dish` (для `UpdatedAt`).
  - 1 коммит транзакции.
- **Consistency.** Strong consistency в рамках одной транзакции — атомарный бандл всех 6 полей.
- **Audit.** Логирование через Serilog. `DishUpdatedEvent` фиксирует факт правки.

---

## Связанные документы

- `docs/public/modules/dishes/domain-model.md` — определение агрегата `Dish` и `Recipe`, методов `Dish.UpdateRecipe(...)` / `Recipe.SetServingsDefault(...)`.
- `docs/public/policies/POL-001-dish-ownership.md` — правила авторизации модификации блюд.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC модуля.
- `docs/public/modules/dishes/use-cases/UC-DSH-002-UpdateDishCard.md` — обновление карточки блюда (симметричный паттерн).
