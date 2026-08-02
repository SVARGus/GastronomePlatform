# ADR-0020: Денормализованные маркеры состава на корне Dish (`AllergensMask`, `HasUnverifiedAllergens`)

**Status:** Accepted
**Date:** 2026-05-17
**Updated:** 2026-08-02 — финализация при переносе в `docs/public/adr/`: отражена эволюция пересчёта в единый `RecalculateDishMarkers` (совместно с автокоррекцией диет-меток по ADR-0016), Implementation Reference актуализирован
**Stage:** 2

---

## Related (Связи)

- **Связанные ADR:** [ADR-0006](./ADR-0006-dish-recipe-single-aggregate.md) (Dish + Recipe как один агрегат — определяет, где может жить денормализованное поле), [ADR-0011](./ADR-0011-two-layer-dish-storage.md) (Двухслойное хранение — разделение «данные для фильтра каталога» / «данные для карточки»), [ADR-0012](./ADR-0012-recipe-ingredient-discriminated-union.md) (RecipeIngredient — discriminated union, источник значений для маски и причина появления `HasUnverifiedAllergens`), [ADR-0016](./ADR-0016-diet-conflicts-mask.md) (конфликты диет-меток — второй маркер, пересчитываемый тем же механизмом)
- **Связанные модули:** Dishes
- **Связанная документация:** `docs/public/modules/dishes/domain-model.md` (поля Dish), реестр UC Dishes — UC-DSH-030..032 (управление составом с пересчётом), UC-DSH-054 (каталожный фильтр)

---

## 1. Context (Контекст)

Домен требует хранить набор аллергенов блюда в виде битовой маски (`AllergenType : Flags` — Gluten, Dairy, Eggs, Nuts, Peanuts, Fish, Shellfish, Soy, Sesame, Mustard, Celery, Sulphites). Значение вычисляется из состава: для каждого `RecipeIngredient` со ссылкой на справочник берётся `Ingredient.AllergenType`, всё OR-ится.

Первоначальная доменная модель размещала `AllergensMask` на `Recipe` — рядом с составом, откуда маска вычисляется. При реализации агрегата обнаружились три обстоятельства против этого размещения:

1. **Каталожный фильтр UC-DSH-054** («блюда без орехов и молочных продуктов»). В двухслойной модели (ADR-0011) каталог фильтруется по `Dish` без обращения к дочерним сущностям. Маска на `Recipe` — это JOIN на самом частом запросе платформы.
2. **Потребность в `HasUnverifiedAllergens : bool`.** RecipeIngredient — discriminated union (ADR-0012): позиция либо ссылается на справочник (аллергены известны), либо задана свободным текстом (аллергены неизвестны). UI нужен явный флаг «маска может быть неполной» для дисклеймера. Флаг — сигнал о полноте маски и логически неотделим от неё.
3. **Симметрия с денормализованными полями Dish** (`RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount`) — все хранятся на корне для быстрого чтения в каталоге. `AllergensMask` — того же типа: денормализованный публичный маркер.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Оставить на Recipe, JOIN в фильтре каталога

`Recipe.AllergensMask`; фильтр — `JOIN Recipes r ON r.DishId = d.Id WHERE r.AllergensMask & @excluded = 0`.

**Плюсы:** маска рядом с источником вычисления, инкапсуляция «свойство рецепта — на рецепте».
**Минусы:** JOIN на самом частом запросе платформы; нарушение принципа двухслойной модели (каталожные запросы не обращаются к дочерним сущностям).

**Причина отклонения:** производительность плюс архитектурная несовместимость с ADR-0011.

### Вариант B — Дублировать на оба места с синхронизацией

`Recipe.AllergensMask` (источник правды) + копия на `Dish` для каталога.

**Плюсы:** каталог быстрый, маска концептуально «на месте».
**Минусы:** два источника правды, инвариант «маски равны» надо поддерживать в каждом методе изменения состава. Избыточность без функциональной выгоды.

**Причина отклонения:** дублирование без выгоды — перенос на Dish решает ту же проблему без него.

### Вариант C — Нормализованная таблица `DishAllergens (DishId, AllergenType)`

Фильтр через `NOT EXISTS (...)`.

**Плюсы:** 1:M-нормализация без битовых операций; произвольные аллергены.
**Минусы:** усложнение схемы ради поля, естественно ложащегося на битовую маску. `AllergenType` — фиксированный enum (гайдлайны пищевой безопасности), не справочник. Битовая операция быстрее subquery.

**Причина отклонения:** избыточная сложность.

### Вариант D — На Dish, пересчёт явным Domain-методом ⭐

`Dish.AllergensMask` + `Dish.HasUnverifiedAllergens`; пересчёт — публичный метод Dish, принимающий словарь маркеров ингредиентов, который Application-слой собирает из справочника.

**Плюсы:** каталог без JOIN; один источник правды на корне; симметрия с другими денормализованными полями; `HasUnverifiedAllergens` рядом с маской; Application контролирует момент пересчёта.
**Минусы:** Domain-метод требует данных справочника — параметр-словарь, а не автономный пересчёт. Альтернатива (репозиторий в Domain) нарушала бы Clean Architecture.

---

## 3. Decision (Принятое решение)

**Размещение:**

- `Dish.AllergensMask : AllergenType` — на корне агрегата;
- `Dish.HasUnverifiedAllergens : bool` — рядом с маской;
- `Recipe` маску **не содержит** (первоначальное размещение отменено).

**Пересчёт — единый метод маркеров** (эволюция: изначально `RecalculateAllergens`; с принятием ADR-0016 расширен до единого прохода по составу):

```csharp
public void RecalculateDishMarkers(
    IReadOnlyDictionary<Guid, IngredientMarkers> markers,
    DateTimeOffset utcNow)
```

где `IngredientMarkers(AllergenType Allergens, DietLabels DietConflicts)` — доменный record-словарь маркеров справочного ингредиента. Метод за один проход по `Recipe.Ingredients`:

- OR-ит `Allergens` каталожных позиций → `AllergensMask`;
- поднимает `HasUnverifiedAllergens`, если есть хотя бы одна freeform-позиция;
- OR-ит `DietConflicts` и **молча снимает** конфликтующие биты из `DietLabelsMask` (silent auto-clear, ADR-0016);
- обновляет `UpdatedAt` и поднимает `DishUpdatedEvent`.

**Ответственность Application:**

Handler-ы изменения состава (UC-DSH-030 добавление, 031 обновление, 032 удаление) после успешной модификации вызывают пересчёт через сервис `IDishMarkersRecalculator` (Application/Services): сервис собирает словарь `IIngredientRepository.GetMarkersByIdsAsync(catalogIds, ct)` и вызывает Domain-метод. UC-DSH-033 (переупорядочивание) пересчёт **не вызывает** — состав не меняется.

**Инвариант:**

- `AllergensMask` и `HasUnverifiedAllergens` изменяются **только** через `RecalculateDishMarkers` — прямых сеттеров нет.
- `HasUnverifiedAllergens = true` ⇔ существует хотя бы одна позиция с `FreeformText != null`.

---

## 4. Rationale (Обоснование)

1. **Производительность каталога.** `WHERE d.AllergensMask & @excluded = 0` — битовая операция на поле Dish, без JOIN, на самом частом запросе платформы.
2. **Симметрия с денормализованными полями.** Единый паттерн размещения (`RatingAvg`, `ViewsCount`, ... и маркеры состава) — единообразная модель для читателя кода.
3. **`HasUnverifiedAllergens` рядом с маской.** Пара «маска + признак её полноты» живёт в одном месте — критично для честного UI на платформе с пользовательским контентом.
4. **Единый источник правды.** Нет дублирования — нет и синхронизации.
5. **Application контролирует пересчёт.** Вызов явный (виден в Handler-е/сервисе), тестируемый, оптимизируемый (Reorder не пересчитывает).
6. **Один проход для двух масок.** Объединение с логикой ADR-0016 в `RecalculateDishMarkers` избавило от двух проходов по составу и двух запросов к справочнику.

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- Каталожный фильтр по аллергенам — без JOIN, предсказуемая латентность.
- Один источник правды на корне агрегата.
- `HasUnverifiedAllergens` даёт UI сигнал для дисклеймера «состав аллергенов может быть неполным».
- Domain-метод — чистое преобразование без инфраструктурных зависимостей, легко тестируется.
- Побочная выгода объединения с ADR-0016: диет-метки не могут молча разъехаться с составом.

### Negative / Trade-offs (Отрицательные / компромиссы)

- Handler обязан вызвать пересчёт после каждой модификации состава — если забыть, маска устареет. Митигация: единый сервис `IDishMarkersRecalculator`, один и тот же вызов во всех трёх Handler-ах.
- Пересчёт требует запроса маркеров ингредиентов — дополнительный запрос к БД в UC-DSH-030..032. Митигация: `GetMarkersByIdsAsync` возвращает словарь одним запросом.
- Параметр-словарь в Domain-методе — менее «доменный», чем автономный пересчёт, но не нарушает границы слоёв.

### Areas of Caution (На что обратить внимание)

- **UC-DSH-033 (переупорядочивание) не вызывает пересчёт** — осмысленное исключение: состав не меняется.
- **Freeform-позиции** учитываются только для `HasUnverifiedAllergens`, не для маски. Блюдо из одних freeform-позиций: `AllergensMask = None`, `HasUnverifiedAllergens = true` — UI обязан показывать «аллергены не проверены», а не «аллергенов нет».
- **Изменение `Ingredient.AllergenType` (админ, Этап 8+)** устаревает маски всех блюд с этим ингредиентом — пересчёт войдёт в каскадные обновления (`DishSnapshotInvalidation`, ADR-0011 §3.5). До этого этапа админские правки масок справочника не выполняются.
- **Полнота словаря — ответственность Application:** идентификаторы, отсутствующие в словаре, интерпретируются как «без маркеров».

---

## 6. Future Scope (Будущие направления)

- **Этап 8+:** каскадный пересчёт маркеров при админских изменениях справочника — через механизм `DishSnapshotInvalidation` (ADR-0011) с типом события «recalculate markers».
- **При появлении новых денормализованных маркеров состава** (например, будущая маска «острота») — расширять `IngredientMarkers` и единый проход `RecalculateDishMarkers`, не заводить отдельные методы пересчёта.

---

## 7. Implementation Reference (Связь с кодовой базой)

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Dish.cs` — свойства `AllergensMask : AllergenType`, `HasUnverifiedAllergens : bool` (оба `private set`); метод `RecalculateDishMarkers(IReadOnlyDictionary<Guid, IngredientMarkers>, DateTimeOffset)`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/IngredientMarkers.cs` — `record IngredientMarkers(AllergenType Allergens, DietLabels DietConflicts)`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Enums/AllergenType.cs` — `[Flags]`: None, Gluten, Dairy, Eggs, Nuts, Peanuts, Fish, Shellfish, Soy, Sesame, Mustard, Celery, Sulphites.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Repositories/IIngredientRepository.cs` — `Task<IReadOnlyDictionary<Guid, IngredientMarkers>> GetMarkersByIdsAsync(...)`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Application/Services/IDishMarkersRecalculator.cs` + `DishMarkersRecalculator.cs` — сборка словаря и вызов Domain-метода.
- Handler-ы состава: `Application/Commands/AddCatalogIngredientToRecipe/`, `AddFreeformIngredientToRecipe/`, `UpdateRecipeIngredient/`, `RemoveRecipeIngredient/` — вызывают рекалькулятор после модификации.
- `Infrastructure/Persistence/Configurations/DishConfiguration.cs` — маппинг маски и флага, NOT NULL с дефолтами.

---

## История изменений

- **2026-05-17:** Accepted. Решение принято при реализации `RecipeIngredient` и логики пересчёта аллергенов; одновременно введено поле `HasUnverifiedAllergens`. Пересчёт — метод `RecalculateAllergens`.
- **2026-05-23:** Синхронизирована `domain-model.md`: описание маски перенесено из раздела Recipe в раздел Dish; UC-DSH-030..032 дополнены явным пересчётом.
- **2026-06-07:** С принятием ADR-0016 пересчёт объединён в единый `RecalculateDishMarkers(IReadOnlyDictionary<Guid, IngredientMarkers>, DateTimeOffset)` — один проход по составу обновляет маску аллергенов, флаг полноты и снимает конфликтующие биты диет-меток; в Application добавлен сервис `IDishMarkersRecalculator`.
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`, присвоен номер ADR-0020.
