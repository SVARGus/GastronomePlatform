# ADR-0019: Автоматическое обновление `Dish.UpdatedAt` через `SaveChangesInterceptor`

**Status:** Accepted
**Date:** 2026-05-23
**Updated:** 2026-08-02 — добавлено правило «приоритет домена» (§3.3): Interceptor не перетирает `UpdatedAt`, уже проставленный Domain-кодом в той же операции; финализация при переносе в `docs/public/adr/`
**Stage:** 2

---

## Related (Связи)

- **Связанные ADR:** [ADR-0006](./ADR-0006-dish-recipe-single-aggregate.md) (Dish + Recipe как один агрегат — Interceptor отслеживает все семь сущностей агрегата), [ADR-0011](./ADR-0011-two-layer-dish-storage.md) (Двухслойное хранение Dish — исключаемые поля определяются семантикой снепшота и денормализованных счётчиков; индикатор `UpdatedAt > PublishedAt`), [ADR-0002](./ADR-0002-clean-architecture.md) (Clean Architecture — Interceptor живёт в Infrastructure и не проникает в Domain)
- **Связанные модули:** Dishes
- **Связанная документация:** `docs/public/modules/dishes/domain-model.md` (автообновление UpdatedAt)

---

## 1. Context (Контекст)

Согласно ADR-0011, поле `Dish.UpdatedAt` служит источником правды для UI-индикатора «есть неопубликованные правки» (`UpdatedAt > PublishedAt`) и для инвалидации HTTP-кэшей. Оно должно обновляться при **любой** правке автора, затрагивающей содержимое агрегата.

Агрегат Dish (ADR-0006) объединяет семь сущностей: сам `Dish` плюс дочерние `Recipe`, `RecipeStep`, `RecipeIngredient`, `Timing`, `Yield`, `Nutrition`. Правка любой из них — это правка блюда. Однако модификация дочерней сущности через wrapper-методы Dish не гарантирована на 100%: возможны сценарии, когда сущность приходит в состоянии `Modified` в `ChangeTracker` иначе (прямая работа через EF из тестов, будущие интеграции).

Одновременно на Dish есть свойства, изменение которых **не должно** триггерить `UpdatedAt`:

- Денормализованные счётчики (`RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount`) — обновляются между публикациями по внешним событиям, не являются авторскими правками.
- Снепшотные поля (`PublishedVersionData`, `PublishedVersionUpdatedAt`) — обновляются самой публикацией, а не правкой.
- Само поле `UpdatedAt` — иначе Interceptor триггерил бы сам себя.

Нужен механизм, который автоматически обновляет `Dish.UpdatedAt` при любой правке любой сущности агрегата, но игнорирует изменения перечисленных исключений.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Только явный `MarkAsUpdated` в wrapper-методах

Полагаться исключительно на дисциплину: каждый wrapper-метод Dish и каждый Application Handler, изменяющий агрегат, обязан явно обновить `UpdatedAt`.

**Плюсы:** прозрачность — обновление видно в исходном коде метода. Нет «магии» на уровне инфраструктуры. Тестируется без EF Core.
**Минусы:** легко забыть при добавлении нового wrapper-метода. Не защищает от прямых модификаций через `ChangeTracker`. Единственная страховка — код-ревью.

**Причина отклонения:** ставит критический инвариант («UpdatedAt всегда актуально при правке») в зависимость от человеческой памяти. При росте кодовой базы риск разъезжания увеличивается монотонно.

### Вариант B — Триггер в БД

PostgreSQL-триггер на `UPDATE` дочерних таблиц, обновляющий `Dishes.UpdatedAt = NOW()`.

**Плюсы:** самый низкий уровень — защита работает независимо от источника `UPDATE` (EF Core, прямой SQL, скрипт миграции).
**Минусы:** логика инварианта уходит в БД — нарушение границ слоёв (ADR-0002). Список исключаемых полей пришлось бы дублировать в SQL. Триггер использует `NOW()`, а не время `IDateTimeProvider` — рассинхронизация с Application по таймзоне и мокированию в тестах. Отладка усложняется.

**Причина отклонения:** нарушает границы слоёв и создаёт вторую точку правды для правила, которое должно жить на уровне приложения.

### Вариант C — SaveChangesInterceptor с анализом ChangeTracker ⭐

EF Core `SaveChangesInterceptor` перехватывает `SaveChangesAsync` до фиксации. Анализирует `ChangeTracker.Entries()`, находит изменения в семи сущностях агрегата, определяет затронутые `Dish.Id`, устанавливает `UpdatedAt` через `Property(...).CurrentValue`.

**Плюсы:** живёт в Infrastructure модуля, работает автоматически для любого источника модификации, использует тот же `IDateTimeProvider`, что и Application, — время консистентно. Список исключаемых свойств — один статический `HashSet<string>` в одном месте.
**Минусы:** механизм скрыт от прямого чтения Domain-кода. Не покрывает M:M-связки (`DishCategory`, `DishTag`) — для них остаётся явный `MarkAsUpdated` (двойной механизм).

---

## 3. Decision (Принятое решение)

Реализован **`UpdatedAtInterceptor : SaveChangesInterceptor`** в Infrastructure модуля Dishes.

### 3.1. Отслеживаемые сущности

Семь типов — по составу агрегата: `Dish` (корень), `Recipe`, `RecipeStep`, `RecipeIngredient`, `Timing`, `Yield`, `Nutrition`. Для дочерних сущностей — свой case в `switch`, определяющий затронутый `Dish.Id` (через `Recipe.DishId`, для внучатых — через `RecipeId → Recipe.DishId` по `ChangeTracker`).

### 3.2. Исключаемые свойства Dish (7)

Приватное статическое поле `_excludedDishProperties : HashSet<string>`:

- `PublishedVersionData`, `PublishedVersionUpdatedAt` — снепшотные;
- `RatingAvg`, `RatingCount` — рейтинговые счётчики (Этап 5+);
- `ViewsCount`, `FavoritesCount` — счётчики взаимодействий;
- `UpdatedAt` — сам себя не триггерит.

Если `Dish` пришёл в `EntityState.Modified`, но все изменённые свойства — из этого списка, `UpdatedAt` не обновляется (`IsMeaningfulDishChange`).

`PublishedAt` и `Status` в исключения **не входят** — их меняют только lifecycle-методы, а для них работает правило §3.3.

### 3.3. Приоритет домена: Interceptor не перетирает `UpdatedAt`, проставленный Domain-кодом

Если в текущей операции `UpdatedAt` уже помечен изменённым (`Property(nameof(Dish.UpdatedAt)).IsModified == true` — wrapper-метод или lifecycle-метод присвоил значение), Interceptor **пропускает** запись.

Правило добавлено по итогам реального инцидента (2026-08-01): `Dish.Publish` присваивает `PublishedAt = UpdatedAt = utcNow` одним значением, но Interceptor видел «значимые» изменения Dish (`Status`, `PublishedAt`) и перезаписывал `UpdatedAt` собственным `_dateTimeProvider.UtcNow` — на доли миллисекунды **позже**. Итог: `UpdatedAt > PublishedAt` сразу после каждой публикации → ложный индикатор «есть неопубликованные правки» у всех опубликованных блюд. Interceptor — defensive-слой для модификаций **мимо** Domain-методов; когда Domain высказался сам, инфраструктура не корректирует его значение.

### 3.4. Skip Added/Deleted

`Added` — создание нового Dish, `UpdatedAt` уже задан конструктором; `Deleted` — обновлять удаляемую строку бессмысленно.

### 3.5. Поиск родительского Dish через ChangeTracker

Для `RecipeStep`/`RecipeIngredient`/`Timing`/`Yield` — поиск `Recipe` с совпадающим `Id == entity.RecipeId` в `ChangeTracker.Entries<Recipe>()`, затем `recipe.DishId`; для `Nutrition` — поиск Recipe-владельца по `NutritionId`. **Никаких обращений к БД.** Требование: агрегат загружен в `ChangeTracker` целиком (Handler делает `GetByIdWithFullRecipeAsync` и модифицирует загруженный объект).

### 3.6. Двойной механизм: Interceptor + явный Domain-код

M:M-связки (`DishCategory`, `DishTag`) Interceptor не отслеживает — wrapper-методы `SetCategories`/`SetTags` вызывают `MarkAsUpdated(utcNow)` явно. Остальные wrapper-методы (`UpdateCard`, `UpdateRecipe`, `AddRecipeStep`, `ChangeMainImage`, `UpdateHistory`, ...) присваивают `UpdatedAt = utcNow` напрямую и поднимают `DishUpdatedEvent` — Interceptor для них лишь страховка (и по §3.3 не вмешивается).

### 3.7. Регистрация

`UpdatedAtInterceptor` — **Singleton** (без per-request состояния, зависит только от Singleton `IDateTimeProvider`); подключение через `options.AddInterceptors(sp.GetRequiredService<UpdatedAtInterceptor>())` в `AddDbContext<DishesDbContext>((sp, options) => ...)`.

---

## 4. Rationale (Обоснование)

1. **Инвариант защищён без нагрузки на разработчика.** Правило «правка агрегата → обновление UpdatedAt» реализовано один раз и работает для всех сценариев модификации.
2. **Список исключений в одном месте.** `HashSet<string>` в одном файле; новый счётчик или снепшотное поле — правка одной строки.
3. **Совместимость с Clean Architecture.** Interceptor живёт в Infrastructure, Domain о нём не знает. При выделении Dishes в микросервис переезжает вместе с Infrastructure.
4. **Чёткое разделение ролей (после §3.3).** Domain-методы — источник правды для `UpdatedAt` в своих операциях; Interceptor — только страховка для модификаций мимо Domain. До этого правила роли конфликтовали, что и породило инцидент с ложным индикатором.
5. **Двойной механизм принят сознательно.** Научить Interceptor отслеживать M:M — усложнение без выгоды; явный вызов в двух методах читается яснее.

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- `Dish.UpdatedAt` корректен после `SaveChangesAsync` при любом источнике модификации — индикатор «есть неопубликованные правки» надёжен.
- Прямые модификации через EF (тесты, будущие сценарии) корректно обновляют Dish без ручной работы.
- Список исключаемых свойств кодифицирован и виден при ревью в одном файле.

### Negative / Trade-offs (Отрицательные / компромиссы)

- «Магия» на уровне инфраструктуры — при отладке нужно знать про Interceptor.
- Двойной механизм — концептуальная нагрузка на нового разработчика.
- Скрытые взаимодействия Interceptor ↔ Domain-методы способны порождать трудноуловимые расхождения меток: инцидент с ложным `hasUnsavedChanges` (см. §3.3) прожил в системе с момента реализации публикации до живого тестирования веб-интерфейса.
- Unit-тесты Domain без EF не видят эффект Interceptor — тестируют присвоения Domain-методов напрямую.

### Areas of Caution (На что обратить внимание)

- **Interceptor никогда не перетирает `UpdatedAt`, уже изменённый в операции** (§3.3). При рефакторинге Interceptor это правило — первый инвариант для сохранения.
- **Новое поле в Dish** — определить тип: счётчик/снепшотное → в `_excludedDishProperties`; авторская правка → не добавлять (по умолчанию триггерит `UpdatedAt`).
- **Новая сущность в агрегате** (например, будущий `RecipeVariant`) — добавить case в `switch`, иначе её правки не обновят `UpdatedAt`.
- **Новые M:M-коллекции на Dish** — явный `MarkAsUpdated` в setter-методе.
- **Interceptor выполняется после `DetectChanges`.** При будущей оптимизации `AutoDetectChangesEnabled = false` потребуется явный `DetectChanges()`.

---

## 6. Future Scope (Будущие направления)

- **Этап 8+ (`DishSnapshotInvalidation`).** Фоновая пересборка снепшотов обновляет только `PublishedVersionData` и `PublishedVersionUpdatedAt` — оба в исключениях, Interceptor корректно проигнорирует.
- **При росте числа сущностей** — registry-паттерн (`IReadOnlyDictionary<Type, Func<EntityEntry, Guid?>>`); пока `switch` на семь типов читаемее.
- **При появлении других агрегатов с той же семантикой** (например, `Order.UpdatedAt`) — базовый класс `AggregateUpdatedAtInterceptor<TRoot>`; пока Dish единственный, обобщать преждевременно.

---

## 7. Implementation Reference (Связь с кодовой базой)

- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Persistence/Interceptors/UpdatedAtInterceptor.cs` — реализация целиком: `_excludedDishProperties` (7 элементов); override `SavingChanges`/`SavingChangesAsync` → приватный `ApplyUpdatedAt(DbContext)`; `switch` на семь типов; `IsMeaningfulDishChange(EntityEntry)`; `TryAddDishIdFromRecipeId(...)`, `TryAddDishIdFromNutritionOwner(...)`; во втором проходе — проверка `updatedAtProperty.IsModified` (приоритет домена, §3.3).
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — `AddSingleton<UpdatedAtInterceptor>()`; `AddDbContext<DishesDbContext>((sp, options) => { options.UseNpgsql(...); options.AddInterceptors(sp.GetRequiredService<UpdatedAtInterceptor>()); })`.
- `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Entities/Dish.cs` — `MarkAsUpdated(DateTimeOffset utcNow)` (присваивает `UpdatedAt`, поднимает `DishUpdatedEvent`; вызывается `SetCategories`/`SetTags`); wrapper-методы присваивают `UpdatedAt` напрямую.
- `src/Common/GastronomePlatform.Common.Application/Abstractions/IDateTimeProvider.cs` — зависимость Interceptor.

---

## История изменений

- **2026-05-23:** Accepted. Interceptor реализован вместе с добавлением M:M-таблиц агрегата (миграция `20260523075145_AddDishMmTables`).
- **2026-08-01:** Добавлено правило «приоритет домена» (§3.3) по итогам инцидента с ложным индикатором «есть неопубликованные правки»: Interceptor перетирал `UpdatedAt`, присвоенный `Dish.Publish`, значением на доли миллисекунды позже `PublishedAt`. Фикс — пропуск Dish с `UpdatedAt.IsModified == true`.
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`, присвоен номер ADR-0019.
