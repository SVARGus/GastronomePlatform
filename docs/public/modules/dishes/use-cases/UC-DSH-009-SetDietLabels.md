# UC-DSH-009: Установить диетические метки блюда

**Version:** 1.0 | **Date:** 2026-06-07

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `Dish` — поле `Dish.DietLabelsMask`.
- Identifier: `Dish.Id` (`Guid`) — path-параметр.
- Action: Update (single field).

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required. Эндпоинт защищён `[Authorize(Policy = VALID_ACTOR)]` — требуется валидный JWT с claim `sub`.

### Authorization (Авторизация)

POL-001 Dish Ownership: автор блюда (`Dish.AuthorUserId == _currentUser.UserId`) или роль `Admin`.

### State Constraints (Ограничения по состоянию)

- `Dish.Status = Archived` — модификация запрещена для не-Admin (на Этапе 2 Admin-команды над архивом ещё не реализованы). Архивированное блюдо вернёт `404` после прохождения POL-001 проверки.

### Contextual Constraints (Контекстуальные ограничения)

Согласованность маски с составом рецепта обеспечивает ADR-0016: для каждого catalog-ингредиента из `Recipe.Ingredients` собирается `Ingredient.DietConflictsMask`; запрашиваемая `DietLabelsMask` не должна пересекаться с объединённой маской конфликтов.

---

## API Contract (Контракт API)

### Endpoint

```
PATCH /api/dishes/{id}/diet-labels
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Headers:**

- `Authorization: Bearer <JWT>` — обязательно.

**Body:**

```json
{
  "dietLabelsMask": "Vegetarian, GlutenFree"
}
```

Поле `dietLabelsMask` — битовая маска `DietLabels`, сериализуется строкой (через глобальный `JsonStringEnumConverter`). `None` допустимо — снять все метки.

### Response (Ответ)

- Status: `204 No Content`.
- Body: отсутствует.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | `DishId = Guid.Empty` или невалидное значение `DietLabelsMask`. |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 409 | `DISHES.DIET_LABELS_CONFLICT_WITH_COMPOSITION` | Запрошенная маска содержит биты, конфликтующие с `Ingredient.DietConflictsMask` хотя бы одного catalog-ингредиента рецепта. |

---

## Preconditions (Предусловия)

- HTTP-маршрут `PATCH /api/dishes/{id:guid}/diet-labels` корректно резолвится.
- Аутентификационный middleware валидирует JWT; политика `VALID_ACTOR` гарантирует валидный `sub`.

---

## Invariants (Инварианты домена)

- **ADR-0016, BR-DSH-DIET-001:** для каждого catalog-ингредиента `ri` в `Recipe.Ingredients` справедливо `Dish.DietLabelsMask AND ri.Ingredient.DietConflictsMask == 0`.
- Freeform-ингредиенты в инварианте не участвуют (ответственность автора).
- `Dish.AuthorUserId` иммутабельно — POL-001 проверка стабильна между вызовами.

---

## Main Flow (Основной поток)

1. **Запрос.** Клиент отправляет `PATCH /api/dishes/{id}/diet-labels` с JWT и телом запроса.
2. **Аутентификация и политика.** ASP.NET Core middleware валидирует JWT; политика `VALID_ACTOR` гарантирует валидный claim `sub`.
3. **Контроллер.** `DishesController.SetDietLabelsAsync(Guid id, SetDietLabelsRequest, CancellationToken)`:
   1. Собирает `SetDietLabelsCommand(DishId = id, DietLabelsMask = request.DietLabelsMask)`.
   2. Делегирует через `ISender.Send(command, ct)`.
4. **Валидация.** `ValidationBehavior` запускает `SetDietLabelsCommandValidator` — проверка `DishId.NotEmpty()`.
5. **Handler — `SetDietLabelsCommandHandler.Handle(...)`:**
   1. **Загрузка** через `_dishRepository.GetByIdWithFullRecipeAsync(DishId, ct)` — нужен полный рецепт для сбора Id ингредиентов.
   2. `null` → `return DishesErrors.DishNotFound` → `404`.
   3. **POL-001.** Если `dish.AuthorUserId != currentUserId && !isAdmin` → `return DishesErrors.NotDishOwner` → `403`.
   4. **Сбор конфликтов.** Извлекаются уникальные `IngredientId` всех catalog-позиций (`ri.IngredientId.HasValue`). Если их 0 — словарь пустой (любая маска валидна).
      - Иначе `markers = await _ingredientRepository.GetMarkersByIdsAsync(catalogIngredientIds, ct)`.
      - Из словаря маркеров извлекается отображение `IngredientId → DietConflicts`.
   5. **Вызов Domain:** `result = dish.SetDietLabels(request.DietLabelsMask, conflictsByIngredient, utcNow)`.
      - Domain собирает `combinedConflictsMask` по составу рецепта.
      - Если `(desiredMask AND combinedConflictsMask) != None` → `DishesErrors.DietLabelsConflictWithComposition` → `409`.
      - Иначе присваивает маску, обновляет `UpdatedAt`, поднимает `DishUpdatedEvent`.
   6. **Сохранение:** `SaveChangesAsync`.
   7. **Публикация доменных событий** через `IPublisher`.
6. **Маппинг ответа.** `ApiController.MapResult(Result.Success())` → `204 No Content`.

---

## Alternative Flows (Альтернативные потоки)

- **AF-1 «Admin меняет чужое блюдо».** `dish.AuthorUserId != currentUserId`, но `IsInRole(Admin) == true` → POL-001 пропускает, дальше Main Flow без изменений.
- **AF-2 «Снять все метки».** `dietLabelsMask = None` → `(None AND anything) == None` — конфликта нет, маска снимается всегда.
- **AF-3 «Рецепт пустой».** `Recipe.Ingredients.Count == 0` → словарь конфликтов пустой → любая маска успешно ставится. Сценарий типичен для черновика, в который ещё не добавлены ингредиенты.

---

## Edge Cases (Граничные случаи)

- **EC-1. Чужой пользователь не Admin.** `403 DISHES.NOT_DISH_OWNER`. Намеренно — это POL-001 для модификаций (отличается от UC-DSH-050, где для чтения используется `404`).
- **EC-2. Невалидный `Guid` в маршруте.** ASP.NET Core роутинг `{id:guid}` отвергает с `404`.
- **EC-3. Часть catalog-ингредиентов уже удалена в БД.** `GetMarkersByIdsAsync` возвращает словарь без отсутствующих Id; в Domain отсутствующие Id интерпретируются как `DietLabels.None` (консервативно — не блокируем). Корректно: ингредиент удалён → его конфликты больше не учитываются.
- **EC-4. Только freeform-ингредиенты в рецепте.** Словарь конфликтов пустой → любая маска валидна. Это согласуется с принципом «freeform = ответственность автора».
- **EC-5. Маска содержит несколько меток, конфликтующих с разными ингредиентами.** Конфликт по объединённой маске — одна ошибка `409`, без раскладки по меткам. Сообщение об ошибке универсальное; UI может детализировать через локальное сопоставление маски ингредиентов и желаемой маски.
- **EC-6. Параллельные запросы.** Между двумя одновременными `SetDietLabels` второй перезапишет результат первого. EF Core без оптимистических токенов — конфликт не обнаруживается. Приемлемо для Этапа 2 (нет UI multi-user редактирования).

---

## Postconditions (Постусловия)

- При успехе: `Dish.DietLabelsMask = request.DietLabelsMask`, `Dish.UpdatedAt = utcNow`, поднят `DishUpdatedEvent`.
- `Dish.PublishedVersionData` **не меняется** — публичная версия обновляется только через UC-DSH-004 PublishDish.
- При неуспехе: состояние БД не меняется (`SaveChangesAsync` не вызывается).

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Идемпотентен по содержимому: повторный вызов с тем же `dietLabelsMask` приведёт к тем же значениям полей (но обновит `UpdatedAt` и поднимет `DishUpdatedEvent` повторно — это особенность модели «явная команда → явное событие»).
- **Performance.** Целевое < 80 мс. Один SELECT блюда с рецептом + один SELECT по `Ingredients` (только нужные колонки) + один UPDATE.
- **Consistency.** Read committed. Без блокировок.
- **Audit.** Стандартное HTTP-логирование Serilog.

---

## Реализация Этапа 2 — что в наличии

### Реализовано

- Полная реализация по ADR-0016 (Reject-семантика, словарь маркеров через `IIngredientRepository.GetMarkersByIdsAsync`).
- Эндпоинт `PATCH /api/dishes/{id}/diet-labels`.
- POL-001 (автор / Admin).

### Отложено

- **Заполнение `Ingredient.DietConflictsMask` для существующих сидовых ингредиентов** — постепенная работа модераторов через UC-DSH-111.
- **Автоматический пересчёт `DietLabelsMask` всех блюд при изменении `Ingredient.DietConflictsMask`** через UC-DSH-111 — общий механизм фоновой инвалидации снепшотов (Этап 4+/8+).
- **Раскладка `409 DIET_LABELS_CONFLICT_WITH_COMPOSITION` по конкретным конфликтующим меткам/ингредиентам** — на Этапе 2 ошибка общая; UI делает локальное сопоставление.

---

## Связанные документы

- `docs/public/adr/ADR-0016-diet-conflicts-mask.md` — источник правды по дизайну.
- `docs/public/modules/dishes/domain-model.md` — раздел про `Dish.SetDietLabels` и `Dish.RecalculateDishMarkers`.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC модуля.
- `docs/public/policies/POL-001-dish-ownership.md` — политика POL-001.
- UC-DSH-030/031/032 — состав-команды, после реализации будут вызывать `Dish.RecalculateDishMarkers` (auto-clear конфликтных меток).
- UC-DSH-110/111 — admin-команды, через которые заполняется `Ingredient.DietConflictsMask`.
