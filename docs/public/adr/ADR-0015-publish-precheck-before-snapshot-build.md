# ADR-0015: Pre-check инвариантов публикации до сборки jsonb-снепшота — `Dish.CheckCanPublish`

**Status:** Accepted
**Date:** 2026-06-06
**Stage:** 2

---

## Related (Связи)

- **Связанные ADR:**
  - [ADR-0011 (зарезервирован)](./README.md) — двухслойное хранение Dish (основные таблицы + jsonb-снепшот). ADR-0015 — частный сценарий применения двухслойной модели в Publish-флоу.
  - [ADR-0012](./ADR-0012-recipe-ingredient-discriminated-union.md) — discriminated union ингредиентов; Builder снепшота полиморфно сериализует ветки.
  - [ADR-0013](./ADR-0013-publish-spam-protection.md) — Domain-инвариант «без правок нет повторной публикации». ADR-0015 переносит часть инвариантов на pre-check Application, оставляя их же в `Publish` как defense-in-depth.
- **Связанные модули:** Dishes.
- **Связанная документация:**
  - `docs/public/modules/dishes/use-cases/README.md` — UC-DSH-004 «Опубликовать блюдо».
  - `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — подробное описание сценария публикации.
- **Связь с кодом:** см. §7 Implementation Reference.

---

## 1. Context (Контекст)

UC-DSH-004 PublishDish реализован в Application как линейный поток:

1. Загрузка агрегата с полным `Recipe` (`GetByIdWithFullRecipeAsync`).
2. Проверка POL-001 (автор блюда совпадает с актором).
3. Сборка jsonb-снепшота через `IPublishedDishSnapshotBuilder.Build(dish)`.
4. Делегирование Domain: `dish.Publish(utcNow, snapshot)` — там все контентные и статусные инварианты (`CannotPublishArchivedDish`, `DishAlreadyPublished`, `MainImageRequiredForPublish`, `StepsRequiredForPublish`, `IngredientsRequiredForPublish`, `TimingRequiredForPublish` — все типа `Error.Conflict` → HTTP 409).

**Проблема порядка.** Шаг 3 (сборка снепшота) выполняется до шага 4 (доменные проверки). Последствия:

1. **Маскировка 409 как 500.** Если `PublishedDishSnapshotBuilder.Build` бросит исключение (NRE на повреждённом агрегате, `JsonException` / `NotSupportedException` при расширении DTO), клиент получит `500 Internal Server Error` от `GlobalExceptionHandlingMiddleware`. При этом доменная причина отказа (например, отсутствие шагов) могла бы быть точно отрисована как `409 DISHES.STEPS_REQUIRED_FOR_PUBLISH`.
2. **Работа впустую.** При самом частом отказе — `DishAlreadyPublished` (двойной клик «Опубликовать» в UI на блюде без правок, см. ADR-0013) — Builder уже отработал, сериализовав десятки полей агрегата и его дочерних сущностей. CPU и аллокации потрачены ради последующего отказа.
3. **Нарушение симметрии DDD.** Domain хранит инвариант готовности к публикации, но Application успевает выполнить побочную работу до того, как Domain получает шанс отказать.

**Сейчас риск (1) минимален.** Все navigation-поля (`Recipe`, `Recipe.Timing`, `Recipe.Yield`, `Recipe.Steps`, `Recipe.Ingredients`) — non-null по EF-конфигурации и Domain-конструкторам. Сериализация текущего набора DTO `PublishedDishSnapshot` / `PublishedRecipeDto` / etc. не падает. То есть проблема — архитектурная, не текущий боевой баг.

**Но риск будет расти.** На Этапах 4+/8+ Builder вероятно начнёт денормализовывать имена справочников (`CategoryName`, `TagName`, `IngredientName`) — обращаться к справочным репозиториям, конвертировать nullable-поля, валидировать ограничения. Каждое такое расширение увеличивает площадь исключений, способных «съесть» 409 за 500-м.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Оставить как есть (Build → Publish)

Никаких правок. Текущий порядок сохраняется, формальная фиксация как принятого статус-кво.

- **Плюсы:** ноль изменений в коде; реальный риск throws у текущего Builder ≈ 0.
- **Минусы:** не решает проблему ни сейчас, ни на будущих этапах; ADR-кандидат закрывается фразой «принято к сведению»; будущее расширение Builder воскрешает проблему.
- **Отклонён:** не отвечает на вопрос, к чему фиксировать дизайн при расширении Builder.

### Вариант B — Pure pre-check метод `Dish.CheckCanPublish(): Result` ⭐

Domain получает публичный метод `CheckCanPublish()` без побочных эффектов, возвращающий тот же `Result`, что и `Publish`. Application вызывает его перед `Builder.Build`; `Publish` сохраняет те же проверки как defense-in-depth. Помещение проверок в общий приватный helper `CheckPublishInvariants()` устраняет дублирование внутри агрегата.

- **Плюсы:**
  - Domain остаётся чистым (никаких делегатов и зависимостей на Application).
  - `Publish` сохраняет полный набор проверок — внешний вызывающий не может обойти инварианты, если забудет pre-check.
  - Дублирование локализовано в одном приватном helper-е.
  - `CheckCanPublish` — потенциально полезен для Query (флаг `CanPublish` / причина отказа в DTO без вызова сценария).
  - Сигнатура `Publish(utcNow, snapshot)` не меняется — минимизация рисков для существующих тестов и Application-кода.
- **Минусы:**
  - Дополнительный публичный метод на агрегате (расширение поверхности API).
  - Pre-check и Publish сейчас зовут одни и те же инварианты в одном Handler — два вызова вместо одного. Стоимость пренебрежимая (несколько if-ов), но архитектурно «лишний звонок».
- **Выбран ⭐**: лучший компромисс между чистотой Domain и решением проблемы порядка.

### Вариант C — `Dish.Publish(utcNow, Func<string> snapshotFactory)`

`Publish` принимает не готовый снепшот, а делегат-фабрику, которую вызывает только после прохождения всех инвариантов.

- **Плюсы:** единственный источник истины проверок (никакого дублирования); снепшот собирается ровно тогда, когда уже можно публиковать.
- **Минусы:**
  - Domain принимает делегат — слабая, но реальная зависимость на функцию из Application.
  - Сигнатура `Publish` становится «два аргумента, один из которых ленивая функция» — менее очевидна для junior-разработчика.
  - Усложняет unit-тестирование Domain: нужен mock делегата.
- **Отклонён:** архитектурная чистота уступает Варианту B; технический выигрыш «нет дублирования» нивелируется тем, что в Варианте B дублирование локализовано в одном private helper.

### Вариант D — `internal CheckCanPublish` + `InternalsVisibleTo` на `Dishes.Application`

Подвариант B: видимость метода ограничивается одним «привилегированным» соседом через атрибут assembly.

- **Плюсы:** строгое выражение «только Application того же модуля» средствами C#.
- **Минусы:**
  - Domain.csproj получает именное упоминание Application — слабая, но архитектурно нежелательная связь (даже без ProjectReference и без цикла компиляции).
  - Несогласованность со стилем класса: `Publish`, `Unpublish`, `Archive` и прочие lifecycle-методы — `public`; делать только pre-check `internal` — выделять один метод из общего ряда без бизнес-причины.
- **Отклонён**: чистота `Domain.csproj` приоритетнее, чем формальное усиление видимости одного метода.

---

## 3. Decision (Принятое решение)

1. На агрегате `Dish` добавляется **публичный** метод
   ```csharp
   public Result CheckCanPublish();
   ```
   Метод pure: не меняет состояние агрегата, не поднимает доменные события, возвращает тот же набор ошибок (и в том же порядке), что и `Publish`.
2. Инварианты публикации выносятся в приватный helper:
   ```csharp
   private Result CheckPublishInvariants();
   ```
   Это **единственный источник истины** проверок. Из него вызываются и `CheckCanPublish`, и `Publish`.
3. `Dish.Publish(utcNow, snapshot)` сохраняет проверки **как defense-in-depth**: первым шагом вызывает `CheckPublishInvariants()` и возвращает ошибку при `IsFailure` — внешний вызывающий не может обойти инварианты, забыв pre-check.
4. `PublishDishCommandHandler` встраивает pre-check между POL-001 и сборкой снепшота:
   ```text
   Load → POL-001 → CheckCanPublish → Build(snapshot) → Publish
   ```
   При `CheckCanPublish().IsFailure` Handler возвращает ошибку, не вызывая Builder.
5. Видимость `CheckCanPublish` — `public`, согласовано со стилем lifecycle-методов агрегата. Ограничение области применения выражается через XML-doc и code review, не через C#-модификатор. Альтернатива через `InternalsVisibleTo` рассмотрена и отклонена ради чистоты `Domain.csproj` (см. §2, Вариант D).
6. Сигнатура `Dish.Publish(DateTimeOffset, string)` **не меняется** — никаких делегатов, никаких новых параметров.

---

## 4. Rationale (Обоснование)

1. **Архитектурная корректность.** Доменные инварианты публикации возвращают 409 до того, как Application делает дорогую работу. Будущее расширение Builder (денормализация имён справочников, валидация полей) не превратит 409 в 500.
2. **Defense-in-depth.** Domain остаётся самодостаточным: любой вызов `Publish` напрямую (например, миграционные скрипты, фоновые задачи, будущие admin-сценарии) защищён теми же инвариантами, что и Application-флоу.
3. **Чистота Domain.** Никаких делегатов из Application, никаких упоминаний Application в `Domain.csproj`. Сигнатура `Publish` стабильна — существующие тесты и вызывающий код не требуют правки.
4. **Единый источник истины.** Helper `CheckPublishInvariants` гарантирует, что добавление нового инварианта (например, при появлении UC-DSH-009 SetDietLabels — проверка согласованности `DietLabelsMask` и состава) затрагивает ровно один метод и автоматически отражается и в pre-check, и в `Publish`.
5. **Дешёвая стоимость.** Pre-check — это 6 if-ов на загруженном агрегате; стоимость пренебрежимая по сравнению с Builder, репозиторным IO и сериализацией.
6. **Готовность к Query-применению.** При появлении первого Query-потребителя (DTO с флагом `CanPublish` или `PublishBlockReason` для UC-DSH-050 / UC-DSH-053) — метод уже есть и работает; видимость менять не требуется.

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- 409 возвращается до сборки снепшота — UI получает понятную бизнес-ошибку независимо от наполнения Builder.
- Builder выполняется лениво — экономия CPU и аллокаций при отказах публикации.
- Domain устойчив к расширению Builder: добавление денормализации справочных имён или валидаций не вернёт проблему «маскировки 409».
- При случайных рефакторингах внешний код не обойдёт доменные инварианты — `Publish` повторяет проверки как defense-in-depth.
- Появляется готовая точка для Query-DTO: флаг «можно ли опубликовать» рассчитывается тем же кодом, что и сама публикация.

### Negative / Trade-offs (Отрицательные / компромиссы)

- Дополнительный публичный метод на агрегате — расширение поверхности API.
- Два вызова `CheckPublishInvariants()` подряд в одном Handler-флоу (через `CheckCanPublish` и внутри `Publish`). Стоимость пренебрежимая, но архитектурно «лишний звонок».
- `CheckCanPublish` синтаксически доступен внешним сборкам, ссылающимся на `Dishes.Domain` (на момент принятия ADR — только `Dishes.Application` и `Dishes.Infrastructure`). Ограничение области применения выражается через XML-doc и code review.

### Areas of Caution (На что обратить внимание)

- При добавлении нового инварианта публикации — правка только `CheckPublishInvariants()`. Любое разветвление логики «проверка только в `Publish`, но не в `CheckCanPublish`» — признак ошибки и должно блокироваться на code review.
- При появлении админского `RebuildPublishedSnapshot` (Этап 8+) — его инварианты отличаются (не нужны `Status != Archived` пре-условия в той же форме, нужно `PublishedVersionData != null`). Не переиспользовать `CheckCanPublish` напрямую — завести отдельный pre-check.
- Тесты на Domain должны проверять, что `CheckCanPublish()` и `Publish()` возвращают идентичный набор `Result` для одного и того же состояния агрегата. При расхождении — нарушено приглашение к defense-in-depth.
- Сохраняется defense-in-depth и в `Publish`: повторный вызов helper-а не оптимизация, а гарантия. Не убирать его «потому что pre-check уже был».
- Риск NRE / `JsonException` из Builder при искусственном повреждении агрегата не покрывается этим ADR (этот класс ошибок остаётся 500 от `GlobalExceptionHandlingMiddleware`). Покрывается только маскировка штатных 409 за 500.

---

## 6. Future Scope (Будущие направления)

- **Этап 8+ — `RebuildPublishedSnapshot`.** При появлении админского механизма принудительной пересборки снепшота — отдельный pre-check метод (например, `CheckCanRebuildSnapshot`), потому что набор инвариантов другой (статусные проверки сужаются, требуется `PublishedVersionData != null`). Должен жить в той же паре «helper + публичный pre-check + основной метод».
- **Query-DTO с флагом `CanPublish` / `PublishBlockReason`.** При первой реальной потребности UI (UC-DSH-050 / UC-DSH-053 или отдельный UC «можно ли публиковать») — `CheckCanPublish` вызывается из Query-handler без побочных эффектов. Возможный формат поля: `string? PublishBlockCode` (код доменной ошибки, либо `null` если можно публиковать) — точная форма решается в момент появления потребности.
- **Расширение Builder на денормализацию имён справочников (Этап 4+).** При вынесении в snapshot имён `Category`, `Tag`, `Ingredient` — Builder начнёт делать репозиторные запросы и обрабатывать nullable-поля. Эта ситуация ровно та, для которой принят ADR-0015 — pre-check защитит от маскировки 409 при расширении.

---

## 7. Implementation Reference (Связь с кодовой базой)

### Реализовано

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Dish.cs`:
  - `public Result CheckCanPublish()` — публичный pure pre-check.
  - `private Result CheckPublishInvariants()` — общий helper, единый источник истины проверок.
  - `public Result Publish(DateTimeOffset utcNow, string snapshot)` — переиспользует helper первым шагом (defense-in-depth).
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Application/Commands/PublishDish/PublishDishCommandHandler.cs`:
  - Поток `Load → POL-001 → CheckCanPublish → Build(snapshot) → Publish` — вызов pre-check перед сборкой снепшота.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Errors/DishesErrors.cs` — без правок; набор ошибок остаётся тем же, что и в `Dish.Publish` до ADR.

### Планируется

- **Unit-тесты Dishes** — проверка идентичности `CheckCanPublish()` и первой ветки `Publish()` для всех сценариев отказа, а также чистоты pre-check (повторный вызов не меняет агрегат, не поднимает события). Покрытие модуля Dishes тестами — отложенная задача проекта.
- **Этап 8+ — `RebuildPublishedSnapshot`:** при реализации завести симметричный pre-check.

---

## История изменений

- **2026-06-06:** Accepted.
