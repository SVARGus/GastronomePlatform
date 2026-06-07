# ADR-0016: Источник конфликтов диетических меток — поле `Ingredient.DietConflictsMask`

**Status:** Accepted
**Date:** 2026-06-07
**Stage:** 2

---

## Related (Связи)

- **Связанные ADR:** ADR-0012 (RecipeIngredient discriminated union) — определяет, что freeform-ингредиенты участвуют в составе, но не имеют справочной информации; ADR-0013 (Publish spam protection) — поднимает доменные события только при реальном изменении.
- **Связанные модули:** Dishes.
- **Связанная документация:**
  - `docs/public/modules/dishes/domain-model.md` — раздел про `SetDietLabels`, `RecalculateDishMarkers`.
  - `docs/public/modules/dishes/use-cases/UC-DSH-009-SetDietLabels.md` — полная реализация под этим ADR.
  - `docs/public/modules/dishes/use-cases/README.md` — UC-DSH-030..032 (управление составом) на этапе реализации будут использовать `RecalculateDishMarkers`.
- **Связь с кодом:** см. §7 Implementation Reference.

---

## 1. Context (Контекст)

Поле `Dish.DietLabelsMask` (битовая маска `DietLabels`: `Vegetarian | Vegan | GlutenFree | LactoseFree | Halal | Kosher | KetoFriendly | LowCarb | LowCalorie | SugarFree`) описывает диетические свойства блюда — используется в каталожном фильтре и в карточке. До этого ADR оно ставится автором свободно: метод `Dish.SetDietLabels` — заглушка, просто пишет значение, не проверяет совместимость с составом ингредиентов.

Это допускает противоречивые блюда: автор может пометить рецепт со свининой как `Vegan | Halal`. На Этапе 2 проблема не блокирующая (catalog мал, модерация ручная), но при росте контента и появлении публичных фильтров нужна согласованность.

Симметричная задача для аллергенов частично решена: `Ingredient.AllergenType` хранит маску аллергенов ингредиента, существующий Domain-метод `Dish.RecalculateAllergens(IReadOnlyDictionary<Guid, AllergenType>, utcNow)` пересчитывает `Dish.AllergensMask` по составу рецепта. На момент этого ADR метод ещё не имеет ни одного Application-вызывающего (UC-DSH-030..032 управления составом рецепта ещё не реализованы), поэтому его рефакторинг бесплатен с точки зрения обратной совместимости.

Этот ADR фиксирует, **где живёт информация о конфликтах ингредиента с диетическими метками** и какова семантика автокоррекции `Dish.DietLabelsMask` при изменении состава или при попытке установки меток автором.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Новое поле `Ingredient.DietConflictsMask: DietLabels` ⭐

Маска заполняется модератором/админом через UC-DSH-110 (создание ингредиента) и UC-DSH-111 (обновление). Дефолт `DietLabels.None` (`0`) — «не конфликтует ни с чем» — безопасный no-op для существующих сидовых записей.

- **Плюсы:** одно поле, одна миграция, явная ответственность; маска полностью покрывает кейсы, не выводимые из `AllergenType` (мясо vs Vegan/Halal, алкоголь vs Halal, сахар vs SugarFree); симметрично существующему `AllergenType` — паттерн уже знаком в коде.
- **Минусы:** ручная работа модератора по заполнению. Дублирует часть информации, выводимой из `AllergenType` (например, `Gluten` ⇔ `GlutenFree`, `Dairy` ⇔ `LactoseFree`).
- **Выбран ⭐.**

### Вариант B — Derived из `AllergenType` + новая сущность `IngredientCategory`

Создаётся сущность `IngredientCategory` (Meat, Dairy, Alcohol, Sweetener, ...), у `Ingredient` появляется `CategoryId`, `DietConflictsMask` вычисляется как функция (`Category` × `AllergenType`).

- **Плюсы:** меньше ручного труда у модератора (категория задаётся один раз, конфликты выводятся), категории всё равно понадобятся позже для UI «Энциклопедия ингредиентов» (Этап 4+).
- **Минусы:** требует **полностью новой сущности** `IngredientCategory` + связки с `Ingredient` + admin-команд + миграции. Маппинг «категория → конфликты» — сам по себе нетривиальное решение, кандидат на ещё один ADR. Объём работы — в разы больше Варианта A. Дополнительно: derived-логика требует, чтобы изменение маппинга «категория → конфликты» инвалидировало все блюда (а это и так нужно делать при изменении `DietConflictsMask` ингредиента — см. §6).
- **Отклонён.** Сейчас не оправдано. К `IngredientCategory` вернёмся отдельно, когда понадобится для UI; на тот момент она дополнит, а не заменит `DietConflictsMask`.

### Вариант C — Гибрид: поле `DietConflictsMask` + auto-предзаполнение из `AllergenType`

Поле как в Варианте A, но при создании ингредиента маска **предзаполняется** автоматически по `AllergenType` (Gluten → +GlutenFree, Dairy → +LactoseFree). Модератор может допиливать вручную.

- **Плюсы:** меньше ручной работы; минимальное расширение Варианта A.
- **Минусы:** скрытая магия в фабрике `Ingredient.Create`; неочевидно при чтении кода. Заглушка над Вариантом A — не самостоятельная альтернатива.
- **Отклонён** на данном этапе. Может быть введён инкрементально позже, без нового ADR — это «вспомогательное удобство», а не архитектурное решение.

---

## 3. Decision (Принятое решение)

1. **`Ingredient` получает поле `DietConflictsMask: DietLabels`** (битовая маска). Default `DietLabels.None`. Заполняется только модератором/админом через UC-DSH-110/111. Семантика бита: «если ингредиент в рецепте — блюдо НЕ может иметь эту диет-метку».
2. **Инвариант блюда:** для каждого catalog-ингредиента в `Recipe.Ingredients` выполняется `Dish.DietLabelsMask AND Ingredient.DietConflictsMask == 0`. Freeform-позиции — вне инварианта (см. §5 Areas of Caution).
3. **Автокоррекция при изменении состава:** существующий Domain-метод `Dish.RecalculateAllergens` переименовывается в `Dish.RecalculateDishMarkers(IReadOnlyDictionary<Guid, IngredientMarkers> markers, DateTimeOffset utcNow)`, где `IngredientMarkers` — `record(AllergenType Allergens, DietLabels DietConflicts)`. Метод за один проход обновляет и `AllergensMask` (как раньше), и `DietLabelsMask` (новое: `DietLabelsMask &= ~combinedConflictsMask`). Если итоговая `DietLabelsMask` отличается от исходной — поднимается событие `DishDietLabelsAutoCorrectedEvent(DishId, RemovedLabels, RaisedAt)`.
4. **UC-DSH-009 SetDietLabels (полная реализация):** Domain-метод `Dish.SetDietLabels(desiredMask, IReadOnlyDictionary<Guid, DietLabels> ingredientConflicts, utcNow)` возвращает `Result`:
   - Собирает `combinedConflictsMask` из словаря по текущим catalog-ингредиентам.
   - Если `desiredMask AND combinedConflictsMask != 0` — возвращает `DishesErrors.DietLabelsConflictWithComposition`.
   - Иначе присваивает маску, поднимает `DishUpdatedEvent`, фиксирует `UpdatedAt`.
5. **Application Handler** (`SetDietLabelsCommandHandler`) собирает словарь конфликтов через новый репозиторный метод `IIngredientRepository.GetMarkersByIdsAsync(IReadOnlyCollection<Guid> ids, ...)` (тот же словарь подойдёт и для будущих UC-DSH-030..032).
6. **Freeform-ингредиенты** не учитываются ни в `combinedConflictsMask`, ни в инварианте. Они обрабатываются по принципу «ответственность автора» (как и для `HasUnverifiedAllergens`).
7. **Снепшот:** `Dish.PublishedVersionData` хранит уже автокорректированный `DietLabelsMask`. `DietConflictsMask` ингредиентов в снепшот не денормализуется — перерасчёт работает по основным таблицам.
8. **Опубликованные блюда не пересчитываются автоматически** при изменении `Ingredient.DietConflictsMask` (см. §6 Future Scope).
9. **Миграция:**
   ```sql
   ALTER TABLE dishes."Ingredients"
   ADD COLUMN "DietConflictsMask" integer NOT NULL DEFAULT 0;
   ```
   Никаких изменений в `Dishes`, `RecipeIngredients`, в jsonb-снепшоте.

---

## 4. Rationale (Обоснование)

1. **Прямое поле — кратчайший путь от потребности к решению.** Один день на добавление колонки и фабрики Vs неделя на введение сущности `IngredientCategory` с её admin-командами, миграциями, документацией.
2. **Семантическая симметрия с `AllergenType`** — паттерн «маркер ингредиента → пересчёт маркера блюда» уже работает. Расширение существующего метода `RecalculateAllergens` до `RecalculateDishMarkers` — естественная эволюция.
3. **`AllergenType` не покрывает значимую часть конфликтов.** Мясо/рыба не аллергены, но конфликтуют с Vegan/Vegetarian. Алкоголь не аллерген, но конфликтует с Halal. Сахар не аллерген, но конфликтует с SugarFree/KetoFriendly. Поле `DietConflictsMask` явно выражает связи, не выразимые через аллергены.
4. **Асимметрия поведения — auto-clear vs Reject.** При изменении состава автор фокусирован на рецепте, неявное снятие фоновой диет-метки приемлемо. При установке метки автор фокусирован на ней, неявное молчаливое снятие = UX-сюрприз; правильнее — понятная ошибка с указанием конфликта. Это согласуется с принципом «explicit user action → explicit feedback».
5. **`RecalculateDishMarkers` — единый метод вместо двух.** Один проход по `Recipe.Ingredients`, один Application-вызов, один репозиторный round-trip. При добавлении третьего маркера (например, `EthicalLabels` в будущем) сигнатура расширяется одним полем, потребители не ломаются.

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- Инвариант «`DietLabelsMask` блюда согласован с составом рецепта по catalog-позициям» защищается на уровне Domain (одна проверка в `SetDietLabels`, одна — в `RecalculateDishMarkers`).
- UC-DSH-009 получает полноценную реализацию: предсказуемая 409-ошибка при попытке поставить конфликтующую метку.
- Auto-clear при изменении состава снимает с автора часть ручной работы по поддержанию согласованности и даёт UI чёткий сигнал через `DishDietLabelsAutoCorrectedEvent`.
- Существующий `Dish.AllergensMask` остаётся работать ровно как раньше — новая логика добавляется параллельно, не заменяет.

### Negative / Trade-offs (Отрицательные / компромиссы)

- Ручная работа модератора по заполнению `DietConflictsMask` существующих сидовых ингредиентов. На Этапе 2 это допустимо (catalog мал); постепенное обновление через UC-DSH-111.
- Дублирование информации между `AllergenType` и `DietConflictsMask` (Gluten ⇔ GlutenFree, Dairy ⇔ LactoseFree). Модератор может ошибочно оставить рассогласование. Митигация — admin-валидация при сохранении ингредиента (вне скоупа этого ADR; см. §6 Future Scope).
- Freeform-позиции не покрыты автокоррекцией — теоретически блюдо со свининой-freeform может остаться с `Vegan`. Митигация — `HasUnverifiedAllergens` уже сигнализирует UI о неполноте маркеров; модерация со временем сводит количество freeform к нулю.

### Areas of Caution (На что обратить внимание)

- **Round-trip-тест `RecalculateDishMarkers`** — обе ветки (auto-clear DietLabels + AllergensMask), включая случай без правок (event не поднимается).
- **Unit-тест `Dish.SetDietLabels`** — happy path, ошибка `DietLabelsConflictWithComposition`, пустой рецепт (combined = 0 → любая маска валидна), смешанный рецепт catalog + freeform (freeform игнорируется).
- **При код-ревью UC-DSH-030..032** — убедиться, что Handler вызывает `RecalculateDishMarkers` после состав-команды, а не отдельные методы для аллергенов/диет.
- **При admin UC-DSH-111** — после изменения `DietConflictsMask` ингредиента **рабочие версии блюд не пересчитываются автоматически** (см. §6). Это потенциальный источник тихого рассогласования; митигация — в §6.

---

## 6. Future Scope (Будущие направления)

- **Массовая перегенерация снепшотов при изменении `Ingredient.DietConflictsMask`.** Когда модератор через UC-DSH-111 правит маску у популярного ингредиента, все блюда, где он присутствует, требуют пересчёта `DietLabelsMask` + при наличии снепшота — его перегенерации. Подход — общий механизм фоновой инвалидации (журнал `DishSnapshotInvalidation` + фоновый процесс), уже упомянутый в README модуля для merge тегов. Реализация — Этап 4+/8+.
- **Сущность `IngredientCategory`.** Появится позже для UI «Энциклопедия ингредиентов» (Этап 4+). На тот момент категория **дополнит** `DietConflictsMask`, а не заменит его — категория удобна для UI группировки/фильтрации; конфликтная маска прямо хранит бизнес-инвариант. Связки между ними могут быть выражены через admin-валидацию консистентности при сохранении ингредиента.
- **Admin-валидация консистентности `AllergenType ↔ DietConflictsMask`.** При создании/правке ингредиента можно подсветить ошибку модератору, если `AllergenType` подразумевает биты, отсутствующие в `DietConflictsMask`. Не обязательная блокирующая проверка, а warning. Вне скоупа этого ADR.
- **Третий маркер (например, `EthicalLabels` — fair trade, organic).** Если потребуется — расширяем `IngredientMarkers` record, обновляем `RecalculateDishMarkers` (одно поле в сигнатуре), добавляем колонку в `Ingredient`. Без правок Domain-методов карточки. Этот ADR показывает, как делать.

---

## 7. Implementation Reference (Связь с кодовой базой)

### Реализовано вместе с ADR

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Ingredient.cs` — поле `DietConflictsMask`, расширенная фабрика `Create`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Dish.cs` — `SetDietLabels(desiredMask, ingredientConflicts, utcNow): Result`; `RecalculateDishMarkers(markers, utcNow)` (заменяет `RecalculateAllergens`).
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/IngredientMarkers.cs` (новый) — value-record `(AllergenType Allergens, DietLabels DietConflicts)`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Events/DishDietLabelsAutoCorrectedEvent.cs` — доменное событие автокоррекции.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Errors/DishesErrors.cs` — `DietLabelsConflictWithComposition` (Conflict).
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Repositories/IIngredientRepository.cs` — метод `GetMarkersByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken)`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Persistence/Configurations/IngredientConfiguration.cs` — колонка `DietConflictsMask`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Repositories/IngredientRepository.cs` (новый) — реализация `GetMarkersByIdsAsync`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Application/Commands/SetDietLabels/*` — UC-DSH-009 Command/Handler/Validator.
- `src/GastronomePlatform.WebAPI/Controllers/Dishes/DishesController.cs` — эндпоинт UC-DSH-009.
- Миграция EF Core `AddDietConflictsMask`.

### Связано, но реализуется отдельно

- **UC-DSH-030..032 (Add/Update/Remove RecipeIngredient)** — на момент принятия ADR не реализованы. Когда будут реализоваться — их Handler-ы обязаны вызывать `Dish.RecalculateDishMarkers(...)` после состав-команды.
- **UC-DSH-110/111** — admin-команды управления `Ingredient`. Поле `DietConflictsMask` должно быть включено во входные параметры команд при их реализации.

---

## История изменений

- **2026-06-07:** Accepted.
