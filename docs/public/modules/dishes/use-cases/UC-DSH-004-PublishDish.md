# UC-DSH-004: Опубликовать блюдо

**Version:** 1.0 (MVP — snapshot без денормализации имён справочников) | **Date:** 2026-05-30

---

## Actors (Инициаторы)

- Primary: Автор блюда (`Dish.AuthorUserId == ActorUserId`). На Этапе 8+ — также `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `Dish` — корень агрегата каталога. Затрагиваются также подчинённые сущности (`Recipe`, `RecipeStep`, `RecipeIngredient`, `Timing`, `Yield`, `Nutrition`, `DishCategory`, `DishTag`, `DishCategoryPublished`, `DishTagPublished`) — все в рамках одной транзакции.
- Identifier: `Dish.Id` (`Guid`) — передаётся в path-параметре эндпоинта.
- Action: Lifecycle transition + snapshot build.

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required (JWT Bearer в заголовке `Authorization`).

### Authorization (Авторизация)

- Policy: **`AuthorizationPolicies.VALID_ACTOR`** — гарантирует наличие валидного `Guid` в claim `sub`. Применяется атрибутом `[Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]`.
- Ownership (POL-001): пользователь может публиковать **только своё** блюдо. Проверка выполняется прямой сверкой `dish.AuthorUserId == _currentUser.UserId!.Value` в Handler-е. При несовпадении возвращается `DishesErrors.NotDishOwner` (`HTTP 403`).
- Roles: на Этапе 2 — только автор. Расширения для `Admin` (Этап 8+) и `Moderator` — в `POL-001-dish-ownership.md` §5.

### State Constraints (Ограничения по состоянию)

- `Dish.Status = Archived` → `409 DISHES.CANNOT_PUBLISH_ARCHIVED_DISH`. Архивированное блюдо нельзя вернуть в публикацию автору. На Этапе 8+ восстановление из `Archived` будет отдельным админским UC.
- `Dish.Status = Published && UpdatedAt <= PublishedAt` → `409 DISHES.DISH_ALREADY_PUBLISHED`. Защита от спама `DishPublishedEvent` — см. [ADR-0013](../../../adr/ADR-0013-publish-spam-protection.md).
- `MainImageId IS NULL` → `409 DISHES.MAIN_IMAGE_REQUIRED_FOR_PUBLISH`.
- `Recipe.Steps.Count == 0` → `409 DISHES.STEPS_REQUIRED_FOR_PUBLISH`.
- `Recipe.Ingredients.Count == 0` → `409 DISHES.INGREDIENTS_REQUIRED_FOR_PUBLISH`.
- `Recipe.Timing.TotalTimeMinutes <= 0` → `409 DISHES.TIMING_REQUIRED_FOR_PUBLISH`.

Все эти проверки выполняются в Domain-методе `Dish.Publish(...)` в фиксированном порядке (см. Main Flow §6).

### Contextual Constraints (Контекстуальные ограничения)

На Этапе 8+ при `Dish.ModerationStatus = Pending` публикация недоступна автору — ждёт решения модератора. Сейчас все блюда автоматически `Approved` по умолчанию, ограничение неактивно.

---

## API Contract (Контракт API)

### Endpoint

```
POST /api/dishes/{id}/publish
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Headers:**

- `Authorization: Bearer <JWT>` — обязательный.

Body: отсутствует. Публикация — переход состояния на основе уже сохранённого содержимого блюда; никакого ввода от клиента не требуется.

### Response (Ответ)

**Success:**

- Status: `204 No Content`.
- Body: нет.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | `DishId = Guid.Empty` (на практике маршрут `{id:guid}` отсечёт невалидные значения на уровне ASP.NET Core). |
| 401 | — | JWT отсутствует, просрочен или невалиден. |
| 403 | — | Политика `VALID_ACTOR` не пропустила запрос (claim `sub` не парсится в `Guid`). |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не является автором блюда. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо с указанным `Id` не существует. |
| 409 | `DISHES.CANNOT_PUBLISH_ARCHIVED_DISH` | Блюдо в статусе `Archived`. |
| 409 | `DISHES.DISH_ALREADY_PUBLISHED` | Блюдо уже опубликовано и в нём нет несохранённых правок (`UpdatedAt <= PublishedAt`). |
| 409 | `DISHES.MAIN_IMAGE_REQUIRED_FOR_PUBLISH` | Не задано главное фото (`MainImageId IS NULL`). |
| 409 | `DISHES.STEPS_REQUIRED_FOR_PUBLISH` | Рецепт не содержит ни одного шага. |
| 409 | `DISHES.INGREDIENTS_REQUIRED_FOR_PUBLISH` | Рецепт не содержит ни одного ингредиента. |
| 409 | `DISHES.TIMING_REQUIRED_FOR_PUBLISH` | Общее время приготовления равно нулю. |

---

## Preconditions (Предусловия)

- Пользователь аутентифицирован (валидный JWT в заголовке `Authorization`).
- Политика `VALID_ACTOR` пропустила запрос: `_currentUser.UserId` гарантированно содержит `Guid`.
- Блюдо с указанным `Id` существует в БД.
- `dish.AuthorUserId == _currentUser.UserId.Value` (POL-001).
- Содержательные инварианты публикации выполнены (см. State Constraints).

---

## Invariants (Инварианты домена)

Гарантируются Domain-методом `Dish.Publish(...)`:

- **Lifecycle.** Переходы статуса: `Draft → Published`, `Published → Published` (с правками), `Unpublished → Published`. Переход из `Archived` запрещён.
- **Атомарность.** Либо в одной транзакции применяются все изменения (статус, снепшот, `*Published`-таблицы, метки времени), либо ни одно.
- **`DishPublishedEvent` поднимается тогда и только тогда, когда фактически произошёл переход в `Published`** со сменой `PublishedVersionData`/`PublishedAt` — см. [ADR-0013](../../../adr/ADR-0013-publish-spam-protection.md).
- **`PublishedAt` фиксирует время последней публикации.** При каждой успешной публикации `PublishedAt = utcNow`, что определяет индикатор «есть несохранённые правки» (`UpdatedAt > PublishedAt`) для последующих запросов.
- **`*Published`-таблицы (`DishCategoryPublished`, `DishTagPublished`) полностью заменяются** из текущих `DishCategory`/`DishTag` при каждой публикации. Их содержимое — источник истины для каталожного фильтра (UC-DSH-054), не основные таблицы.
- **Полиморфизм ингредиентов в снепшоте** соблюдает [ADR-0012](../../../adr/ADR-0012-recipe-ingredient-discriminated-union.md): массив `Ingredients[]` содержит элементы с дискриминатором `"type": "catalog" | "freeform"`.

---

## Main Flow (Основной поток)

1. **Запрос.** Клиент отправляет `POST /api/dishes/{id}/publish` с JWT и без тела.
2. **Аутентификация.** ASP.NET Core Authentication middleware валидирует JWT → заполняет `HttpContext.User`.
3. **Авторизация — Policy.** Политика `VALID_ACTOR` проверяет валидность claim `sub`. Если нет — `403 Forbidden`.
4. **Контроллер.** `DishesController.PublishAsync(Guid id, CancellationToken ct)`:
   1. Собирает `PublishDishCommand(DishId: id)`.
   2. Делегирует MediatR через `ISender.Send(command, ct)`.
5. **Валидация.** `ValidationBehavior<PublishDishCommand, Result>` запускает `PublishDishCommandValidator`: `DishId: NotEmpty`. Содержательные инварианты публикации — задача Domain (шаг 6.5).
6. **Handler — `PublishDishCommandHandler.Handle(...)`:**
   1. **Полная загрузка.** `Dish? dish = await _dishRepository.GetByIdWithFullRecipeAsync(request.DishId, ct)` — `Recipe`, `Timing`, `Yield`, `Nutrition`, `Steps`, `Ingredients`, `Categories`, `Tags` подгружаются eager.
   2. **NotFound.** Если `dish is null` → `return DishesErrors.DishNotFound` → `404`.
   3. **Ownership (POL-001).** `actorUserId = _currentUser.UserId!.Value`. Если `dish.AuthorUserId != actorUserId` → `return DishesErrors.NotDishOwner` → `403`.
   4. **Сборка снепшота.** `string snapshot = _snapshotBuilder.Build(dish)`. Чистая функция от агрегата к JSON-строке; полиморфно по ADR-0012 для массива `Ingredients[]`. Формат — MVP (только Id справочников; имена не денормализуются, см. секцию ниже).
   5. **utcNow.** `var utcNow = _clock.UtcNow`.
   6. **Domain.Publish.** `Result publishResult = dish.Publish(utcNow, snapshot)`. Внутри последовательно проверяются: `Status != Archived`, защита от повторной публикации без правок (ADR-0013), `MainImageId IS NOT NULL`, `Steps.Count > 0`, `Ingredients.Count > 0`, `Timing.TotalTimeMinutes > 0`. При успехе: `Status = Published`, `PublishedAt = utcNow`, `PublishedVersionData = snapshot`, `PublishedVersionUpdatedAt = utcNow`, `UpdatedAt = utcNow`; пересобираются `*Published`-таблицы из текущих рабочих коллекций; поднимается `DishPublishedEvent { DishId, AuthorUserId }`.
   7. **Защита от доменной ошибки.** Если `publishResult.IsFailure` → возврат ошибки без сохранения.
   8. **Сохранение.** `await _dishRepository.SaveChangesAsync(ct)` — один транзакционный коммит. EF Core сам сгенерирует UPDATE для `Dish` + INSERT/DELETE для `DishCategoryPublished`/`DishTagPublished`.
   9. **Доменные события.** Собранные `dish.DomainEvents` публикуются через `IPublisher`, затем `dish.ClearDomainEvents()`.
   10. **Результат.** `return Result.Success()`.
7. **Маппинг ответа.** `ApiController.MapResult(Result)` → `204 No Content` при успехе.

---

## Alternative Flows (Альтернативные потоки)

- **AF-1 «Первая публикация» (`Draft → Published`).** Базовый сценарий первой публикации блюда автором. `PublishedAt` устанавливается впервые; `*Published`-таблицы заполняются из текущих `DishCategory`/`DishTag`; `DishPublishedEvent` поднимается.
- **AF-2 «Повторная публикация с правками» (`Published → Published`).** Блюдо уже опубликовано (`Status == Published`), но автор внёс правки после последней публикации (`UpdatedAt > PublishedAt`). Все шаги выполняются заново: пересборка снепшота, перезапись `*Published`-таблиц, обновление `PublishedAt = utcNow` и `PublishedVersionUpdatedAt = utcNow`. `DishPublishedEvent` поднимается (по ADR-0013 событие соответствует реальной публикации, тип ветки не различает).
- **AF-3 «Возврат с Unpublished» (`Unpublished → Published`).** Блюдо ранее было снято с публикации (UC-DSH-005). `PublishedVersionData = null`, `PublishedAt = null`. При успешной публикации статус возвращается в `Published`, заполняется новый снепшот, `*Published`-таблицы пересобираются из текущих рабочих. `DishPublishedEvent` поднимается.

---

## Edge Cases (Граничные случаи)

- **EC-1. Concurrent publish.** Два запроса `Publish` параллельно для одного блюда. Оба загружают агрегат, оба собирают снепшот, оба пытаются сохранить. Один коммит проходит, второй применяет изменения поверх — last-write-wins. На Этапе 2 это **принятое поведение** (без оптимистичной блокировки). При появлении `RowVersion` (Этап 4+) — `409 Conflict`. Защита от спама `DishPublishedEvent` (ADR-0013) **не** срабатывает в этой гонке, так как между загрузками и `dish.Publish` оба видят `UpdatedAt > PublishedAt`.
- **EC-2. Concurrent publish + update.** Автор одновременно `PUT /recipe` (UC-DSH-003) и `POST /publish`. Возможные результаты зависят от порядка сохранения транзакций. На Этапе 2 — принимаем last-write-wins; правки `PUT` могут оказаться неотражёнными в снепшоте, если их транзакция пришла после `Publish`.
- **EC-3. Блюдо без `Recipe`.** Теоретически невозможно: `Recipe` создаётся вместе с `Dish` в фабрике `Dish.Create(...)`. Если кто-то вручную удалит запись из БД — Builder упадёт с `NullReferenceException` при доступе к `dish.Recipe.Timing`. Это считается инвариантом данных, не сценарием для обработки.
- **EC-4. Огромный рецепт (много шагов, ингредиентов).** Размер снепшота растёт линейно. Для типичного рецепта (10 шагов × 15 ингредиентов) JSON ~5–15 КБ. Лимит PostgreSQL `jsonb` — 1 ГБ; конкретный лимит размера снепшота на Этапе 2 не накладывается. При появлении проблем — добавится валидатор размера или вынос частей в отдельные таблицы.
- **EC-5. Архивированное блюдо после успешной публикации.** Если другой запрос архивирует блюдо в перерыве между `Publish.Result.Success` и `SaveChangesAsync` — оптимистично сохранится Published-состояние поверх Archived. На Этапе 2 это принимается; защита через `RowVersion` — Этап 4+.
- **EC-6. `IsFailure` после успешной части.** Если `dish.Publish` вернул `Failure`, никакие изменения не применяются (Domain-метод проверяет инварианты **до** мутаций). `SaveChangesAsync` не вызывается. БД остаётся неизменной.

---

## Postconditions (Постусловия)

При успехе (`204`):

- `Dish.Status = Published`.
- `Dish.PublishedVersionData` содержит свежий JSON-снепшот.
- `Dish.PublishedAt = utcNow`.
- `Dish.PublishedVersionUpdatedAt = utcNow`.
- `Dish.UpdatedAt = utcNow` (синхронизировано с `PublishedAt` — `HasUnsavedChanges = false`).
- Записи в `DishCategoryPublished` и `DishTagPublished` для этого блюда полностью соответствуют текущим `DishCategory` и `DishTag`.
- Поднято одно событие `DishPublishedEvent { DishId, AuthorUserId }` через `IPublisher`. На Этапе 2 подписчиков нет; на Этапе 5+ — рассылка подписчикам автора, индексация для каталога.

При неуспехе (любой не-2xx):

- Никаких изменений в БД (Domain-метод не мутирует поля до прохождения всех проверок; `SaveChangesAsync` не вызывается).

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Семантически идемпотентно при отсутствии правок: повторный `POST /publish` после успешного первого вернёт `409 DISH_ALREADY_PUBLISHED` (а не повторно успешный `204`). Это намеренное поведение для защиты `DishPublishedEvent` (ADR-0013). Технического `Idempotency-Key` нет — Этап 4+.
- **Rate Limit.** Не реализован на Этапе 2.
- **Performance.** Целевое < 200 мс. Профиль:
  - 1 SELECT с полным агрегатом (`GetByIdWithFullRecipeAsync` — 8 включений).
  - Сборка JSON-снепшота в памяти.
  - 1 UPDATE `Dish` + перезапись связующих таблиц (DELETE + INSERT для `*Published`).
  - 1 коммит транзакции.
  - Публикация доменных событий (на Этапе 2 без подписчиков — ~0 мс).
- **Consistency.** Strong consistency в рамках одной транзакции. Read committed для конкурентных запросов.
- **Audit.** Стандартное логирование через Serilog. Факт публикации фиксируется поднятием `DishPublishedEvent`. Структурированные бизнес-логи отказов (по нарушению инвариантов) — отдельная задача техдолга (см. `private_TODO-будущие-этапы.md` §4.7).

---

## Формат jsonb-снепшота (MVP)

Снепшот хранится в `Dish.PublishedVersionData` как jsonb. Корневой объект — `PublishedDishSnapshot`. Сериализатор — `System.Text.Json` с настройками, согласованными с WebAPI (PascalCase, enum как строки через `JsonStringEnumConverter`); опции собраны в статическом `SnapshotJsonOptions.Default`.

**Структура верхнего уровня (упрощённо):**

```json
{
  "Name": "Борщ",
  "Slug": "borsch-1",
  "ShortDescription": null,
  "Description": "...",
  "HistoryText": null,
  "MainImageId": "...",
  "DifficultyLevel": "Medium",
  "CostEstimate": "Mid",
  "OwnerType": "User",
  "DietLabelsMask": "Vegetarian, GlutenFree",
  "AllergensMask": "Dairy",
  "HasUnverifiedAllergens": false,
  "Recipe": {
    "IntroductionText": "...",
    "ServingsDefault": 4,
    "IsAlcoholic": false,
    "AuthorTips": "...",
    "ServingSuggestions": null,
    "Notes": null,
    "Timing": { "PrepTimeMinutes": 15, "CookTimeMinutes": 60, "RestTimeMinutes": null, "ActiveTimeMinutes": null, "TotalTimeMinutes": 75, "IsTotalManual": true },
    "Yield": { "QuantityTotal": 4, "YieldUnit": "Servings", "ServingsCount": 4, "GramsPerServing": 250 },
    "Nutrition": { "CalcMethod": "PerServing", "Calories": 350, "Proteins": 12, "Fats": 8, "SaturatedFats": null, "Carbs": 45, "Sugar": null, "Fiber": null, "Salt": null },
    "Steps": [
      { "Id": "...", "Order": 1, "Title": "Бульон", "Description": "...", "ImageMediaId": null, "VideoUrl": null, "TemperatureCelsius": 100, "TimerMinutes": 60 }
    ],
    "Ingredients": [
      { "type": "catalog", "Id": "...", "Order": 1, "Quantity": 0.5, "MeasureUnitId": "...", "IsOptional": false, "PreparationNote": null, "IngredientId": "...", "IngredientSpecId": null },
      { "type": "freeform", "Id": "...", "Order": 2, "Quantity": 1, "MeasureUnitId": "...", "IsOptional": true, "PreparationNote": "комнатной температуры", "FreeformText": "укроп от соседки" }
    ]
  },
  "Categories": [ { "Id": "..." } ],
  "Tags": [ { "Id": "..." } ]
}
```

**Что MVP-формат содержит:**

- Все плоские поля карточки и рецепта.
- Полную 1:1 информацию по `Timing`, `Yield`, `Nutrition` (на момент публикации).
- Все шаги рецепта в порядке `Order`.
- Все ингредиенты в порядке `Order`, **полиморфно по природе** через дискриминатор `"type"` (см. [ADR-0012](../../../adr/ADR-0012-recipe-ingredient-discriminated-union.md)).
- Идентификаторы привязанных категорий и тегов.

**Что MVP-формат не содержит и почему:**

- **Имена справочников** (`Category.Name`, `Tag.Name`, `Ingredient.Name`, `IngredientSpec.Name`, `MeasureUnit.Code/Name`). Резолвятся потребителем при чтении (например, snapshot-веткой UC-DSH-050) через JOIN со справочниками. Решение принято для минимизации объёма Этапа 2 и сохранения свойства Builder как чистой функции без I/O.
- **`Dish.Id`** — известен по самой записи в БД.
- **Lifecycle-метаданные** (`Status`, `CreatedAt`, `UpdatedAt`, `PublishedAt`, `PublishedVersionUpdatedAt`) — хранятся в полях агрегата `Dish`, дублирование в снепшоте бессмысленно.
- **Runtime-счётчики** (`RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount`) — «живые» значения, обновляемые событиями; иммутабельному слепку противоречат.
- **`ModerationStatus`** — поле жизненного цикла, не публичный контент.

**Будущая миграция на денормализованный формат.** При появлении первой реальной потребности — скорее всего UC-DSH-052 (`GetDishRecipe`) или UC-DSH-054 (`SearchDishes` по текстам) — формат snapshot будет обогащён именами справочников. Миграция аддитивна: добавление nullable-полей `Name`/`Slug` в `Published*SnapshotDto` без breaking change существующего формата. Полный rebuild старых снепшотов — задача Этапа 8+ через админский метод `Dish.RebuildPublishedSnapshot(...)` (запланирован в [ADR-0013](../../../adr/ADR-0013-publish-spam-protection.md) §6).

---

## Связанные документы

- `docs/public/modules/dishes/domain-model.md` — определение агрегата `Dish` и метода `Dish.Publish(...)`.
- `docs/public/policies/POL-001-dish-ownership.md` — правила авторизации модификации блюд.
- `docs/public/adr/ADR-0012-recipe-ingredient-discriminated-union.md` — полиморфная сериализация ингредиентов.
- `docs/public/adr/ADR-0013-publish-spam-protection.md` — защита от спама `DishPublishedEvent`.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC модуля.
- `docs/public/modules/dishes/use-cases/UC-DSH-002-UpdateDishCard.md` — обновление карточки (симметричный паттерн POL-001 + VALID_ACTOR).
- `docs/public/modules/dishes/use-cases/UC-DSH-005-Unpublish.md` (будущий) — обратное снятие с публикации.
- `docs/public/modules/dishes/use-cases/UC-DSH-050-GetDishById.md` — после реализации этого UC у GetDishById активируется snapshot-ветка.
