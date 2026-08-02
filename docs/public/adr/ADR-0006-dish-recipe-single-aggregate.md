# ADR-0006: Dish + Recipe как один агрегат

**Status:** Accepted
**Date:** 2026-04-26
**Updated:** 2026-08-02 — финализация при переносе в `docs/public/adr/`; ранее 2026-05-23 — Implementation Reference расширен под фактическую реализацию агрегата
**Stage:** 2

---

## Related (Связи)
- **Связанные ADR:** [ADR-0002](./ADR-0002-clean-architecture.md) (Clean Architecture — Dish/Recipe живут в Domain-слое), [ADR-0004](./ADR-0004-postgresql-schema-per-module.md) (PostgreSQL со схемами — OnDelete-стратегии внутри агрегата), [ADR-0011](./ADR-0011-two-layer-dish-storage.md) (двухслойное хранение — снепшот собирается из полного агрегата), [ADR-0012](./ADR-0012-recipe-ingredient-discriminated-union.md) (RecipeIngredient — элемент коллекции внутри агрегата), [ADR-0019](./ADR-0019-dish-updated-at-interceptor.md) (автообновление `UpdatedAt` при правках сущностей агрегата), [ADR-0020](./ADR-0020-allergens-mask-on-dish-aggregate.md) (денормализованные маркеры состава на корне)
- **Связанные модули:** Dishes
- **Связанная документация:** `docs/public/modules/dishes/domain-model.md` (ключевые решения, Dish, Recipe), `docs/public/modules/dishes/use-cases/README.md`

---

## 1. Context (Контекст)

Модуль Dishes проектируется вокруг центральной сущности — блюда с рецептом. В доменной модели блюдо содержит: карточную часть (название, описание, категории, теги, диетические метки), рецептную часть (шаги, ингредиенты, таймингов, выход, пищевая ценность). Между блюдом и рецептом — отношение 1:1: не может быть блюда без рецепта, не может быть рецепта в отрыве от блюда.

Проектирование модуля требовало определить: **где проходит граница агрегата** между Dish и Recipe. От этого выбора зависят: организация репозиториев, транзакционные границы, инкапсуляция инвариантов, схема EF Core, шаблон публикации доменных событий.

При этом решение должно соответствовать зафиксированной ранее Clean Architecture (ADR-0002) — с чёткими границами между Domain и Application — и не создавать препятствий для последующего двухслойного хранения (ADR-0011), где снепшот собирается из полного агрегата в момент публикации.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Два отдельных агрегата (Dish и Recipe независимы)

Dish и Recipe — независимые `AggregateRoot`, связаны через FK. У каждого свой репозиторий (`IDishRepository`, `IRecipeRepository`), свои транзакции, свои события.

**Плюсы:** классическое DDD-разделение по «границам изменений» (aggregate boundaries). Меньшие агрегаты — меньший scope одной операции.
**Минусы:**
- Инварианты, связывающие Dish и Recipe («опубликовать можно только если у рецепта есть шаги и ингредиенты»), невозможно защитить внутри одного агрегата. Application-слой вынужден координировать проверку между двумя объектами.
- Транзакционная сложность: одновременное изменение Dish и Recipe требует координации двух репозиториев в одной транзакции.
- При публикации (снепшот) Application должен подтянуть Recipe вместе с Dish — де-факто загрузка «одного целого» двумя запросами.

**Причина отклонения:** отношение 1:1 между Dish и Recipe и жёсткая связность инвариантов не оправдывают разделение на два агрегата. Разделение создало бы искусственную границу, которую Application был бы вынужден постоянно преодолевать.

### Вариант B — Recipe как ValueObject внутри Dish

Recipe — иммутабельный ValueObject, часть Dish. Обновление рецепта = замена всего объекта Recipe целиком.

**Плюсы:** максимальная простота и жёсткая иммутабельность. Нет отдельной таблицы `Recipe` — данные лежат внутри `Dish`.
**Минусы:**
- Recipe имеет идентичность и жизненный цикл: шаги можно добавлять по одному (UC-DSH-020..023), ингредиенты — по одному (UC-DSH-030..033), тайминги обновляются отдельно (UC-DSH-040). Замена всего объекта при каждой правке шага — некорректная семантика.
- Recipe содержит подколлекции (`RecipeStep[]`, `RecipeIngredient[]`) с собственной идентичностью и порядком — это не подходит под ValueObject.
- ValueObject не может иметь свою таблицу в EF Core (только owned type в той же таблице) — не масштабируется на 10+ шагов, 15+ ингредиентов.

**Причина отклонения:** природа Recipe противоречит семантике ValueObject. Recipe — это сущность с идентичностью и жизненным циклом, а не значение.

### Вариант C — Один агрегат: Dish как AggregateRoot, Recipe как внутренняя Entity ⭐

Dish — корень агрегата (`AggregateRoot<Guid>`). Recipe — Entity внутри агрегата (1:1). Одна операция — одна транзакция, один репозиторий, один источник событий. Правки Recipe идут через методы Dish (wrapper-методы), внутренние методы Recipe помечены `internal`.

**Плюсы:**
- Инварианты «блюда с рецептом» защищаются в одном месте — на уровне корня.
- Один репозиторий `IDishRepository` — простая модель загрузки/сохранения. Транзакционная граница совпадает с границей операции.
- Публикация (снепшот) собирается из одного связного объекта — Application запросил Dish целиком, сериализовал в jsonb.
- События (`DishPublishedEvent`) поднимаются корнем — единая точка публикации.
- Естественная сборка снепшота при `Dish.Publish()` — Recipe уже часть объекта.

**Минусы:**
- Загрузка полного агрегата (Dish + Recipe + шаги + ингредиенты) — многострочный запрос. Не для каждого UC нужен полный объект (например, UC-DSH-002 «Обновить карточку» не трогает Recipe). Митигация — разные перегрузки в репозитории с разной глубиной `Include()`.
- Concurrent modification одного блюда двумя ролями (автор + модератор) может вызвать конфликты, если они правят разные части. Митигация — оптимистичная блокировка (`xmin` в PostgreSQL) на Этапе 8+, если проблема реально возникнет.

---

## 3. Decision (Принятое решение)

**Dish — `AggregateRoot<Guid>`, Recipe — Entity внутри агрегата Dish, отношение 1:1.**

### Правила инкапсуляции

1. **Recipe не имеет самостоятельного жизненного цикла.** Создаётся вместе с Dish (в фабрике `Dish.Create(...)` — через `Recipe.CreateForDish(dishId)`). Удаляется вместе с Dish (cascade delete). Не может существовать в отрыве.

2. **Recipe помечен как внутренняя часть агрегата.** Методы Recipe, изменяющие состояние (`Recipe.UpdateIntroduction`, `Recipe.SetIsAlcoholic`, `Recipe.AddStep`, `Recipe.AddIngredient` и т.п.), объявлены `internal` — прямой вызов из Application-слоя запрещён на уровне модификатора доступа.

3. **Wrapper-методы на Dish** — единственная точка входа в изменения рецепта из Application:
   - `Dish.UpdateRecipeIntroduction(text, clock)` → `_recipe.UpdateIntroduction(text)` + `MarkAsUpdated(clock)`.
   - `Dish.SetRecipeIsAlcoholic(value, clock)` → `_recipe.SetIsAlcoholic(value)` + `MarkAsUpdated(clock)`.
   - `Dish.AddRecipeStep(...)` → `_recipe.AddStep(...)` + `MarkAsUpdated(clock)`.
   - и т.д.

4. **Единый репозиторий `IDishRepository`.** Не создаётся `IRecipeRepository`. Загрузка агрегата — через `IDishRepository` с перегрузками разной глубины:
   - `GetByIdAsync(id)` — только Dish (для UC, не трогающих Recipe).
   - `GetByIdWithRecipeAsync(id)` — Dish + Recipe (без коллекций шагов/ингредиентов).
   - `GetByIdWithFullRecipeAsync(id)` — Dish + Recipe + шаги + ингредиенты + Timing + Yield + Nutrition (для UC-DSH-004 Publish и других операций, требующих полного агрегата).

5. **События поднимает корень.** `DishPublishedEvent`, `DishUpdatedEvent` — регистрируются через `Dish.RaiseDomainEvent(...)`. Recipe напрямую события не поднимает — при необходимости передаёт факт изменения корню.

### Cascade delete

На уровне EF Core: `Dish → Recipe` через `OnDelete(DeleteBehavior.Cascade)`. `Recipe → RecipeStep`, `Recipe → RecipeIngredient` — тоже cascade. Удаление Dish каскадно очищает весь агрегат.

---

## 4. Rationale (Обоснование)

1. **Отношение 1:1 естественно ложится на «один агрегат».** Разделение на два агрегата создаёт искусственную границу, которая не соответствует бизнес-семантике («блюдо без рецепта не имеет смысла»).

2. **Инварианты защищены компилятором.** Правило «опубликовать можно только если у рецепта есть шаги» проверяется внутри `Dish.Publish()` — Recipe уже доступен как поле. Никакой Application-код не может обойти проверку.

3. **Одна транзакция — одна операция.** Правка карточки, добавление шага, замена ингредиента — всё сохраняется одним `SaveChangesAsync()` на `IDishRepository`. Application не занимается координацией нескольких репозиториев.

4. **Естественная сборка снепшота.** ADR-0011 требует, чтобы при `PublishDish` собирался снепшот из «полного блюда». Если Dish и Recipe — один агрегат, снепшот собирается из одного объекта, уже прогруженного в память. При двух агрегатах пришлось бы явно координировать загрузку.

5. **Wrapper-методы через `MarkAsUpdated`.** Каждый wrapper на Dish автоматически ставит `Dish.UpdatedAt = clock.UtcNow`. Это обеспечивает корректную работу индикатора «есть неопубликованные правки» (`UpdatedAt > PublishedAt`, см. ADR-0011) без необходимости помнить об этом в каждом методе Recipe.

---

## 5. Consequences (Последствия)

### Positive (Положительные)
- Инварианты «блюда с рецептом» защищены на уровне Domain — Application не может их обойти.
- Один репозиторий, одна транзакция, один источник событий — простая ментальная модель.
- Естественная сборка jsonb-снепшота при публикации (ADR-0011) — из одного связного объекта.
- Cascade delete работает корректно: удалить Dish = удалить весь агрегат, БД гарантирует консистентность.
- Правки Recipe автоматически инкрементируют `Dish.UpdatedAt` через wrapper-методы.

### Negative / Trade-offs (Отрицательные / компромиссы)
- Полная загрузка агрегата — тяжёлый SQL-запрос. Митигация: разные перегрузки `IDishRepository` с явной глубиной загрузки.
- Recipe нельзя мокать в тестах Dish отдельно — они всегда вместе. На практике это скорее плюс: тесты работают с настоящим агрегатом.
- Concurrent modification автором и модератором может вызвать оптимистичные конфликты. На Этапе 2 — приемлемо (авторы редко правят одновременно с модерацией). На Этапе 8+ — рассмотреть `xmin`-версию.
- Application-разработчик может по инерции пытаться вызвать метод Recipe напрямую — компилятор запретит (`internal`), но это требует объяснения при обучении.

### Areas of Caution (На что обратить внимание)
- При добавлении новых полей в Recipe — не забывать добавить wrapper-метод в Dish. Прямой вызов из Application не сработает.
- Загрузка агрегата в handler'ах — использовать перегрузку под конкретный сценарий (`GetByIdAsync` для UC без Recipe; `GetByIdWithFullRecipeAsync` для Publish). Не грузить всё подряд — производительность.
- Каскадное удаление действует и на связующие таблицы `DishCategory`, `DishTag`, `DishCategoryPublished`, `DishTagPublished` — при удалении Dish эти связи исчезают. Осмысленно и ожидаемо.
- События (`DishPublishedEvent` и т.п.) регистрируются через `RaiseDomainEvent()` на Dish. Recipe в этом не участвует, чтобы избежать двойной публикации.
- **Навигационные свойства в справочниках Dishes не используются — только `Guid`-FK поля.** Справочниковые сущности (`MeasureUnit`, `Tag`, `Nutrition`, `Category`, `Ingredient`, `IngredientSpec`) не имеют навигационных свойств ни на соседей, ни друг на друга. FK конфигурируются через `HasOne<T>().WithMany()` без объявления `public T Navigation { get; }`-свойств на обеих сторонах. Причина: справочники лукапятся редко и обычно поодиночке (`GetByIdAsync`, `GetByCodeAsync`, `GetByNameAsync`), а в каталожных запросах участвуют как «таблицы фактов» (FK targets), не как «корни запросов». Отсутствие навигаций уменьшает риск N+1 и делает Domain-модель проще. Рассматривавшееся исключение — `RecipeIngredient` (навигации на `Ingredient`/`IngredientSpec`/`MeasureUnit`) — **не понадобилось**: при реализации чтения рецепта (UC-DSH-052) публичная версия отдаётся из jsonb-снепшота (ADR-0011), а рабочая маппится из агрегата по `Guid`-полям; резолв имён справочников выполняют клиенты через отдельные lookup-эндпоинты. Итог: во всём модуле Dishes справочники без навигационных свойств.

---

## 6. Future Scope (Будущие направления)

- **Этап 8+:** При появлении сценария «конкурентная правка одного блюда несколькими ролями» — оптимистичная блокировка через `xmin` (PostgreSQL) или `Version` (int-инкремент) в `Dish`. Recipe не нуждается в собственной версионности — правки идут через корень.
- **Этап 8+:** При появлении полноценной модерации — возможное разделение операций «правка автором» и «правка модератором» на уровне wrapper-методов, с разными политиками авторизации (расширение POL-001).

---

## 7. Implementation Reference (Связь с кодовой базой)

**Domain:**

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Dish.cs`
  — `AggregateRoot<Guid>`. Навигация `public Recipe Recipe { get; private set; }`, устанавливается в фабрике. Wrapper-методы к Recipe:
  - Простые поля: атомарный `UpdateRecipe(...)` (UC-DSH-003) + точечные `UpdateRecipeIntroduction`, `SetRecipeIsAlcoholic`, `UpdateRecipeAuthorTips`, `UpdateRecipeNotes`, `UpdateRecipeServingSuggestions`, `SetRecipeServingsDefault` (возвращает `Result`).
  - Owned-сущности: `UpdateTiming(...)`, `UpdateYield(...)`, `UpdateNutrition(...)`.
  - Шаги: `AddRecipeStep`, `UpdateRecipeStep`, `RemoveRecipeStep`, `ReorderRecipeSteps`.
  - Ингредиенты: `AddRecipeIngredientFromCatalog`, `AddRecipeIngredientFreeform`, `UpdateRecipeIngredient`, `RemoveRecipeIngredient`, `ReorderRecipeIngredients`.
  Все wrapper'ы делегируют в internal-метод Recipe и завершаются обновлением `UpdatedAt` с поднятием `DishUpdatedEvent` (напрямую либо через `MarkAsUpdated(utcNow)`).

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Recipe.cs`
  — `Entity<Guid>`. Фабрика `internal static Recipe CreateForDish(Guid dishId)` — единственный конструктор рецепта; вызывается только из `Dish.Create(...)`. Внутри создаёт `Timing.CreateForRecipe(...)` и `Yield.CreateForRecipe(...)` c дефолтами; `Nutrition` остаётся `null` до первого `UpdateNutrition`. Все методы модификации (`UpdateIntroduction`, `SetIsAlcoholic`, `SetServingsDefault`, `AddStep`, `UpdateStep`, `AddIngredientFromCatalog`, `AddIngredientFreeform`, `UpdateIngredient`, ...) — `internal`. Backing fields: `_steps`, `_ingredients` (private, EF-mapped). Публичные read-only коллекции: `Steps => _steps`, `Ingredients => _ingredients` (тип `IReadOnlyList<T>`).

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/RecipeStep.cs`, `RecipeIngredient.cs`, `Timing.cs`, `Yield.cs`, `Nutrition.cs`
  — все `Entity<Guid>`, все методы модификации `internal`. `RecipeStep` и `RecipeIngredient` создаются через `internal static CreateForRecipe(...)` / `CreateFromCatalog(...)` / `CreateFreeform(...)`.

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Repositories/IDishRepository.cs`
  — перегрузки загрузки разной глубины: `GetByIdAsync`, `GetByIdWithRecipeAsync`, `GetByIdWithFullRecipeAsync` (полный агрегат, включая `*Published`-коллекции — см. ADR-0011 Areas of Caution), `GetByIdWithLinksAsync` (связки категорий/тегов), `GetByIdWithPublishedLinksAsync`; плюс `GetBySlugAsync`, списочные методы каталога и `SaveChangesAsync`.

**Infrastructure:**

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Persistence/Configurations/DishConfiguration.cs`
  — навигация Dish → Recipe с UNIQUE-индексом на `Recipe.DishId` и `OnDelete.Cascade`. `Navigation(d => d.Categories).HasField("_categories").UsePropertyAccessMode(PropertyAccessMode.Field)` — аналогично для `Tags`, `CategoriesPublished`, `TagsPublished`.

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Persistence/Configurations/RecipeConfiguration.cs`
  — `Navigation(r => r.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field)` для инкапсуляции backing field. Аналогично для `_ingredients`. Recipe не имеет обратной навигации на Dish — только поле `DishId`.

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Persistence/Configurations/IngredientConfiguration.cs`, `MeasureUnitConfiguration.cs`, `CategoryConfiguration.cs`, `TagConfiguration.cs` — примеры конфигураций справочников без навигационных свойств (`HasOne<T>().WithMany()` без `public T Nav`-свойства ни на одной из сторон).

Реализация агрегата зафиксирована миграциями: `20260517074806_AddRecipeAndOwnedEntities` (Recipe + Timing + Yield), `20260517143518_AddRecipeStepsAndIngredients`, `20260523075145_AddDishMmTables` (M:M-связки; одновременно — `UpdatedAtInterceptor`, ADR-0019).

---

## История изменений

- **2026-04-26:** Accepted. Решение принято при проектировании доменной модели Dishes (Этап 2 проектирование).
- **2026-05-10:** Добавлен параграф про навигационные свойства в справочниках Dishes (Areas of Caution) — систематизация правила, зафиксированного при реализации 6 справочников на v0.8.0.
- **2026-05-23:** Инструментализация в коде завершена: все методы Recipe (и вложенных RecipeStep/RecipeIngredient/Timing/Yield/Nutrition) помечены `internal`; wrapper-методы на Dish реализованы для всех сценариев модификации рецепта; backing fields для read-only коллекций подключены через `Navigation.HasField(...).UsePropertyAccessMode(PropertyAccessMode.Field)`. `IRecipeRepository` не создан — единый `IDishRepository`.
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`. Зафиксировано: исключение по навигациям `RecipeIngredient` не понадобилось (чтение рецепта — снепшот/Guid-поля); список перегрузок `IDishRepository` актуализирован; добавлен атомарный `UpdateRecipe` в перечень wrapper-методов.
