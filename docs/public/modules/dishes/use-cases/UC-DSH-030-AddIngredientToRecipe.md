# UC-DSH-030: Добавить ингредиент в рецепт

**Version:** 1.0 | **Date:** 2026-06-07

---

## Actors (Инициаторы)

- Primary: автор блюда либо пользователь с ролью `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `RecipeIngredient` (часть агрегата `Dish` → `Recipe`).
- Identifier: `Dish.Id` (`Guid`) — path-параметр; `RecipeIngredient.Id` (`Guid`) — генерируется при создании, возвращается в `Location` и теле ответа.
- Action: Create.

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required. Эндпоинт защищён `[Authorize(Policy = VALID_ACTOR)]`.

### Authorization (Авторизация)

POL-001 Dish Ownership: автор блюда (`Dish.AuthorUserId == _currentUser.UserId`) или роль `Admin`.

### State Constraints (Ограничения по состоянию)

- На Этапе 2 — без отдельной проверки `Dish.Status`. UC-DSH-006 Archive ещё не реализован; для согласованности с UC-DSH-002/003/009 явная проверка `Archived` не добавляется.

---

## API Contract (Контракт API)

UC разделён на две команды по природе позиции (ADR-0012, ADR-0014 — discriminated union на write-side). Функционально это **один** UC: одна операция автора «добавить позицию в рецепт».

### Endpoint — catalog-ветка

```
POST /api/dishes/{id}/recipe/ingredients/catalog
```

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Body:**

```json
{
  "ingredientId": "00000000-0000-0000-0000-000000000000",
  "ingredientSpecId": null,
  "quantity": 100.0,
  "measureUnitId": "00000000-0000-0000-0000-000000000000",
  "isOptional": false,
  "preparationNote": "мелко нарезанный"
}
```

| Поле | Тип | Обязательно | Ограничения |
|---|---|---|---|
| `ingredientId` | `Guid` | Да | ≠ `Guid.Empty`; должен существовать в справочнике; `IsActive == true`. |
| `ingredientSpecId` | `Guid?` | Нет | Если задан — должен существовать и принадлежать `ingredientId`. |
| `quantity` | `decimal` | Да | `> 0`. |
| `measureUnitId` | `Guid` | Да | Должен существовать в справочнике. |
| `isOptional` | `bool` | Да | — |
| `preparationNote` | `string?` | Нет | До 200 символов. |

### Endpoint — freeform-ветка

```
POST /api/dishes/{id}/recipe/ingredients/freeform
```

**Body:**

```json
{
  "freeformText": "Соль крымская",
  "quantity": 1.0,
  "measureUnitId": "00000000-0000-0000-0000-000000000000",
  "isOptional": false,
  "preparationNote": null
}
```

| Поле | Тип | Обязательно | Ограничения |
|---|---|---|---|
| `freeformText` | `string` | Да | 1..200 символов, не пробельный. |
| `quantity` | `decimal` | Да | `> 0`. |
| `measureUnitId` | `Guid` | Да | Должен существовать в справочнике. |
| `isOptional` | `bool` | Да | — |
| `preparationNote` | `string?` | Нет | До 200 символов. |

### Response (общий для обеих веток)

- Status: `201 Created`.
- Headers: `Location: /api/dishes/{id}/recipe/ingredients/{newRecipeIngredientId}`.
- Body: `{ "id": "<newRecipeIngredientId>" }`.

### Errors

| HTTP | Код | Условие |
|---|---|---|
| 400 | `VALIDATION.ERROR` | Нарушены структурные ограничения (см. FluentValidation). |
| 401 | — | Отсутствует или невалиден JWT. |
| 403 | `DISHES.NOT_DISH_OWNER` | Пользователь не автор и не Admin. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует. |
| 404 | `DISHES.INGREDIENT_NOT_FOUND` | (catalog) Ингредиент отсутствует в справочнике. |
| 404 | `DISHES.INGREDIENT_SPEC_NOT_FOUND` | (catalog) Указанный сорт отсутствует. |
| 404 | `DISHES.MEASURE_UNIT_NOT_FOUND` | Единица измерения отсутствует. |
| 409 | `DISHES.INGREDIENT_INACTIVE` | (catalog) Ингредиент деактивирован. |
| 409 | `DISHES.INGREDIENT_SPEC_MISMATCH` | (catalog) Сорт принадлежит другому ингредиенту. |

---

## Preconditions (Предусловия)

- Блюдо существует и доступно текущему пользователю по POL-001.
- (catalog) Ингредиент, сорт и единица измерения существуют в справочниках; ингредиент активен.
- (freeform) Единица измерения существует.

---

## Invariants (Инварианты домена)

- XOR `(IngredientId IS NOT NULL) <> (FreeformText IS NOT NULL)` — обеспечивается структурно двумя фабриками `RecipeIngredient.CreateFromCatalog` / `CreateFreeform`, продублирован CHECK-constraint'ом в БД.
- `Quantity > 0` — Domain (`RecipeIngredient.Update` — только для апдейта; в Add-фабриках валидация делегирована команде через `Quantity > 0` в валидаторе).
- `Order = max(Order)+1` для новой позиции — поддерживается `Recipe.AddIngredientFromCatalog` / `AddIngredientFreeform`.
- ADR-0016, BR-DSH-DIET-001: для каждого catalog-ингредиента `ri` в `Recipe.Ingredients` справедливо `Dish.DietLabelsMask AND ri.Ingredient.DietConflictsMask == 0`. После добавления Handler вызывает `Dish.RecalculateDishMarkers` — диет-метки автокорректируются (silent auto-clear).

---

## Main Flow (Основной поток)

1. Клиент шлёт `POST .../catalog` (либо `.../freeform`) с телом запроса.
2. FluentValidation → 400 при структурных ошибках.
3. Handler загружает блюдо с полным рецептом (нужен текущий состав для последующего перерасчёта).
4. POL-001: автор или Admin.
5. Проверка справочников (Ingredient + Spec + MeasureUnit для catalog; только MeasureUnit для freeform).
6. Вызов `Dish.AddRecipeIngredientFromCatalog` / `AddRecipeIngredientFreeform` — позиция добавлена в `Recipe.Ingredients` с `Order = max+1`; `Dish.UpdatedAt = utcNow`; поднят `DishUpdatedEvent`.
7. Handler собирает словарь `IngredientId → IngredientMarkers` через `IIngredientRepository.GetMarkersByIdsAsync` для всех catalog-позиций текущего состава.
8. Вызов `Dish.RecalculateDishMarkers(markers, utcNow)`:
   - пересчитан `AllergensMask` (OR по catalog-позициям);
   - `HasUnverifiedAllergens` поднят, если в составе есть freeform-позиции;
   - из `DietLabelsMask` сняты конфликтующие биты (если есть) → поднят `DishDietLabelsAutoCorrectedEvent`;
   - `Dish.UpdatedAt = utcNow` (повторный `MarkAsUpdated` — поднимает ещё один `DishUpdatedEvent`).
9. `SaveChangesAsync` + публикация доменных событий.
10. Ответ `201 Created` с `Location` и телом `{ id }`.

---

## Alternative Flows (Альтернативные потоки)

Нет — две ветки (catalog / freeform) рассматриваются как два API-варианта одного UC, не как Alternative Flows.

---

## Edge Cases (Граничные случаи)

- **EC-1: Freeform-позиция повышает `HasUnverifiedAllergens`.** При добавлении freeform Handler не обращается к справочнику ингредиентов для новой позиции, но `Dish.RecalculateDishMarkers` всё равно вызывается — она перебирает `Recipe.Ingredients` и видит freeform-элемент, поднимая флаг.
- **EC-2: Добавлен ингредиент, конфликтующий с текущими диет-метками блюда.** Сценарий ADR-0016: автор имеет `DietLabelsMask = Vegan` и добавляет «Свинину». `Dish.RecalculateDishMarkers` снимает бит `Vegan`, поднимает `DishDietLabelsAutoCorrectedEvent` с `RemovedLabels = Vegan`. UI на следующем GET-запросе показывает новую маску.
- **EC-3: Двойной `DishUpdatedEvent`.** За одну операцию агрегат генерирует два `DishUpdatedEvent` (после `Add...`, после `RecalculateDishMarkers`). На Этапе 2 подписчиков нет — оптимизация преждевременная. См. техдолг.
- **EC-4: Конкурентные Add от автора и Admin.** Каждый запрос — своя транзакция; `Order` назначается атомарно по `max+1`. Возможна минимальная гонка `Order` (два одинаковых номера) при одновременной вставке — на Этапе 2 не защищаемся (нет уникального индекса на `(RecipeId, Order)`; есть только Domain-вычисление).

---

## Postconditions (Постусловия)

- В `dishes."RecipeIngredients"` появилась новая запись.
- `Dish.UpdatedAt = utcNow`.
- `Dish.AllergensMask`, `Dish.HasUnverifiedAllergens`, `Dish.DietLabelsMask` — приведены в согласованное состояние через `RecalculateDishMarkers`.
- `Dish.PublishedVersionData` не изменён — правка касается рабочей версии (двухслойная модель публикации).
- Поднята серия доменных событий: `DishUpdatedEvent` (×2 при минимуме), опционально `DishDietLabelsAutoCorrectedEvent`.

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Не идемпотентен — каждый успешный вызов создаёт новую позицию. Клиент при ретрае получит вторую запись с тем же `IngredientId`.
- **Performance.** Целевое < 100 мс. До 5 SELECT-запросов (Dish с полным составом, Ingredient, Spec, MeasureUnit, GetMarkersByIds) + 1 INSERT + 1 UPDATE на `Dish.UpdatedAt`.
- **Consistency.** Один `SaveChangesAsync` — добавление позиции и обновление маркеров в одной транзакции.
- **Audit.** Стандартное HTTP-логирование (Serilog). Доменные события (`DishUpdatedEvent`, `DishDietLabelsAutoCorrectedEvent`) — будущая основа аудита изменений.

---

## Связанные документы

- ADR-0012 — `RecipeIngredient` как discriminated union (write-side фабрики + read-side полиморфные DTO).
- ADR-0014 — общий принцип discriminated unions в CQRS.
- ADR-0016 — `Ingredient.DietConflictsMask` + автокоррекция `Dish.DietLabelsMask`.
- POL-001 — Dish Ownership Policy.
- `docs/public/modules/dishes/domain-model.md` — `Recipe`, `RecipeIngredient`, `Dish.RecalculateDishMarkers`.
- UC-DSH-031, 032, 033 — другие операции над позициями рецепта.
- UC-DSH-062 — поиск ингредиентов (использует автор перед catalog-добавлением).
