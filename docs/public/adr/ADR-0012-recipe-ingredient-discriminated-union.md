# ADR-0012: `RecipeIngredient` — discriminated union «catalog vs freeform» в модуле Dishes

**Status:** Accepted
**Date:** 2026-05-30
**Stage:** 2

---

## Related (Связи)

- **Связанные ADR:** ADR-0014 — общий принцип Discriminated Unions в CQRS, который применяется в этом ADR.
- **Связанные модули:** Dishes.
- **Связанная документация:**
  - `docs/public/modules/dishes/domain-model.md` — §7.2 «Recipe → Управление ингредиентами рецепта», §7.4 «RecipeIngredient».
  - `docs/public/modules/dishes/use-cases/README.md` — UC-DSH-030..033 (управление составом рецепта), UC-DSH-052 (получение рецепта), UC-DSH-004 (публикация блюда).
- **Связь с кодом:** см. §7 Implementation Reference.

---

## 1. Context (Контекст)

`RecipeIngredient` — позиция в списке ингредиентов рецепта блюда. Сущность является **гибридом двух природ**:

- **Catalog-позиция** — ссылка на справочник `Ingredient` (через `IngredientId`), с опциональным уточнением сорта/спецификации `IngredientSpec` (через `IngredientSpecId`).
- **Freeform-позиция** — свободный текст ингредиента (через `FreeformText`), для случаев когда в справочнике нет нужной позиции.

**XOR-инвариант:** заполнено ровно одно из двух — `IngredientId` либо `FreeformText`. Оба или ни одного — недопустимо. Дополнительно: `IngredientSpecId` допустим только при заполненном `IngredientId`.

На момент принятия ADR в коде Этапа 2:

- **Write-side** уже реализован (приватный конструктор, internal-фабрики `CreateFromCatalog` / `CreateFreeform`, runtime-проверка XOR в методе `Update`, CHECK-constraint в БД, wrapper-методы на корне агрегата `Dish`).
- **Read-side** (DTO) и **Application-команды** (UC-DSH-030..033, UC-DSH-052) ещё не реализованы — фигурируют в `use-cases/README.md` как Core на Этапе 2.
- **Сериализация в jsonb-снепшот** `Dish.PublishedVersionData` — будет реализована в UC-DSH-004 PublishDish.

Этот ADR — **частное применение** принципа из ADR-0014 «Discriminated Unions в CQRS» к конкретной сущности `RecipeIngredient`. Он фиксирует:

1. Текущую write-side реализацию как соответствующую выбранному принципу.
2. Обязательство при будущей реализации read-side и команд следовать тому же принципу.
3. Обязательство при реализации UC-DSH-004 PublishDish использовать полиморфную сериализацию для массива `ingredients[]` в jsonb-снепшоте.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

Полный сравнительный разбор пяти общих вариантов работы с DU-сущностями — в ADR-0014 §2. Здесь — краткое перечисление с привязкой к специфике `RecipeIngredient`.

### Вариант A — Один класс с nullable-полями + runtime XOR

Один публичный конструктор/фабрика `RecipeIngredient.Create(Guid? ingredientId, Guid? ingredientSpecId, string? freeformText, ...)`. XOR — только в runtime. Read-DTO — один с теми же nullable-полями.

- **Минусы:** компилятор не помогает; потребители видят 3 nullable-поля catalog/spec/freeform и должны мысленно держать XOR в голове.
- **Отклонён**.

### Вариант B — Множество фабрик на одном классе, один общий read-DTO

Write-side как в выбранном Варианте E. Read-side остаётся единым DTO с nullable-полями.

- **Минусы:** защищает только запись; чтение остаётся уязвимым к ошибкам клиента.
- **Отклонён**.

### Вариант C — Иерархия классов (TPH в EF Core)

Базовый `RecipeIngredient`, наследники `RecipeIngredientCatalog` / `RecipeIngredientFreeform`. EF Core маппит как TPH.

- **Минусы:** TPH-маппинг разрезает запросы по типу, усложняет миграции, ломает принцип «один тип — одна таблица».
- **Отклонён**.

### Вариант D — Value Object discriminator `IngredientSource`

`RecipeIngredient` содержит value object `IngredientSource.FromCatalog(...)` / `IngredientSource.Freeform(...)`.

- **Минусы:** дополнительный тип ради косвенности, которая в Варианте E уже достигнута парой фабрик; EF-маппинг owned value object усложняется.
- **Отклонён**.

### Вариант E — Write: фабрики по природе, Read: отдельные DTO + полиморфизм ⭐

**Write-side:**
- Один Domain-класс `RecipeIngredient`, приватный конструктор.
- Две `internal static` фабрики: `CreateFromCatalog(...)`, `CreateFreeform(...)`.
- Wrapper-методы на корне агрегата `Dish`: `AddRecipeIngredientFromCatalog`, `AddRecipeIngredientFreeform` (разделены по природе), `UpdateRecipeIngredient`, `RemoveRecipeIngredient`, `ReorderRecipeIngredients` (общие для обеих природ).
- Update-метод в Domain — единый, с runtime-проверкой XOR через `DishesErrors.InvalidIngredientComposition`. Допускает смену природы catalog ↔ freeform (например, freeform-позиция переключается на catalog, когда нужный ингредиент появился в справочнике).
- CHECK-constraint в БД `CK_RecipeIngredients_IngredientXorFreeform` — последняя линия защиты.

**Read-side:**
- Абстрактный record `RecipeIngredientViewDto` как объединяющий тип.
- Два наследника: `CatalogRecipeIngredientViewDto` и `FreeformRecipeIngredientViewDto`, каждый со своим набором non-nullable полей.
- Полиморфная JSON-сериализация через атрибуты `[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]` + `[JsonDerivedType(typeof(CatalogRecipeIngredientViewDto), "catalog")]` + `[JsonDerivedType(typeof(FreeformRecipeIngredientViewDto), "freeform")]` на базовом типе.
- Поле-дискриминатор в JSON — `"type": "catalog" | "freeform"`.

**Application-команды:**
- **Add — две команды:** `AddCatalogIngredientToRecipeCommand` (поля: `DishId`, `IngredientId`, `IngredientSpecId?`, `Quantity`, `MeasureUnitId`, `IsOptional`, `PreparationNote?`) и `AddFreeformIngredientToRecipeCommand` (поля: `DishId`, `FreeformText`, `Quantity`, `MeasureUnitId`, `IsOptional`, `PreparationNote?`). Структурная гарантия XOR на уровне типа команды — невозможно поднять команду с пустым `IngredientId` или с обоими заполненными.
- **Update — одна команда:** `UpdateRecipeIngredientCommand` с runtime-XOR в Domain. Поддерживает смену природы.
- **Remove, Reorder — общие команды**, без вариаций.

**Сериализация в `Dish.PublishedVersionData`:** массив `ingredients[]` в jsonb-снепшоте использует тот же полиморфный сериализатор. Каждый элемент имеет дискриминатор `"type"`.

- **Плюсы:** типобезопасность на write (compile-time) и read (без nullable-капканов); естественная эволюция в специфичные поля; совместимость с EF Core (одна таблица `dishes.RecipeIngredients`); полиморфная сериализация настраивается один раз через атрибуты.
- **Минусы:** объяснимая асимметрия «две Add-команды vs один Update»; при добавлении новой природы в будущем — нужно обновить все слои.
- **Выбран ⭐.**

---

## 3. Decision (Принятое решение)

Применить к сущности `RecipeIngredient` принцип ADR-0014. Конкретно:

1. **Domain:**
   - Класс `RecipeIngredient` — один, приватный конструктор, без публичных конструкторов.
   - Две `internal static` фабрики: `CreateFromCatalog(...)`, `CreateFreeform(...)`.
   - Единый метод `Update(...)` с runtime-XOR через `DishesErrors.InvalidIngredientComposition`.
2. **Корень агрегата `Dish`:**
   - Add-обёртки разделены по природе: `AddRecipeIngredientFromCatalog`, `AddRecipeIngredientFreeform`.
   - Update-, Remove-, Reorder-обёртки — общие (`UpdateRecipeIngredient`, `RemoveRecipeIngredient`, `ReorderRecipeIngredients`).
3. **Application-команды:**
   - Add — две отдельные команды на каждую природу.
   - Update — одна общая команда.
   - Remove, Reorder — по одной общей команде.
4. **Application-DTO (Read-side):**
   - Абстрактный record `RecipeIngredientViewDto` как объединяющий тип.
   - Два sealed наследника: `CatalogRecipeIngredientViewDto`, `FreeformRecipeIngredientViewDto`.
   - На базовом типе — атрибуты `[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]` + два `[JsonDerivedType]` с литералами `"catalog"` и `"freeform"`.
5. **WebAPI-эндпоинты:**
   - Два отдельных POST-эндпоинта для Add: `POST /api/dishes/{id}/recipe/ingredients/catalog` и `POST /api/dishes/{id}/recipe/ingredients/freeform`.
   - Один эндпоинт для Update: `PUT /api/dishes/{id}/recipe/ingredients/{ingredientId}`.
   - Один эндпоинт для Remove, один для Reorder.
6. **Сериализация в `Dish.PublishedVersionData` (jsonb):** массив `ingredients[]` использует тот же `System.Text.Json` полиморфный сериализатор; дискриминатор `"type"` присутствует у каждого элемента.
7. **БД:** CHECK-constraint `CK_RecipeIngredients_IngredientXorFreeform` остаётся как defense-in-depth, независимо от Domain- и Application-проверок.
8. **Код ошибки XOR:** единый `DishesErrors.InvalidIngredientComposition` (тип `Conflict` → HTTP 409). Дробление на «оба null», «оба заполнены», «`IngredientSpecId` без `IngredientId`» — не требуется на уровне HTTP-ошибки. Более детальные сообщения о природе нарушения — задача будущих бизнес-логов (см. `docs/_private/private_TODO-будущие-этапы.md` §4.7 — приватный TODO разработчика, не часть публичного контракта).

---

## 4. Rationale (Обоснование)

1. **Write-side: compile-time гарантия XOR.** Через две фабрики и две Add-команды невозможно создать невалидный объект и невозможно поднять невалидную команду. Самый ранний возможный момент обнаружения ошибки.
2. **Read-side: типобезопасные DTO.** Потребитель работает с одним из конкретных DTO — все поля non-nullable (где это допустимо доменом). Клиент (UI / external) получает естественный discriminated union с автоматическим сужением типа.
3. **Допустимость смены природы при Update.** Сценарий «автор создал freeform-позицию, потом нужный ингредиент появился в справочнике и автор переключает на catalog» — реальный. Дробление Update на три команды (`UpdateCatalogRecipeIngredient`, `UpdateFreeformRecipeIngredient`, `ConvertRecipeIngredientNature`) усложнит UI без выгоды.
4. **Совместимость с уже принятой моделью DTO.** Read-side разделение `Dish` ≠ `DishCardDto` ≠ `DishDetailDto` уже норма в проекте. Разделение на `CatalogRecipeIngredientViewDto` / `FreeformRecipeIngredientViewDto` — естественное продолжение того же подхода для вариативной сущности.
5. **Полиморфная сериализация — единая настройка для всех потребителей.** API-ответы, jsonb-снепшоты и (при появлении в будущем) интеграционные события используют один и тот же сериализатор без custom-конвертеров.
6. **Defense in depth по XOR на трёх уровнях.** Compile-time (две фабрики) → runtime (`RecipeIngredient.Update`) → БД (CHECK-constraint). Каждый уровень закрывает свой класс ошибок.

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- Write-side `RecipeIngredient` уже соответствует ADR без правок кода.
- При реализации UC-DSH-030..033 структура Application-слоя предопределена — меньше архитектурных решений в момент кодирования UC.
- Клиенты модуля Dishes (UI веба, мобильное приложение) получают типобезопасный DTO для рендеринга ингредиентов рецепта.
- `Dish.PublishedVersionData` после реализации UC-DSH-004 будет содержать самодостаточные ingredient-записи с явной природой — упрощает парсинг при чтении снепшота (snapshot-ветка UC-DSH-050, отложенная до появления Publish).

### Negative / Trade-offs (Отрицательные / компромиссы)

- Две Add-команды и два Add-эндпоинта вместо одного — небольшое разрастание API. Это разовая надбавка к коду, не зависит от частоты использования.
- Текущая формулировка UC-DSH-030 «Добавить ингредиент в рецепт» как **одного** UC в `use-cases/README.md` требует пересмотра — либо разделение на два UC, либо явное указание «один UC, реализуемый двумя эндпоинтами под капотом». См. §6 Future Scope.
- Регистрация атрибутов `[JsonDerivedType]` для всех природ обязательна — при добавлении новой природы можно забыть атрибут.

### Areas of Caution (На что обратить внимание)

- При реализации сборщика jsonb-снепшота в UC-DSH-004 PublishDish: убедиться, что сериализатор использует тот же `JsonSerializerOptions`, что и WebAPI-ответы. Иначе возможна рассинхронизация формата API-ответа и формата снепшота.
- Покрыть unit-тестом round-trip-сериализации каждой природы (`Catalog` → JSON → `Catalog`, `Freeform` → JSON → `Freeform`). Хотя бы один тест на каждую природу. Защищает от тихого ломания формата снепшота при будущих правках.
- При код-ревью UC-DSH-030..033: проверять, что Add разделён на две команды/два эндпоинта; что Update — одна команда; что DTO разделены и наследуются от общей базы; что `[JsonDerivedType]` зарегистрированы.

---

## 6. Future Scope (Будущие направления)

- **Согласование UC-DSH-030 с принятым принципом.** UC-DSH-030 остаётся **единым** UC «Добавить ингредиент в рецепт» — функционально это одна операция автора («добавить позицию в список ингредиентов»), но реализуется двумя эндпоинтами по природе из-за структурного различия входных данных catalog vs freeform. Уточнение зафиксировано в `use-cases/README.md` (описание UC-DSH-030).
- **Появление третьей природы ингредиента — Этап 8+.** Возможный кандидат — пользовательский справочник ингредиентов (`UserCustomIngredient`). Расширение паттерна: добавить третью фабрику `CreateFromUserCatalog(...)`, третью Add-команду, третий read-DTO с дискриминатором `"user-catalog"`, обновить CHECK-constraint и атрибуты `[JsonDerivedType]`. Структурно ничего не меняется.
- **Бизнес-логи отказов по XOR.** При реализации структурированных бизнес-логов (см. `docs/_private/private_TODO-будущие-этапы.md` §4.7) — отказы `InvalidIngredientComposition` стоит логировать с разбивкой по конкретной причине нарушения (оба null, оба заполнены, spec без catalog) на уровне `Warning`. Это не часть HTTP-контракта — клиент по-прежнему получает единый код ошибки.

---

## 7. Implementation Reference (Связь с кодовой базой)

### Реализовано (Write-side)

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/RecipeIngredient.cs` — приватный конструктор, фабрики `CreateFromCatalog`, `CreateFreeform`, метод `Update` с runtime-XOR.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Dish.cs` — wrapper-методы `AddRecipeIngredientFromCatalog`, `AddRecipeIngredientFreeform`, `UpdateRecipeIngredient`, `RemoveRecipeIngredient`, `ReorderRecipeIngredients`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Persistence/Configurations/RecipeIngredientConfiguration.cs` — CHECK-constraint `CK_RecipeIngredients_IngredientXorFreeform` (+ связанные `CK_RecipeIngredients_SpecRequiresIngredient`, `CK_RecipeIngredients_QuantityPositive`, `CK_RecipeIngredients_OrderPositive`).
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Errors/DishesErrors.cs` — код ошибки `InvalidIngredientComposition` (`Error.Conflict`).

### Планируется

> Конкретные пути могут смениться в ходе реализации соответствующих UC. Здесь — обязательство соблюсти структуру принципа в указанных слоях.

- **UC-DSH-030 «Добавить ингредиент в рецепт»** — две команды Application-слоя:
  - `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Application/Commands/AddCatalogIngredientToRecipe/AddCatalogIngredientToRecipeCommand.cs` (+ `Handler`, `Validator`).
  - `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Application/Commands/AddFreeformIngredientToRecipe/AddFreeformIngredientToRecipeCommand.cs` (+ `Handler`, `Validator`).
- **UC-DSH-031 «Обновить ингредиент в рецепте»** — одна команда:
  - `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Application/Commands/UpdateRecipeIngredient/UpdateRecipeIngredientCommand.cs` (+ `Handler`, `Validator`).
- **UC-DSH-052 «Получить рецепт блюда»** — Read-DTO:
  - `RecipeIngredientViewDto` (абстрактный), `CatalogRecipeIngredientViewDto`, `FreeformRecipeIngredientViewDto` в соответствующей папке `Queries/GetDishRecipe/`.
  - Атрибуты `[JsonPolymorphic]` + `[JsonDerivedType]` — на базовом типе.
- **UC-DSH-004 «Опубликовать блюдо»** — сборщик jsonb-снепшота для `Dish.PublishedVersionData`:
  - Использует те же DTO-типы из UC-DSH-052 для массива `ingredients[]`.
  - `JsonSerializerOptions` — общие с WebAPI, во избежание рассинхронизации формата.
- **WebAPI-эндпоинты** в `src/GastronomePlatform.WebAPI/Controllers/Dishes/DishesController.cs`:
  - `POST /api/dishes/{id}/recipe/ingredients/catalog`.
  - `POST /api/dishes/{id}/recipe/ingredients/freeform`.
  - `PUT /api/dishes/{id}/recipe/ingredients/{ingredientId}`.
  - `DELETE /api/dishes/{id}/recipe/ingredients/{ingredientId}`.
  - `POST /api/dishes/{id}/recipe/ingredients/reorder`.

---

## История изменений

- **2026-05-30:** Accepted.
