# Модуль Dishes — Доменная модель (Этап 2)

> **Статус:** Проектирование
> **Этап дорожной карты:** 2 (Контент и медиа)
> **Дата:** 2026-04-19 (создано), 2026-04-26 (расширено)
> **Связанные документы:** [[05_Дорожная-карта]], [[02_Архитектура]], [[13_Структура-проекта]], [[08_Разработка-(Development-Guide)]], `use-cases-dishes-draft.md`, `POL-001-dish-ownership.md`
>
> **Изменения от 2026-04-26:**
> - Введена двухслойная модель данных: основные таблицы + снепшот `PublishedVersionData (jsonb)` для отдачи посетителям. Добавлен новый концептуальный раздел.
> - Введены `*Published`-таблицы (`DishCategoryPublished`, `DishTagPublished`) для каталожного фильтра по опубликованной версии.
> - Уточнена семантика временных меток на агрегате `Dish` — `CreatedAt`, `UpdatedAt`, `PublishedAt`, `PublishedVersionUpdatedAt`. Добавлен новый концептуальный раздел.
> - Добавлены поля `PublishedVersionData jsonb`, `PublishedVersionUpdatedAt timestamptz` в `Dish`.
> - Добавлены ссылки на `POL-001` для UC модификации блюда.
> - В Deferred добавлены: `DishOwnership` (Этап 4+), `DishVersion`, `DishSnapshotInvalidation` (оба — Этап 8+).
> - Добавлены TODO в коде про `MarkAsUpdated()` для UC-DSH-007/008.

---

## Содержание

1. [Ключевые архитектурные решения](#1-ключевые-архитектурные-решения)
2. [Стратегия реализации](#2-стратегия-реализации)
3. [Сводная таблица сущностей](#3-сводная-таблица-сущностей)
4. [Двухслойная модель: основные таблицы и снепшот](#4-двухслойная-модель-основные-таблицы-и-снепшот)
5. [Четыре временные метки агрегата Dish](#5-четыре-временные-метки-агрегата-dish)
6. [Enums](#6-enums)
7. [Core-сущности — детальный разбор](#7-core-сущности--детальный-разбор)
8. [Stub-сущности](#8-stub-сущности)
9. [Deferred-сущности (на будущие этапы)](#9-deferred-сущности-на-будущие-этапы)
10. [Заметки на будущее (TODO в коде)](#10-заметки-на-будущее-todo-в-коде)
11. [Seed-данные — план](#11-seed-данные--план)
12. [Что дальше](#12-что-дальше)

---

## 1. Ключевые архитектурные решения

Ниже зафиксированы все развилки, принятые на этапе проектирования модуля `Dishes`.

| № | Вопрос | Решение |
|---|--------|---------|
| 1 | Структура Dish/Recipe | **Один агрегат** — `Dish` является корнем агрегата, `Recipe` — его внутренняя часть (1:1) |
| 2 | Категории — плоский список или иерархия | **Иерархия** через `ParentId` (ограничение — 3 уровня) |
| 3 | Ингредиенты — справочник или свободный ввод | **Гибрид**: либо `IngredientId` из справочника, либо `FreeformText`. Domain-инвариант: заполнено одно из двух |
| 4 | Модерация на Этапе 2 | `ModerationStatus` с дефолтом `Approved`. Реальная модерация — Этап 8 |
| 5 | Хранение локальных файлов (wwwroot vs контроллер) | *Решение отложено до обсуждения модуля Media* |
| 6 | Проверка владельца медиа | *Решение отложено до обсуждения модуля Media* |
| 7 | `DifficultyLevel` / `CostEstimate` | **Enum** в коде, не таблицы-справочники |
| 8 | Статусы: одна колонка или две | **Две колонки** — `Status` (что хочет автор) и `ModerationStatus` (что решил админ) |
| 9 | Category vs UserCollection | `Category` — Этап 2. `UserCollection` — Этап 5 |
| 10 | Slug | Добавлен в `Dish` и `Category`. В `Tag` — не нужен |
| 11 | `IngredientSpec` — полная реализация или stub | **Stub**: таблица создаётся, но на Этапе 2 используется минимально. Полноценное расширение — Этап 8 |
| — | `DietLabelsMask` в `Dish` | **Добавить сразу** — ценность для фильтрации высокая, стоимость минимальная |
| — | `HistoryText` в `Dish` | Пока поле в `Dish`, с пометкой на будущий вынос в отдельную таблицу `DishHistory` |
| — | `Notes` в `Recipe` | **Добавить** — свободное поле для дополнительных комментариев автора, отличается от `AuthorTips` / `ServingSuggestions` / `IntroductionText` отсутствием заданной семантики |
| — | Иконка `Category` — `IconUrl` или `IconMediaId` | **`IconMediaId`** — единообразие с остальными файлами платформы. При проектировании Media учесть случай системного владельца (иконки не принадлежат пользователю) |
| — | `Description` и `ImageMediaId` в `Ingredient` | **Добавить сейчас прямо в `Ingredient`** (Путь А). Выделение в отдельную сущность `IngredientDetails` — Этап 8+, когда добавятся расширенные поля (HistoryText, SubstitutionTips, SeasonalityInfo) |
| — | Версионирование `Dish` | **Снепшот публичной версии** в jsonb-поле `Dish.PublishedVersionData`. Правки автора — в основных таблицах. Снепшот пересобирается при публикации (UC-DSH-004). История версий (`DishVersion`) — Deferred (Этап 4+ или 8+) |
| — | Каталожный фильтр по опубликованным связям | **Отдельные `*Published`-таблицы** (`DishCategoryPublished`, `DishTagPublished`) — заполняются при публикации синхронно с снепшотом. Связи рабочей версии остаются в обычных `DishCategory` / `DishTag` |
| — | Поле для HTTP-кэша | **`Dish.PublishedVersionUpdatedAt` (timestamptz NULL)** — момент последнего обновления снепшота. Используется в HTTP-заголовке `Last-Modified` и в журнале invalidation (Этап 8+) |
| — | Авторизация модификаций | **Политика `POL-001 Dish Ownership Policy`** — единый источник правил «кто может изменять блюдо». UC модификации ссылаются на политику, а не дублируют правила. См. `POL-001-dish-ownership.md` |
| — | Лицензионная модель пользовательского контента | **Неисключительная безотзывная лицензия** (стандарт индустрии). Снятие с публикации не отзывает лицензию. Удаление аккаунта не равно отзыв контента. Подробности — в `use-cases-dishes-draft.md` (концептуальный раздел) и в будущей оферте платформы |
| — | Передача прав платформе (PlatformOwnership) | Отдельная таблица `DishOwnership` — Deferred (Этап 4+). Используется для блюд, переданных платформе по результатам конкурсов и т.п. По умолчанию (отсутствие записи) — блюдо `AuthorOwned` |

---

## 2. Стратегия реализации

Все сущности модуля разложены по трём уровням готовности:

| Статус | Что делаем сейчас (Этап 2) |
|--------|---------------------------|
| 🟢 **Core** | Полная реализация: Domain-модель с инвариантами, EF-конфигурация, репозитории, use cases, контроллер, unit-тесты |
| 🟡 **Stub** | Таблица создаётся (чтобы не делать миграцию потом), базовая доменная модель без развитой логики. Минимальный API или вообще без API. Seed-данные — 0-2 примера |
| ⚪ **Deferred** | Не трогаем сейчас. В коде оставляется комментарий `// TODO: <Entity> — Этап N` рядом с местами, где эта сущность появится. В документе перечислены, чтобы видеть полную картину |

**Принцип отнесения к категории:** если добавление поля/таблицы позже потребует миграции данных — заложить сейчас (хотя бы Stub). Если позже будет чистая добавочная миграция — откладываем в Deferred.

---

## 3. Сводная таблица сущностей

**Легенда связей:**
- `A → B (1:1)` — один к одному, FK у стороны `A`
- `A → B (M:1)` — многие к одному, FK у стороны `A` (обычная ссылка)
- `A ← B (1:M)` — один ко многим, FK у стороны `B`
- `A ↔ B (M:M)` — многие ко многим, через связующую таблицу
- `self` — self-reference (ссылка на ту же таблицу)

| № | Русское имя | Имя в проекте | Статус | Описание | Ключевые поля | Связи |
|---|-------------|---------------|--------|----------|---------------|-------|
| 1 | Блюдо | `Dish` | 🟢 | Корень агрегата. Публичная карточка блюда | Id, AuthorUserId, Name, Slug, Description, MainImageId, Status, ModerationStatus, DifficultyLevel, CostEstimate, OwnerType, DietLabelsMask, HistoryText, RatingAvg, RatingCount, ViewsCount, **PublishedVersionData**, **PublishedVersionUpdatedAt**, PublishedAt, CreatedAt, UpdatedAt | `Dish → Recipe (1:1)`; `Dish ↔ Category (M:M)`; `Dish ↔ Tag (M:M)`; `Dish ↔ Category (M:M, опубликованные)` через `DishCategoryPublished`; `Dish ↔ Tag (M:M, опубликованные)` через `DishTagPublished`; `Dish → MediaFile (через MainImageId, кросс-модульно, без FK на уровне БД)` |
| 2 | Рецепт | `Recipe` | 🟢 | Инструкция приготовления. Часть агрегата `Dish` | Id, DishId, IntroductionText, ServingsDefault, IsAlcoholic, AllergensMask, AuthorTips, ServingSuggestions, Notes | `Recipe → Dish (1:1)`; `Recipe → Timing (1:1)`; `Recipe → Yield (1:1)`; `Recipe → Nutrition (1:1)`; `Recipe ← RecipeStep (1:M)`; `Recipe ← RecipeIngredient (1:M)` |
| 3 | Шаг рецепта | `RecipeStep` | 🟢 | Один шаг приготовления | Id, RecipeId, Order, Title, Description, ImageMediaId, VideoUrl, TemperatureCelsius, TimerMinutes | `RecipeStep → Recipe (M:1)` |
| 4 | Ингредиент в рецепте | `RecipeIngredient` | 🟢 | Позиция в списке ингредиентов конкретного рецепта | Id, RecipeId, IngredientId, IngredientSpecId, FreeformText, Quantity, MeasureUnitId, Order, IsOptional, PreparationNote | `RecipeIngredient → Recipe (M:1)`; `RecipeIngredient → Ingredient (M:1, nullable)`; `RecipeIngredient → IngredientSpec (M:1, nullable)`; `RecipeIngredient → MeasureUnit (M:1)` |
| 5 | Категория | `Category` | 🟢 | Справочник каталога. Иерархия | Id, Name, Slug, ParentId, Order, IconMediaId, IsActive | `Category → Category (self, ParentId)`; `Category ↔ Dish (M:M)`; `Category → MediaFile (через IconMediaId, кросс-модульно, без FK)` |
| 6 | Связь Блюдо ↔ Категория | `DishCategory` | 🟢 | Связующая таблица M:M (рабочая версия) | DishId, CategoryId | — (связующая) |
| 6.1 | Связь Блюдо ↔ Категория (опубликованная) | `DishCategoryPublished` | 🟢 | Связующая таблица M:M для опубликованных версий блюд. Заполняется при `UC-DSH-004 PublishDish` | DishId, CategoryId | — (связующая) |
| 7 | Тег | `Tag` | 🟢 | Плоский список пользовательских меток | Id, Name, NormalizedName, UsageCount, IsVerified, CreatedByUserId | `Tag ↔ Dish (M:M)` |
| 8 | Связь Блюдо ↔ Тег | `DishTag` | 🟢 | Связующая таблица M:M (рабочая версия) | DishId, TagId | — |
| 8.1 | Связь Блюдо ↔ Тег (опубликованная) | `DishTagPublished` | 🟢 | Связующая таблица M:M для опубликованных версий блюд. Заполняется при `UC-DSH-004 PublishDish` | DishId, TagId | — (связующая) |
| 9 | Ингредиент (справочник) | `Ingredient` | 🟢 | Глобальный справочник продуктов | Id, Name, PluralName, Description, ImageMediaId, IsLiquid, DensityApprox, IsAllergen, AllergenType, BaseMeasureUnitId, DefaultNutritionId, IsActive | `Ingredient → MeasureUnit (M:1)`; `Ingredient → Nutrition (1:1, nullable)`; `Ingredient ← IngredientSpec (1:M)`; `Ingredient → MediaFile (через ImageMediaId, кросс-модульно, без FK)` |
| 10 | Сорт/уточнение ингредиента | `IngredientSpec` | 🟡 | Разновидность продукта (жирность, сорт) | Id, IngredientId, SpecName, NutritionId | `IngredientSpec → Ingredient (M:1)`; `IngredientSpec → Nutrition (1:1)` |
| 11 | Единица измерения | `MeasureUnit` | 🟢 | Справочник единиц + коэффициенты конвертации | Id, Code, NameRu, Type, ConversionToBase, IsBase | — |
| 12 | Тайминг рецепта | `Timing` | 🟢 | Времена этапов приготовления | Id, PrepTimeMinutes, CookTimeMinutes, RestTimeMinutes, ActiveTimeMinutes, TotalTimeMinutes, IsTotalManual | — (owned by `Recipe`) |
| 13 | Выход продукции | `Yield` | 🟢 | Сколько получается и размер порции | Id, QuantityTotal, YieldUnit, ServingsCount, GramsPerServing | — (owned by `Recipe`) |
| 14 | КБЖУ | `Nutrition` | 🟢 | Пищевая ценность | Id, CalcMethod, Calories, Proteins, Fats, SaturatedFats, Carbs, Sugar, Fiber, Salt | — (используется `Recipe`, `Ingredient`, `IngredientSpec`) |
| 15 | Группа взаимозаменяемости | `IngredientGroup` | ⚪ | «Лук красный ИЛИ белый» в рамках рецепта | — | Этап 8+ |
| 16 | Инвентарь | `Equipment` | ⚪ | Справочник оборудования | — | Этап 8+ |
| 17 | Коэффициент потерь | `CookingLossCoefficient` | ⚪ | Уварка/ужарка для точного КБЖУ | — | Этап 8+ |
| 18 | История блюда | `DishHistory` | ⚪ | Культурно-историческое описание | — | Этап 8+ (пока `HistoryText` в `Dish`) |
| 19 | Пользовательская подборка | `UserCollection` | ⚪ | Личные коллекции пользователя | — | Этап 5 |
| 20 | Бракераж | `ServiceSpec` | ⚪ | Температура подачи, срок годности | — | Этап 8+ |

**Итого на Этапе 2:** 13 Core-таблиц + 1 Stub-таблица + 4 связующие (`DishCategory`, `DishTag`, `DishCategoryPublished`, `DishTagPublished`) = **18 таблиц в схеме `dishes`**.

---

## 4. Двухслойная модель: основные таблицы и снепшот

В Dishes используется **сознательное дублирование данных** между двумя источниками — это ключевое архитектурное решение модуля.

### 4.1. Назначение каждого источника

| Источник | Назначение | Содержимое |
|----------|-----------|------------|
| **Основные таблицы** (`Dish`, `Recipe`, `RecipeStep`, `RecipeIngredient`, `DishCategory`, `DishTag` и пр.) | Источник правды для запросов, фильтрации, сортировки, JOIN. Рабочая версия автора | Все поля, по которым ведётся фильтр / сортировка / связь |
| **`Dish.PublishedVersionData (jsonb)`** | Готовая «карточка для отдачи клиенту» — снепшот публичной версии | Все поля, которые нужно показать в карточке или рецепте, в виде nested JSON |
| **`*Published`-таблицы** (`DishCategoryPublished`, `DishTagPublished`) | Опубликованные M:M связи — для каталожного фильтра по тегам и категориям | Только записи опубликованных версий блюд; обновляются синхронно при `UC-DSH-004 PublishDish` |

### 4.2. Правило проектирования полей

- Поле, по которому **есть фильтр, сортировка или JOIN** → обязательно в основной таблице (или в отдельной `*Published`-таблице, если фильтр идёт по опубликованной версии).
- Поле, которое **только отдаётся клиенту** в карточке/рецепте → может жить только в снепшоте.
- Поле, которое нужно и для фильтра, и для отдачи (Name, MainImageId, DifficultyLevel, …) → дублируется в обоих источниках.
- **Динамические счётчики** (`RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount`) — обновляются часто (от событий между публикациями), хранятся **только** в основных колонках. Включать их в снепшот бессмысленно — пришлось бы переписывать jsonb на каждое изменение.

### 4.3. Пример SQL-запроса каталога

```sql
-- UC-DSH-054 SearchDishes: каталог с фильтрами и сортировкой
SELECT
    d.id,
    d.slug,
    d.published_version_data->'name' AS name,                    -- из снепшота
    d.published_version_data->'main_image_id' AS main_image_id,
    d.rating_avg,                                                 -- из основной таблицы
    d.views_count
FROM dishes.dish d
INNER JOIN dishes.dish_category_published dcp ON dcp.dish_id = d.id   -- *Published-таблица
WHERE d.published_version_data IS NOT NULL                            -- опубликовано
  AND d.moderation_status = 0                                          -- Approved
  AND (d.diet_labels_mask & 1) = 1                                     -- фильтр по биту в основной колонке
  AND dcp.category_id = @categoryId
  AND d.rating_avg >= 4.0
ORDER BY d.views_count DESC                                            -- сортировка по обычному индексу
LIMIT 20;
```

Что тут происходит:
- `WHERE` использует **обычные колонки и обычные таблицы** — все B-tree индексы работают.
- `SELECT` достаёт нужные поля через jsonb-оператор `->`.
- Никаких `Include` через рецепт, шаги, ингредиенты — для каталога они не нужны.

### 4.4. Когда автор правит блюдо

При правке (UC-DSH-002, 003, 007, 008, 020-033, 040-042):
- Изменения попадают **только** в основные таблицы и связующие (`DishCategory`/`DishTag`).
- Снепшот `PublishedVersionData` не трогается, посетители видят старую версию.
- `*Published`-таблицы тоже не трогаются.
- При публикации (`UC-DSH-004`) — снепшот пересобирается, `*Published`-таблицы переписываются. Всё атомарно.

### 4.5. Каскадные обновления (Этап 8+)

Некоторые админские операции изменяют связанные данные, которые попадают в снепшоты опубликованных блюд:

| Операция | Что меняется | Что устаревает в снепшотах |
|----------|--------------|----------------------------|
| Объединение тегов (UC-DSH-132) | `Tag.NormalizedName`, связи `DishTag`/`DishTagPublished` | Имена тегов в `tags[]` снепшота |
| Переименование категории (UC-DSH-102) | `Category.Name` | Имя категории в `categories[]` снепшота |
| Hard delete тега (UC-DSH-131) | Связи `DishTag`, `DishTagPublished` | Лишний тег в `tags[]` снепшота |
| Деактивация ингредиента (UC-DSH-112) | `Ingredient.IsActive` | Описание ингредиента в `ingredients[]` снепшота |

**Решение** (Этап 8+): фоновая задача `RecalculatePublishedSnapshots` пересобирает снепшоты по журналу `DishSnapshotInvalidation`. Реляционные связи (включая `*Published`-таблицы) обновляются **синхронно в транзакции админской операции** — фоновая задача только дособирает jsonb-снепшот.

На Этапе 2 эта проблема не возникает — соответствующие UC помечены как Drafted на Этап 8+.

### 4.6. Аргументация выбора архитектуры

**Почему не GIN-индекс на jsonb?** Запросы по jsonb-полям с GIN-индексом медленнее B-tree в 3–5 раз. Сортировка по jsonb-полям не использует обычные B-tree индексы. EF Core с jsonb-операторами дружит хуже, чем с реляционными связями.

**Почему не «всё в jsonb», убрав нормализованные таблицы?** Связи M:M (категории, теги) всё равно нужны в реляционной форме — иначе фильтр «блюда категории X» становится full scan по всем jsonb. И теряется возможность каскадных обновлений по справочникам.

**Почему не «всё в нормализованных таблицах», убрав снепшот?** При просмотре карточки или рецепта нужно делать 5+ JOIN'ов с десятками строк (шаги, ингредиенты). Снепшот — это денормализация для быстрой отдачи.

Гибрид «реляционные таблицы для запросов + jsonb-снепшот для отдачи» — компромисс, оптимизированный под профиль кулинарной платформы (много читателей, мало писателей).

### 4.7. Возможный переход к другой схеме (Этап 8+)

При значительном росте админской активности по справочникам или при появлении сложных конкурентных правок одного блюда несколькими ролями (автор + ассистент + модератор) — может потребоваться введение поля `Dish.DraftVersionData jsonb` для рабочей версии. Тогда модель становится трёхслойной: рабочая версия в jsonb автора, опубликованная версия в jsonb для посетителей, реляционные связи для запросов.

Это — кандидат на отдельный ADR в момент, когда триггеры для перехода реально появятся. На Этапе 2 трогать не нужно.

---

## 5. Четыре временные метки агрегата `Dish`

В агрегате `Dish` используются **четыре поля с временем** — у каждого свой смысл, их нельзя смешивать.

### 5.1. Сводная таблица меток

| Поле | Что отражает | Когда обновляется | Использование |
|------|--------------|---------------------|---------------|
| `Dish.CreatedAt` | Время создания блюда | Один раз, при `UC-DSH-001 CreateDish`. Иммутабельно | UI «создано N марта 2024»; UI «опубликовано» (в простом случае берём `PublishedAt`, но если нужна метка «когда блюдо вообще появилось» — это `CreatedAt`) |
| `Dish.UpdatedAt` | Последнее изменение **данных блюда автором** | Через `SaveChangesInterceptor` при изменении полей самого блюда / рецепта (см. ниже семь сущностей). Дополнительно — через явный `MarkAsUpdated()` при правке тегов и категорий автором (UC-DSH-007, UC-DSH-008). **НЕ** обновляется при каскадных обновлениях снепшота админом и при изменении `*Published`-таблиц | Индикатор несохранённых правок: `UpdatedAt > PublishedAt` |
| `Dish.PublishedAt` | Когда автор последний раз нажал «Опубликовать» | При **каждом** успешном `UC-DSH-004` (первая публикация, повторная, возврат с Unpublished). Обнуляется при `UC-DSH-005 Unpublish` и `UC-DSH-006 Archive` | Индикатор; UI «последняя публикация» |
| `Dish.PublishedVersionUpdatedAt` | Когда снепшот `PublishedVersionData` последний раз обновлялся | (а) при `UC-DSH-004` — переиздание автором; (б) при каскадных обновлениях снепшота — переименование категории, объединение тегов и т.п. Обнуляется при `Unpublish`/`Archive` | HTTP-заголовок `Last-Modified` посетителю; журнал `DishSnapshotInvalidation` (Этап 8+) |

### 5.2. Индикатор «есть несохранённые правки»

`Dish.UpdatedAt > Dish.PublishedAt`. Работает корректно потому что:

- При **любой** правке автором (поля блюда, рецепта, шагов, ингредиентов, тегов, категорий) `UpdatedAt` сдвигается в `now()` — либо автоматически через `SaveChangesInterceptor`, либо через явный `MarkAsUpdated()` для случая тегов/категорий.
- Каскадное обновление снепшота админом **не трогает `UpdatedAt`** — оно меняет только `PublishedVersionData` и `PublishedVersionUpdatedAt`. С точки зрения автора ничего не правилось — индикатор молчит.
- При повторной публикации автором `PublishedAt` обновляется в `now()`, индикатор корректно сбрасывается.

> **Не путать `PublishedAt` (момент намерения автора — «нажал Опубликовать») с `PublishedVersionUpdatedAt` (момент реального обновления снепшота — может меняться и по причинам, не зависящим от автора, например при каскадном обновлении тегов).**

### 5.3. Технические детали `UpdatedAt`: автоматический Interceptor + явный `MarkAsUpdated`

**Способ 1. Автоматически через `SaveChangesInterceptor`.**

EF Core Interceptor реагирует на изменения **семи сущностей**, которые относятся к «данным самого блюда»:

- `Dish` (но без полей `PublishedVersionData`, `PublishedVersionUpdatedAt`, `RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount` — они меняются по другим причинам)
- `Recipe`, `RecipeStep`, `RecipeIngredient`
- `Timing`, `Yield`, `Nutrition`

Покрывает UC-DSH-002, 003, 009, 010, 020-023, 030-033, 040-042 — все они меняют поля одной из этих сущностей, и `UpdatedAt` обновится без явных усилий.

**Способ 2. Явно через `MarkAsUpdated()` в Domain-методе.**

Для UC, которые меняют **связующие таблицы M:M** (`DishCategory`, `DishTag`), Interceptor по умолчанию не сработает — он не отслеживает связующие сущности. Но с точки зрения автора изменение тегов или категорий блюда — это **изменение блюда**, индикатор «есть несохранённые правки» должен сработать.

Решение: в Domain-методах `Dish.SetCategories(...)` и `Dish.SetTags(...)` — явный вызов `MarkAsUpdated()` (внутренний метод агрегата, ставит `UpdatedAt = clock.UtcNow`).

Покрывает UC-DSH-007 (категории) и UC-DSH-008 (теги). Других подобных UC в модуле нет.

**Что НЕ должно инкрементировать `UpdatedAt`:**

- Изменения `*Published`-таблиц (`DishCategoryPublished`, `DishTagPublished`) — это техническая денормализация, к рабочей версии не относится.
- Каскадное обновление снепшота админской операцией.
- Обновление денормализованных счётчиков (`RatingAvg`, `ViewsCount` и т.п.).
- Обновление `PublishedVersionData` без участия автора.

### 5.4. HTTP-кэширование (`Last-Modified`)

Для заголовка `Last-Modified` в ответе на UC-DSH-050 / UC-DSH-051 / UC-DSH-052 используется **`Dish.PublishedVersionUpdatedAt`** — он точно отражает момент последнего изменения отдаваемых посетителю данных.

Не использовать `UpdatedAt` для кэша — иначе правки автора в рабочую версию приведут к лишней инвалидации кэша у посетителей (для которых ничего не изменилось, они видят неизменный снепшот).

### 5.5. Разделение синхронных правок и асинхронной пересборки снепшотов (Этап 8+)

Когда админ выполняет операцию, затрагивающую справочник (объединение тегов UC-DSH-132, переименование категории UC-DSH-102 и т.п.), работа делится на **два этапа**:

**Этап A — синхронная транзакция админской операции:**

Внутри транзакции UC переписываются **все реляционные связи** в обеих таблицах: основной (`DishTag`/`DishCategory`) и опубликованной (`DishTagPublished`/`DishCategoryPublished`). Это гарантирует, что после завершения транзакции:

- Каталожный фильтр (UC-DSH-054) сразу работает корректно — посетитель не увидит ссылок на удалённый/переименованный тег.
- Авторские правки последующих UC-DSH-008 сохранят актуальные связи.
- Обе версии связей синхронизированы.

В журнал `DishSnapshotInvalidation` ставятся записи для всех затронутых блюд.

**Этап B — асинхронная фоновая задача `RecalculatePublishedSnapshots`:**

Каждые 5–10 минут забирает записи из журнала и **пересобирает только jsonb-снепшот** (`PublishedVersionData`), плюс обновляет `PublishedVersionUpdatedAt`. Реляционные связи на этом этапе **не трогаются** — они уже актуальны после этапа A.

**Что НЕ меняется на этапе B:** `UpdatedAt`, `PublishedAt`, основные `DishCategory`/`DishTag`, `*Published`-таблицы.

> **Зачем такое разделение:** реляционные связи нужны мгновенно (фильтр каталога не должен ссылаться на несуществующие записи), а пересборка снепшота — операция дорогая (десериализация-сборка-сериализация jsonb для каждого затронутого блюда), её можно отложить на батчевую обработку.

---

## 6. Enums

Все enums размещаются в `Dishes.Domain/Enums/`. Маркированы хранимыми значениями для EF Core (`int` в БД).

### 6.1. DishStatus

Жизненный цикл блюда с точки зрения автора.

| Значение | int | Описание |
|----------|-----|----------|
| `Draft` | 0 | Черновик. Виден только автору |
| `Published` | 1 | Опубликовано, виден всем |
| `Unpublished` | 2 | Снят с публикации автором (можно вернуть в `Published`) |
| `Archived` | 3 | Мягкое удаление |

### 6.2. ModerationStatus

Результат модерации. На Этапе 2 — дефолт `Approved`.

| Значение | int | Описание |
|----------|-----|----------|
| `Approved` | 0 | Одобрено (дефолт на Этапе 2) |
| `Pending` | 1 | На модерации |
| `Rejected` | 2 | Отклонено админом |
| `Flagged` | 3 | Жалобы от пользователей, требует пересмотра |

### 6.3. DifficultyLevel

| Значение | int | Локализация (ru) |
|----------|-----|------------------|
| `Easy` | 0 | Легко |
| `Medium` | 1 | Средне |
| `Hard` | 2 | Сложно |
| `Pro` | 3 | Профессиональный уровень |

### 6.4. CostEstimate

| Значение | int | Локализация (ru) |
|----------|-----|------------------|
| `Budget` | 0 | Бюджетное |
| `Moderate` | 1 | Умеренное |
| `Expensive` | 2 | Дорогое |

### 6.5. OwnerType

Тип автора-владельца блюда. Денормализуется из ролей пользователя на момент публикации.

| Значение | int | Описание |
|----------|-----|----------|
| `User` | 0 | Обычный пользователь / блогер |
| `Chef` | 1 | Самозанятый повар |
| `Restaurant` | 2 | Ресторан |
| `Brand` | 3 | Бренд / производитель (зарезервировано на будущее) |

### 6.6. MeasureUnitType

| Значение | int | Примеры |
|----------|-----|---------|
| `Mass` | 0 | г, кг |
| `Volume` | 1 | мл, л, ст.л, ч.л, стакан |
| `Count` | 2 | шт |
| `Pinch` | 3 | щепотка (неконвертируемое) |

### 6.7. YieldUnit

Единица выхода готового продукта.

| Значение | int |
|----------|-----|
| `Grams` | 0 |
| `Kilograms` | 1 |
| `Milliliters` | 2 |
| `Liters` | 3 |
| `Pieces` | 4 |
| `Servings` | 5 |

### 6.8. NutritionCalcMethod

Ось расчёта КБЖУ. Выбирается одна, остальное вычисляется через `Yield.GramsPerServing`.

| Значение | int | Описание |
|----------|-----|----------|
| `Per100g` | 0 | КБЖУ указаны на 100 г готового продукта |
| `PerServing` | 1 | КБЖУ указаны на одну порцию |

### 6.9. AllergenType (flags)

**Битовая маска.** Хранится как `int` в БД. Используется в `Dish.AllergensMask` и `Ingredient.AllergenType` (но там — одиночное значение).

```csharp
[Flags]
public enum AllergenType
{
    None         = 0,
    Gluten       = 1 << 0,   // 1
    Dairy        = 1 << 1,   // 2
    Eggs         = 1 << 2,   // 4
    Nuts         = 1 << 3,   // 8
    Peanuts      = 1 << 4,   // 16
    Fish         = 1 << 5,   // 32
    Shellfish    = 1 << 6,   // 64
    Soy          = 1 << 7,   // 128
    Sesame       = 1 << 8,   // 256
    Mustard      = 1 << 9,   // 512
    Celery       = 1 << 10,  // 1024
    Sulphites    = 1 << 11   // 2048
}
```

### 6.10. DietLabels (flags)

**Битовая маска.** Хранится в `Dish.DietLabelsMask`.

```csharp
[Flags]
public enum DietLabels
{
    None           = 0,
    Vegetarian     = 1 << 0,   // 1
    Vegan          = 1 << 1,   // 2
    GlutenFree     = 1 << 2,   // 4
    LactoseFree    = 1 << 3,   // 8
    Halal          = 1 << 4,   // 16
    Kosher         = 1 << 5,   // 32
    KetoFriendly   = 1 << 6,   // 64
    LowCarb        = 1 << 7,   // 128
    LowCalorie     = 1 << 8,   // 256
    SugarFree      = 1 << 9    // 512
}
```

---

## 7. Core-сущности — детальный разбор

### 7.1. Dish 🟢

**Назначение.** Корень агрегата. Публичная карточка блюда, которую видят все пользователи (включая гостей). Содержит ссылку на `Recipe` и внутреннее состояние (статус, модерация, рейтинг).

**Базовый класс:** `AggregateRoot<Guid>` (так как нужны доменные события на будущих этапах — `DishPublishedEvent` и т.д.).

#### Поля

| Поле | Тип (C#) | БД-тип | Null | Описание |
|------|----------|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `AuthorUserId` | `Guid` | uuid | NOT NULL | Автор (ссылка на `users.UserProfiles.UserId`). Без FK-constraint — кросс-модульно. Индекс обязателен |
| `Name` | `string` | varchar(200) | NOT NULL | «Борщ украинский классический» |
| `Slug` | `string` | varchar(220) | NOT NULL, UNIQUE | `borsch-ukrainskij-klassicheskij`. Генерируется при первом сохранении, дальше иммутабельный |
| `ShortDescription` | `string?` | varchar(500) | NULL | Краткая подводка для карточек каталога |
| `Description` | `string?` | text | NULL | Полное «аппетитное» описание, markdown. НЕ рецепт |
| `HistoryText` | `string?` | text | NULL | Историко-культурный контекст. TODO: вынести в `DishHistory` на Этапе 8+ |
| `MainImageId` | `Guid?` | uuid | NULL | Главное фото. Ссылка на `media.MediaFiles.Id` без FK-constraint |
| `Status` | `DishStatus` | int | NOT NULL | Draft / Published / Unpublished / Archived |
| `ModerationStatus` | `ModerationStatus` | int | NOT NULL | Approved (дефолт) / Pending / Rejected / Flagged |
| `DifficultyLevel` | `DifficultyLevel` | int | NOT NULL | Easy / Medium / Hard / Pro |
| `CostEstimate` | `CostEstimate` | int | NOT NULL | Budget / Moderate / Expensive |
| `OwnerType` | `OwnerType` | int | NOT NULL | User / Chef / Restaurant / Brand |
| `DietLabelsMask` | `DietLabels` | int | NOT NULL, default 0 | Битовая маска диетических меток |
| `RatingAvg` | `decimal(3,2)` | numeric(3,2) | NOT NULL, default 0 | Денормализовано. Обновляется из события `DishRatedEvent` (Этап 5) |
| `RatingCount` | `int` | int | NOT NULL, default 0 | Количество оценок. Денормализовано |
| `ViewsCount` | `long` | bigint | NOT NULL, default 0 | Просмотры. Денормализовано |
| `FavoritesCount` | `int` | int | NOT NULL, default 0 | Денормализовано. Начнёт использоваться на Этапе 5 |
| `PublishedVersionData` | `string?` (jsonb) | jsonb | NULL | **Снепшот публичной версии блюда** (карточка + рецепт + шаги + ингредиенты + теги + категории + Timing + Yield + Nutrition). Заполняется при `UC-DSH-004 PublishDish`. Обнуляется при `Unpublish`/`Archive`. NULL = блюдо не имеет публичной версии. Источник для отдачи посетителям через UC-DSH-050/051/052. См. раздел 4 |
| `PublishedVersionUpdatedAt` | `DateTimeOffset?` | timestamptz | NULL | Момент последнего обновления снепшота — при публикации автором ИЛИ при каскадных обновлениях (Этап 8+). Используется для HTTP-заголовка `Last-Modified` и для журнала `DishSnapshotInvalidation`. Обнуляется при `Unpublish`/`Archive`. См. раздел 5 |
| `PublishedAt` | `DateTimeOffset?` | timestamptz | NULL | Время **последней** публикации автором (UC-DSH-004). NULL если блюдо никогда не публиковалось ИЛИ снято с публикации (`Unpublish`/`Archive`). Используется в индикаторе `UpdatedAt > PublishedAt`. См. раздел 5 |
| `CreatedAt` | `DateTimeOffset` | timestamptz | NOT NULL | Иммутабельно после создания. Используется для UI «когда блюдо появилось» |
| `UpdatedAt` | `DateTimeOffset` | timestamptz | NOT NULL | Последнее изменение данных блюда автором. См. раздел 5.3 — два способа обновления (Interceptor + явный `MarkAsUpdated`) |

#### Инварианты (проверяются в Domain)

- `Name`: не пустая, длина 3–200 символов.
- `Slug`: уникальный, генерируется автоматически, после генерации меняется только через отдельный admin-метод.
- **`AuthorUserId` иммутабельно** — устанавливается при создании, далее не меняется ни при каких обстоятельствах. См. концепцию лицензионной модели в `use-cases-dishes-draft.md`.
- Для перехода `Status: Draft → Published`:
  - `MainImageId != null`;
  - `Recipe` существует и содержит минимум 1 `RecipeStep`;
  - `Recipe` содержит минимум 1 `RecipeIngredient`;
  - `Recipe.Timing.TotalTimeMinutes > 0`.
- `Status: Published → Draft` запрещён (только через `Unpublish`).
- Не более **3 категорий** на блюдо (проверяется в `Dish.SetCategories(...)`).
- Не более **20 тегов** на блюдо.
- `RatingAvg` и `RatingCount` обновляются **только** через `UpdateRating(...)`, не через публичные сеттеры.
- **Согласованность снепшота:** `PublishedVersionData != NULL` ⇔ `PublishedAt != NULL` ⇔ `PublishedVersionUpdatedAt != NULL`. Все три либо заполнены, либо все NULL.
- При наличии записей в `DishCategoryPublished` / `DishTagPublished` для блюда — `PublishedVersionData != NULL`. И наоборот: блюдо без снепшота не имеет записей в `*Published`-таблицах.

#### Методы (Domain API)

| Метод | Назначение |
|-------|-----------|
| `static Create(authorUserId, name, ownerType, clock)` | Фабричный метод. Возвращает `Result<Dish>`. Создаёт блюдо в статусе `Draft`. `CreatedAt = UpdatedAt = clock.UtcNow`. Все «published»-метки — NULL |
| `UpdateCard(name, shortDesc, desc, difficulty, cost, mainImageId)` | Обновление полей публичной карточки. `UpdatedAt` инкрементируется автоматически через `SaveChangesInterceptor` |
| `SetDietLabels(DietLabels mask)` | Установка диетических меток |
| `SetHistoryText(string? text)` | Обновление исторического описания |
| `SetCategories(IReadOnlyCollection<Guid> categoryIds, IDateTimeProvider clock)` | Replace-семантика. Полная замена набора категорий. Лимит 0–3. **Явный вызов `MarkAsUpdated(clock)`** — чтобы UC-DSH-007 корректно сдвигал `UpdatedAt` (т.к. изменение `DishCategory` Interceptor-ом не отслеживается) |
| `SetTags(IReadOnlyCollection<Guid> tagIds, IDateTimeProvider clock)` | Replace-семантика. Лимит 0–20. **Явный `MarkAsUpdated(clock)`** — для UC-DSH-008 |
| `Publish(IDateTimeProvider clock, IPublishedSnapshotBuilder builder)` | Проверка инвариантов перехода в Published → собирает снепшот через `builder` → `PublishedVersionData = snapshot`, `PublishedAt = clock.UtcNow`, `PublishedVersionUpdatedAt = clock.UtcNow`. Для повторных публикаций — то же самое (PublishedAt всегда обновляется). См. UC-DSH-004 |
| `Unpublish()` | `Status = Unpublished`, `PublishedVersionData = NULL`, `PublishedAt = NULL`, `PublishedVersionUpdatedAt = NULL`. Записи `*Published`-таблиц очищаются на уровне репозитория |
| `Archive()` | `Status = Archived`. Эффект на снепшот аналогичен `Unpublish`. См. UC-DSH-006 |
| `UpdateRating(decimal avg, int count)` | Вызывается из event handler'а |
| `IncrementViews()` | Атомарный инкремент. Для частого использования — можно через отдельный SQL update |
| `RegenerateSlug(string newSlug)` | Admin-only. С осторожностью — ломает SEO |
| `RebuildPublishedSnapshot(IPublishedSnapshotBuilder builder, IDateTimeProvider clock)` | Внутренний метод для каскадных обновлений (Этап 8+): пересобирает снепшот из текущих основных таблиц, обновляет `PublishedVersionUpdatedAt`. **НЕ** трогает `UpdatedAt`, `PublishedAt`. Вызывается из фоновой задачи `RecalculatePublishedSnapshots` |
| `MarkAsUpdated(IDateTimeProvider clock)` | Internal-метод: ставит `UpdatedAt = clock.UtcNow`. Используется в `SetCategories`/`SetTags`, где Interceptor не сработает автоматически |

#### Заметки

- **Кросс-модульные ссылки** (`AuthorUserId`, `MainImageId`): не создаём FK-constraints между схемами `dishes`, `users`, `media`. Это даёт возможность в будущем вынести модули в отдельные БД. Целостность обеспечивается на уровне Application (проверки через `IUsersService`, `IMediaService`).
- **`OwnerType`** денормализуется на момент публикации. При смене роли автора старые блюда сохраняют тип, который был в момент публикации. Это корректное поведение.
- **Авторизация модификаций** — все UC, изменяющие состояние блюда (UC-DSH-002–010, 020–023, 030–033, 040–042), проходят через политику `POL-001 Dish Ownership Policy`. См. `POL-001-dish-ownership.md`. Domain не проверяет авторизацию сам — это работа Application.
- **`SaveChangesInterceptor`** для `UpdatedAt` настраивается на семь сущностей (см. раздел 5.3). Поля `RatingAvg`/`RatingCount`/`ViewsCount`/`FavoritesCount`/`PublishedVersionData`/`PublishedVersionUpdatedAt` в самом `Dish` исключаются — их изменение не должно сдвигать `UpdatedAt`.

---

### 7.2. Recipe 🟢

**Назначение.** Детальная инструкция приготовления. Часть агрегата `Dish`. Доступ — только Premium+ (проверка на уровне Application/Authorization).

**Базовый класс:** `Entity<Guid>` (не агрегат, так как корень — `Dish`).

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `DishId` | `Guid` | uuid | NOT NULL, UNIQUE | FK на `Dish`, связь 1:1 |
| `IntroductionText` | `string?` | varchar(2000) | NULL | Вступительное слово автора перед шагами |
| `ServingsDefault` | `int` | int | NOT NULL, default 4 | Количество порций по умолчанию. Используется калькулятором |
| `IsAlcoholic` | `bool` | boolean | NOT NULL, default false | Для фильтрации |
| `AllergensMask` | `AllergenType` | int | NOT NULL, default 0 | Битовая маска. Пересчитывается из `RecipeIngredient → Ingredient.AllergenType` при сохранении |
| `AuthorTips` | `string?` | varchar(2000) | NULL | Советы от автора (отдельно от шагов) |
| `ServingSuggestions` | `string?` | varchar(1000) | NULL | «С чем подавать» |
| `Notes` | `string?` | text | NULL | Свободное поле для дополнительной информации на усмотрение автора: варианты замены, личные примечания, оговорки. До 4000 символов (валидация) |
| `TimingId` | `Guid` | uuid | NOT NULL, UNIQUE | FK на `Timing` (1:1) |
| `YieldId` | `Guid` | uuid | NOT NULL, UNIQUE | FK на `Yield` (1:1) |
| `NutritionId` | `Guid?` | uuid | NULL | FK на `Nutrition` (1:1). Может отсутствовать — автор не обязан заполнять |

#### Инварианты

- `ServingsDefault > 0` и `ServingsDefault <= 100`.
- `AllergensMask` **всегда** пересчитывается при изменении состава `RecipeIngredient`, а не устанавливается вручную.
- При создании `Recipe` автоматически создаются `Timing` и `Yield` с дефолтными значениями.

#### Методы (Domain API)

| Метод | Назначение |
|-------|-----------|
| `static Create(dishId, servingsDefault)` | Создаёт рецепт с дефолтными `Timing` и `Yield` |
| `UpdateIntroduction(string? text)` | |
| `UpdateServings(int count)` | |
| `UpdateTips(authorTips, servingSuggestions)` | |
| `UpdateNotes(string? notes)` | Обновление свободного поля комментариев |
| `SetAlcoholic(bool value)` | |
| `AddStep(order, title, description, ...)` / `RemoveStep(...)` / `ReorderSteps(...)` | Управление шагами |
| `AddIngredient(ingredientId, quantity, unitId, ...)` / `AddIngredientFreeform(text, quantity, unitId, ...)` / `RemoveIngredient(...)` | Управление ингредиентами |
| `RecalculateAllergens(...)` | Внутренний метод, вызывается после изменения ингредиентов |

---

### 7.3. RecipeStep 🟢

**Назначение.** Один шаг приготовления. Принадлежит `Recipe`.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `RecipeId` | `Guid` | uuid | NOT NULL | FK |
| `Order` | `int` | int | NOT NULL | Порядковый номер (1, 2, 3...). UNIQUE в рамках Recipe |
| `Title` | `string?` | varchar(200) | NULL | Короткий заголовок: «Варим бульон» |
| `Description` | `string` | text | NOT NULL | Основной текст шага |
| `ImageMediaId` | `Guid?` | uuid | NULL | Иллюстрация шага. Ссылка на `media.MediaFiles` без FK |
| `VideoUrl` | `string?` | varchar(500) | NULL | YouTube/VK/внешний плеер |
| `TemperatureCelsius` | `int?` | int | NULL | «Поставить в духовку на 180°» |
| `TimerMinutes` | `int?` | int | NULL | «Тушить 20 минут». Для UI-таймера |

#### Инварианты

- `Order`: целое положительное, уникально в рамках `RecipeId`.
- `Description`: не пустое, длина 10–4000 символов.
- `TemperatureCelsius`: если указано, диапазон -30…300.
- `TimerMinutes`: если указано, 1…1440 (до 24 часов).
- При удалении шага — автоматический re-order оставшихся.

---

### 7.4. RecipeIngredient 🟢

**Назначение.** Позиция в списке ингредиентов конкретного рецепта. Гибрид: из справочника ИЛИ свободный текст.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `RecipeId` | `Guid` | uuid | NOT NULL | FK |
| `IngredientId` | `Guid?` | uuid | NULL | FK — если выбран из справочника |
| `IngredientSpecId` | `Guid?` | uuid | NULL | FK — уточнение сорта (stub на Этапе 2) |
| `FreeformText` | `string?` | varchar(200) | NULL | Свободный ввод, если в справочнике нет |
| `Quantity` | `decimal(10,3)` | numeric(10,3) | NOT NULL | «1.5» |
| `MeasureUnitId` | `Guid` | uuid | NOT NULL | FK. В каких единицах указано |
| `Order` | `int` | int | NOT NULL | Порядок в списке |
| `IsOptional` | `bool` | boolean | NOT NULL, default false | «по желанию» |
| `PreparationNote` | `string?` | varchar(200) | NULL | «мелко рубленая», «комнатной температуры» |
| `GroupId` | `Guid?` | uuid | NULL | FK на `IngredientGroup` (⚪ Deferred — поле заложено на будущее) |

#### Инварианты (ключевой)

**Заполнено ровно одно из двух:** `IngredientId IS NOT NULL` ИЛИ `FreeformText IS NOT NULL` (но не оба, и не ни одного).

Проверяется в Domain-конструкторе и в CHECK-constraint БД:
```sql
CHECK (
    (ingredient_id IS NOT NULL AND freeform_text IS NULL)
    OR
    (ingredient_id IS NULL AND freeform_text IS NOT NULL)
)
```

- `IngredientSpecId` — только при заполненном `IngredientId` (не работает со свободным текстом).
- `Quantity > 0`.
- `MeasureUnitId` обязателен всегда.

---

### 7.5. Category 🟢

**Назначение.** Каталог категорий. Иерархия до 3 уровней. Управляется админом.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `Name` | `string` | varchar(100) | NOT NULL | «Супы», «Грузинская кухня» |
| `Slug` | `string` | varchar(120) | NOT NULL, UNIQUE | `supy`, `gruzinskaya-kuhnya` |
| `ParentId` | `Guid?` | uuid | NULL | Self-reference для иерархии |
| `Order` | `int` | int | NOT NULL, default 0 | Для сортировки в меню |
| `IconMediaId` | `Guid?` | uuid | NULL | Иконка категории. Ссылка на `media.MediaFiles.Id` без FK-constraint (кросс-модульно). См. заметку ниже |
| `IsActive` | `bool` | boolean | NOT NULL, default true | Скрыть без удаления |

#### Инварианты

- Без циклов в `ParentId` (A → B → A запрещено).
- Глубина иерархии — не более 3 уровней (корень → дочерняя → внучатая).
- `Slug` генерируется автоматически при создании.
- Нельзя удалить категорию, если у неё есть дочерние или если она использована в `DishCategory`. Альтернатива — `IsActive = false`.

#### Заметки по `IconMediaId`

Иконки категорий — системный контент, загружается администратором. В модуле Media при проектировании потребуется учесть:

- У системных медиа нет «обычного» владельца-пользователя. Варианты: зарезервированный системный `Guid` (например, `Guid.Empty`) или сделать `MediaFile.OwnerUserId` nullable.
- При отображении страницы с N категориями — N lookup в Media. Решается batch-запросом или кэшированием (иконок немного, ~20–30 штук, редко меняются).

Альтернатива, от которой отказались — хранить прямой URL (`IconUrl` в `wwwroot/icons/` или на CDN). Проще, но ломает единообразие работы с файлами и не получает плюсов Media (валидация, thumbnails, контроль доступа).

При проектировании Media вернёмся к этому пункту и окончательно закроем вопрос системного владельца.

---

### 7.6. DishCategory 🟢

**Назначение.** Связующая таблица M:M между `Dish` и `Category` для **рабочей версии** блюда. Используется автором при правке (UC-DSH-007). Без доменной логики.

#### Поля

| Поле | Тип | БД-тип | Описание |
|------|-----|--------|----------|
| `DishId` | `Guid` | uuid | Часть composite PK |
| `CategoryId` | `Guid` | uuid | Часть composite PK |

PK: `(DishId, CategoryId)`. Оба поля — FK на соответствующие таблицы с `ON DELETE CASCADE`.

В коде EF Core это обычно конфигурируется через `.HasMany().WithMany().UsingEntity(...)` без отдельного класса.

---

### 7.6.1. DishCategoryPublished 🟢

**Назначение.** Связующая таблица M:M для **опубликованной версии** блюда. Заполняется при `UC-DSH-004 PublishDish` синхронно с обновлением `PublishedVersionData`. Используется в каталожном фильтре (UC-DSH-054) — посетители видят категории по этой таблице, а не по `DishCategory`.

#### Поля

| Поле | Тип | БД-тип | Описание |
|------|-----|--------|----------|
| `DishId` | `Guid` | uuid | Часть composite PK |
| `CategoryId` | `Guid` | uuid | Часть composite PK |

PK: `(DishId, CategoryId)`. Структура полностью идентична `DishCategory`.

#### Бизнес-логика

- При `UC-DSH-004` для блюда: записи в `DishCategoryPublished` для этого `DishId` **полностью** заменяются (delete + insert) на текущий набор из `DishCategory`.
- При `UC-DSH-005 Unpublish` и `UC-DSH-006 Archive` — все записи для блюда удаляются.
- При каскадных операциях (UC-DSH-102 — переименование категории, UC-DSH-103 — удаление) — обновляются **синхронно в той же транзакции** админской операции (Этап 8+). См. раздел 5.5.

#### Заметки

- Изменения в этой таблице **не задевают `Dish.UpdatedAt`** — это техническая денормализация, к рабочей версии блюда не относится.
- Индексы: `(CategoryId, DishId)` — для каталожного фильтра «блюда категории X» (Этап 2).

---

### 7.7. Tag 🟢

**Назначение.** Пользовательские теги с автокомплитом по популярности.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `Name` | `string` | varchar(50) | NOT NULL | Оригинальное написание: «Без глютена» |
| `NormalizedName` | `string` | varchar(50) | NOT NULL, UNIQUE | `bez-glyutena` (lowercase + trim + транслит). По нему ищем дубли |
| `UsageCount` | `int` | int | NOT NULL, default 0 | Количество блюд с тегом. Обновляется при Add/Remove |
| `IsVerified` | `bool` | boolean | NOT NULL, default false | Админ одобрил для отображения в общем списке автокомплита |
| `CreatedByUserId` | `Guid?` | uuid | NULL | Кто первый создал |
| `CreatedAt` | `DateTimeOffset` | timestamptz | NOT NULL | |

#### Бизнес-логика

- При выполнении `UC-DSH-008 SetTags` — для каждого имени из списка вызывается `TagRepository.FindOrCreateByNormalizedName(name)`. Если тег с таким нормализованным именем существует — используем его, иначе создаём.
- `UsageCount` обновляется при изменении набора тегов блюда: для добавленных тегов инкрементируется, для удалённых — декрементируется. Атомарно на уровне репозитория.
- В автокомплите (UC-DSH-060) показываются теги с `IsVerified = true` ИЛИ `UsageCount >= N` (порог настраиваемый, например, 5).

---

### 7.8. DishTag 🟢

Связующая таблица M:M для **рабочей версии** блюда, аналогично `DishCategory`. Composite PK: `(DishId, TagId)`. Используется автором при правке (UC-DSH-008). Изменения **не задевают `Dish.UpdatedAt`** на уровне Interceptor — но Domain-метод `Dish.SetTags(...)` явно вызывает `MarkAsUpdated(clock)`.

---

### 7.8.1. DishTagPublished 🟢

**Назначение.** Связующая таблица M:M для **опубликованной версии** блюда. Структура полностью идентична `DishTag` (composite PK `(DishId, TagId)`).

Заполняется при `UC-DSH-004 PublishDish` синхронно с `PublishedVersionData`. Используется в каталожном фильтре по тегам.

#### Бизнес-логика

- При `UC-DSH-004` для блюда: записи `DishTagPublished` для этого `DishId` полностью заменяются на текущий набор из `DishTag`.
- При `Unpublish` / `Archive` — все записи для блюда удаляются.
- При каскадных операциях (UC-DSH-131 — удаление тега, UC-DSH-132 — объединение тегов) — обновляются синхронно в транзакции админской операции (Этап 8+).

#### Заметки

- Изменения в этой таблице **не задевают `Dish.UpdatedAt`**.
- Индексы: `(TagId, DishId)` — для каталожного фильтра.

---

### 7.9. Ingredient 🟢

**Назначение.** Глобальный справочник продуктов. Одно название — одна запись. Сорта — через `IngredientSpec`.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `Name` | `string` | varchar(200) | NOT NULL, UNIQUE | «Мука пшеничная» |
| `PluralName` | `string?` | varchar(200) | NULL | «муки» — для текстовой генерации «200г муки» |
| `Description` | `string?` | text | NULL | Описание продукта для карточки в справочнике. До 4000 символов (валидация). TODO: при реализации `IngredientDetails` на Этапе 8+ — перенести сюда |
| `ImageMediaId` | `Guid?` | uuid | NULL | Изображение продукта. Ссылка на `media.MediaFiles.Id` без FK-constraint. TODO: при реализации `IngredientDetails` — перенести |
| `IsLiquid` | `bool` | boolean | NOT NULL, default false | Флаг жидкости для конвертации объём ↔ вес |
| `DensityApprox` | `decimal(5,3)?` | numeric(5,3) | NULL | г/мл (например, молоко ≈ 1.030) |
| `IsAllergen` | `bool` | boolean | NOT NULL, default false | Быстрый флаг для фильтра |
| `AllergenType` | `AllergenType?` | int | NULL | Тип аллергена (если `IsAllergen = true`) |
| `BaseMeasureUnitId` | `Guid` | uuid | NOT NULL | FK. Базовая единица хранения (обычно «г» или «мл») |
| `DefaultNutritionId` | `Guid?` | uuid | NULL | FK на `Nutrition`. Базовые КБЖУ — используются, если не указан `IngredientSpec` |
| `IsActive` | `bool` | boolean | NOT NULL, default true | Скрыть устаревший без удаления |
| `CreatedAt` | `DateTimeOffset` | timestamptz | NOT NULL | |

#### Инварианты

- `Name` уникально в рамках таблицы.
- Если `IsLiquid = true`, `DensityApprox` должно быть заполнено (для корректной конвертации).
- Если `IsAllergen = true`, `AllergenType` должно быть заполнено.

#### Заметка на будущее — возможный вынос в `IngredientDetails` (Этап 8+)

Сейчас `Ingredient` совмещает две роли: справочник-«обложка» (Name, PluralName, флаги, КБЖУ) и развёрнутая карточка (Description, ImageMediaId). На Этапе 2 это оправдано простотой.

**Когда потребуется выделение** в отдельную сущность `IngredientDetails` по аналогии с `Dish → Recipe`:

- появление дополнительных полей: `HistoryText` (история продукта), `SubstitutionTips` (чем заменить), `SeasonalityInfo` (сезонность), `StorageTips` (как хранить);
- разделение прав доступа: справочник читается всеми, развёрнутая карточка — только авторизованными / Premium;
- оптимизация запросов каталога блюд (не грузить description из БД, когда нужен только Name).

**Схема миграции при необходимости:**
1. Создать таблицу `IngredientDetails` (IngredientId UNIQUE, Description, ImageMediaId, …).
2. Миграцией перенести данные из `Ingredient.Description` / `Ingredient.ImageMediaId` в новую таблицу.
3. Удалить поля из `Ingredient`.
4. Обновить DTO / Use cases.

До этого — работаем с полями прямо в `Ingredient`.

**Назначение.** Справочник единиц измерения с коэффициентами конвертации.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `Code` | `string` | varchar(20) | NOT NULL, UNIQUE | `g`, `kg`, `ml`, `tbsp`, `tsp`, `cup_250`, `pinch`, `pcs` |
| `NameRu` | `string` | varchar(50) | NOT NULL | «грамм», «столовая ложка» |
| `Type` | `MeasureUnitType` | int | NOT NULL | Mass / Volume / Count / Pinch |
| `ConversionToBase` | `decimal(10,5)` | numeric(10,5) | NOT NULL | Коэффициент пересчёта к базовой единице своего типа |
| `IsBase` | `bool` | boolean | NOT NULL | Это базовая единица своего типа? (ровно одна на Type) |

#### Инварианты и логика

- Ровно одна запись с `IsBase = true` в пределах каждого `Type` (за исключением `Pinch` — он не конвертируется).
- Базовая для `Mass` — граммы (`ConversionToBase = 1`).
- Базовая для `Volume` — миллилитры (`ConversionToBase = 1`).
- Примеры: `kg.ConversionToBase = 1000`, `tbsp.ConversionToBase = 15` (мл), `cup_250.ConversionToBase = 250` (мл).
- Конвертация **только внутри одного `Type`**. Mass ↔ Volume возможна **только** при известной `Ingredient.DensityApprox`.

---

### 7.11. Timing 🟢

**Назначение.** Времена этапов приготовления рецепта.

**Базовый класс:** `Entity<Guid>`. Таблица отдельная (1:1 с `Recipe`), но концептуально — owned.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `PrepTimeMinutes` | `int?` | int | NULL | Подготовка: порезать, замариновать, достать из холодильника |
| `CookTimeMinutes` | `int?` | int | NULL | Основное приготовление (варка, жарка, запекание) |
| `RestTimeMinutes` | `int?` | int | NULL | Отдых: тесто подошло, мясо «отдохнуло» после жарки |
| `ActiveTimeMinutes` | `int?` | int | NULL | Сколько повар активно участвует (подмножество общего времени) |
| `TotalTimeMinutes` | `int` | int | NOT NULL | Общее время. Единственное обязательное поле |
| `IsTotalManual` | `bool` | boolean | NOT NULL, default true | `true` — заполнено вручную; `false` — рассчитано из `Prep + Cook + Rest` |

#### Логика

- Если `IsTotalManual = false`: `TotalTimeMinutes = (PrepTimeMinutes ?? 0) + (CookTimeMinutes ?? 0) + (RestTimeMinutes ?? 0)` при каждом сохранении.
- `ActiveTimeMinutes` **не** включается в сумму — это отдельная метрика «сколько нужно быть на кухне».
- Если автор заполняет только `TotalTimeMinutes` — `IsTotalManual = true`, остальные поля NULL.

---

### 7.12. Yield 🟢

**Назначение.** Выход готового продукта и размер порции.

**Базовый класс:** `Entity<Guid>`. Таблица 1:1 с `Recipe`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `QuantityTotal` | `decimal(8,2)` | numeric(8,2) | NOT NULL | «Получилось 2 кг» или «4 порции» |
| `YieldUnit` | `YieldUnit` | int | NOT NULL | Единица выхода (Grams / Kilograms / Servings / …) |
| `ServingsCount` | `int` | int | NOT NULL | Сколько порций |
| `GramsPerServing` | `decimal(6,1)?` | numeric(6,1) | NULL | Вес одной порции в граммах. Критичен для расчёта КБЖУ «на порцию» |

#### Инварианты

- `QuantityTotal > 0`.
- `ServingsCount > 0`.
- Если `YieldUnit == Grams` или `Kilograms`: `GramsPerServing` должно вычисляться как `QuantityTotal_в_граммах / ServingsCount`.

---

### 7.13. Nutrition 🟢

**Назначение.** Пищевая ценность. Используется в `Recipe`, `Ingredient`, `IngredientSpec`.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `CalcMethod` | `NutritionCalcMethod` | int | NOT NULL | `Per100g` или `PerServing` |
| `Calories` | `decimal(7,2)` | numeric(7,2) | NOT NULL | ккал |
| `Proteins` | `decimal(6,2)` | numeric(6,2) | NOT NULL | г |
| `Fats` | `decimal(6,2)` | numeric(6,2) | NOT NULL | г |
| `SaturatedFats` | `decimal(6,2)?` | numeric(6,2) | NULL | Насыщенные жиры, г (для ЗОЖ) |
| `Carbs` | `decimal(6,2)` | numeric(6,2) | NOT NULL | г |
| `Sugar` | `decimal(6,2)?` | numeric(6,2) | NULL | Из них сахар, г |
| `Fiber` | `decimal(6,2)?` | numeric(6,2) | NULL | Клетчатка, г |
| `Salt` | `decimal(6,2)?` | numeric(6,2) | NULL | г. Важно для некоторых диет |

#### Инварианты

- Все значения `>= 0`.
- `SaturatedFats <= Fats` (если оба заполнены).
- `Sugar <= Carbs` (если оба заполнены).

---

## 8. Stub-сущности

### 8.1. IngredientSpec 🟡

**Назначение.** Уточнение сорта/вида ингредиента (например, «Молоко» → «3.2%», «2.5%», «Безлактозное»). На Этапе 2 — минимальная реализация без развитой логики.

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `IngredientId` | `Guid` | uuid | NOT NULL | FK на `Ingredient` |
| `SpecName` | `string` | varchar(100) | NOT NULL | «Высший сорт», «3.2%», «Цельнозерновая» |
| `NutritionId` | `Guid` | uuid | NOT NULL, UNIQUE | FK на `Nutrition` (КБЖУ для этого сорта) |

#### Что делаем на Этапе 2

- Таблица создаётся.
- Базовая Domain-модель с конструктором.
- Репозиторий с минимумом: `GetByIdAsync`, `GetByIngredientIdAsync`.
- Seed: 0–2 примера (например, «Мука → Высший сорт»).
- API для управления — **нет** (Admin работает напрямую с БД при необходимости).
- `RecipeIngredient.IngredientSpecId` может быть заполнен, но в UI пока не предлагается.

#### Расширение на Этапе 8

- Admin CRUD API: `/api/admin/ingredients/{id}/specs`.
- UI выбора сорта при добавлении ингредиента в рецепт.
- Если `IngredientSpecId` заполнен — КБЖУ берутся из `IngredientSpec.Nutrition`, иначе из `Ingredient.DefaultNutrition`.
- Seed: расширенный набор сортов для популярных ингредиентов.

---

## 9. Deferred-сущности (на будущие этапы)

Кратко перечисляю, чтобы видеть полную картину модели. Детали — при реализации соответствующего этапа.

### 9.1. UserCollection ⚪ (Этап 5)

Личные подборки пользователя («Мои любимые завтраки», «На выходные»). Отдельная сущность в модуле Dishes или Users — обсудим на Этапе 5.

**Поля (предварительно):** `Id`, `OwnerUserId`, `Name`, `Description`, `IsPublic`, `CoverImageId`, `CreatedAt`.

**Связь:** `UserCollection ↔ Dish (M:M)` через `UserCollectionDish`.

### 9.2. IngredientGroup ⚪ (Этап 8+)

Группы взаимозаменяемости в рамках одного рецепта: «Лук красный ИЛИ белый». Позволяет пользователю готовить из того, что есть.

**Поле `RecipeIngredient.GroupId`** уже заложено на Этапе 2 — при реализации не потребуется миграции `RecipeIngredient`.

### 9.3. Equipment ⚪ (Этап 8+)

Справочник оборудования («Блендер», «Сковорода гриль», «Пароварка»). Позволит фильтр «что я могу приготовить с моим оборудованием».

**Связи:** `Equipment ↔ Recipe (M:M)`, `Equipment ↔ RecipeStep (M:M)`.

### 9.4. CookingLossCoefficient ⚪ (Этап 8+)

Коэффициенты уварки/ужарки для точного расчёта КБЖУ готового блюда. Профессиональная функция.

**Поля (предварительно):** `Id`, `IngredientId`, `CookingMethod` (enum: Boil, Fry, Bake, Stew), `LossPercent`.

### 9.5. DishHistory ⚪ (Этап 8+)

Отдельная таблица для культурно-исторического описания блюд. Пока эту роль выполняет поле `Dish.HistoryText`.

**Предварительные поля:** `Id`, `DishId` (1:1), `HistoryText`, `PeriodFrom`, `PeriodTo`, `OriginRegion`, `SourceReferences` (jsonb — ссылки на книги/статьи).

**При миграции на Этапе 8+:** текст из `Dish.HistoryText` переносится в `DishHistory.HistoryText`, поле в `Dish` удаляется.

### 9.6. ServiceSpec (Бракераж) ⚪ (Этап 8+)

Профессиональные параметры подачи для ресторанов: температура подачи, срок годности после приготовления, условия хранения.

**Предварительные поля:** `Id`, `RecipeId` (1:1), `ServingTempCelsius`, `ShelfLifeHours`, `StorageNotes`.

### 9.7. IngredientDetails ⚪ (Этап 8+)

Отдельная сущность для развёрнутой карточки ингредиента. По аналогии с `Dish → Recipe`: `Ingredient` становится справочной «обложкой», `IngredientDetails` — развёрнутой статьёй.

**Предварительные поля:** `Id`, `IngredientId` (1:1), `Description`, `ImageMediaId`, `HistoryText`, `SubstitutionTips`, `SeasonalityInfo`, `StorageTips`.

**При миграции на Этапе 8+:** поля `Description` и `ImageMediaId` из `Ingredient` переносятся в `IngredientDetails`, в `Ingredient` удаляются.

Пока (Этап 2) — `Description` и `ImageMediaId` живут прямо в `Ingredient`.

### 9.8. DishOwnership ⚪ (Этап 4+)

**Назначение.** Side-таблица для случаев, когда блюдо передано в собственность платформы (например, по результатам конкурсов). По умолчанию (отсутствие записи в этой таблице) блюдо считается `AuthorOwned` — принадлежит автору с лицензией платформе.

**Предварительные поля:**

| Поле | Тип | Описание |
|------|-----|----------|
| `DishId` | uuid | PK + FK |
| `OwnershipType` | enum (`AuthorOwned`/`PlatformOwned`) | По умолчанию `AuthorOwned`, в таблицу попадают только `PlatformOwned` |
| `TransferredAt` | timestamptz | Когда передано (NULL для AuthorOwned) |
| `TransferReason` | varchar(200) | «Победа в конкурсе X», «Заказная разработка» |
| `TransferDocumentRef` | varchar(500) | Ссылка на электронный договор / акт |

**Эффект для PlatformOwned блюд:**
- Автор не может Unpublish, Archive, редактировать.
- Модифицировать может только `Admin` (расширение POL-001 на Этапе 4+).
- Атрибуция «Автор: ...» в карточке остаётся, но это уже декоративная подпись.

**Почему side-таблица, а не колонка в `Dish`:** 99% блюд `AuthorOwned` (по умолчанию), и колонка раздувала бы основную таблицу без пользы. PlatformOwned — редкость, для них side-таблица оправдана.

### 9.9. DishVersion ⚪ (Этап 4+ или 8+)

**Назначение.** История публикаций блюда. На Этапе 2 `PublishedVersionData` содержит **только последнюю** опубликованную версию. На будущих этапах при необходимости (откат, аналитика, аудит) появится история всех версий.

**Предварительные поля:**

| Поле | Тип | Описание |
|------|-----|----------|
| `Id` | uuid | PK |
| `DishId` | uuid | FK |
| `VersionNumber` | int | 1, 2, 3, … |
| `VersionData` | jsonb | Полный снепшот версии |
| `ActionType` | enum (Published / Updated / Deleted) | Тип события, создавшего версию |
| `ActionAt` | timestamptz | |
| `ActionByUserId` | uuid | Кто инициировал |
| `ActionReason` | varchar(500) NULL | Причина (для модерации, отзыва и т.п.) |

**Стратегии управления размером таблицы** (если объём станет проблемой):
- Партиционирование по году.
- Хранение только последних N версий каждого блюда.
- Перенос старых версий в холодное хранилище (S3 Glacier-style).

### 9.10. DishSnapshotInvalidation ⚪ (Этап 8+)

**Назначение.** Журнал записей о том, какие снепшоты блюд устарели после админских операций (объединение тегов, переименование категорий и т.п.). Используется фоновой задачей `RecalculatePublishedSnapshots` для пересборки jsonb-снепшотов асинхронно.

**Предварительные поля:**

| Поле | Тип | Описание |
|------|-----|----------|
| `Id` | uuid | PK |
| `DishId` | uuid | Какое блюдо требует пересборки |
| `Reason` | enum (`TagsMerged`, `CategoryRenamed`, `IngredientDeactivated`, …) | Почему устарело |
| `Priority` | enum (`Normal`, `Low`) | Для приоритизации в часы пик |
| `InvalidatedAt` | timestamptz | Когда поставлено в очередь |
| `ProcessedAt` | timestamptz NULL | Когда обработано (NULL — ещё не обработано) |

**Принцип работы:** при админской операции в той же транзакции пишутся:
1. Изменения в основные таблицы и `*Published`-таблицы (синхронно).
2. Записи в `DishSnapshotInvalidation` для всех затронутых блюд.

Фоновая задача `RecalculatePublishedSnapshots` (запуск каждые 5–10 минут) забирает записи с `ProcessedAt IS NULL`, пересобирает `PublishedVersionData` и обновляет `PublishedVersionUpdatedAt`. Реляционные связи на этом этапе не трогаются — они уже актуальны.

См. раздел 5.5 «Разделение синхронных правок и асинхронной пересборки снепшотов».

---

## 10. Заметки на будущее (TODO в коде)

Эти пункты оставляем комментариями `// TODO: <описание> — Этап N` в соответствующих местах кода. Цель — сохранить контекст при реализации в будущем.

### 10.1. В `Dish.cs`

```csharp
// TODO: HistoryText — Этап 8+: вынести в отдельную сущность DishHistory
//   с полями PeriodFrom/To, OriginRegion, SourceReferences (jsonb).
//   При миграции перенести данные из Dish.HistoryText → DishHistory.HistoryText.
public string? HistoryText { get; private set; }

// TODO: SeasonalityMask — Этап 5: добавить поле для фильтра «сезонные блюда»
// TODO: SourceUrl, RawText — Этап 8+: импортер рецептов с внешних сайтов
// TODO: IsFeatured — Этап 8: редакторская подборка, промо-акции
// TODO: Language — Этап 8+: мультиязычность (сейчас дефолт "ru")

// SetCategories / SetTags — Etap 2 ВНИМАНИЕ:
//   Изменение DishCategory / DishTag не отслеживается SaveChangesInterceptor для UpdatedAt
//   (он настроен на 7 сущностей самого блюда — см. раздел 5.3 domain-model.md).
//   Поэтому в этих методах нужен ЯВНЫЙ вызов MarkAsUpdated(clock).
public Result SetCategories(IReadOnlyCollection<Guid> categoryIds, IDateTimeProvider clock)
{
    // ... валидация лимита 0–3, обновление _categories ...
    MarkAsUpdated(clock);  // ← обязательно
    return Result.Success();
}

// TODO: RebuildPublishedSnapshot — Этап 8+
//   Internal-метод для каскадных обновлений (вызывается из Hosted Service
//   RecalculatePublishedSnapshots). Пересобирает PublishedVersionData из
//   текущих основных таблиц, обновляет PublishedVersionUpdatedAt.
//   НЕ трогает UpdatedAt и PublishedAt — эти метки — собственность автора.
internal void RebuildPublishedSnapshot(IPublishedSnapshotBuilder builder, IDateTimeProvider clock) { /* ... */ }
```

### 10.1.1. SaveChangesInterceptor — конфигурация

В `Dishes.Infrastructure` создаётся `UpdatedAtInterceptor`, который при `SaveChangesAsync` инкрементирует `Dish.UpdatedAt` если был изменён хотя бы один Entity из:

```csharp
// /Persistence/Interceptors/UpdatedAtInterceptor.cs
private static readonly Type[] TrackedEntityTypes =
{
    typeof(Dish),
    typeof(Recipe),
    typeof(RecipeStep),
    typeof(RecipeIngredient),
    typeof(Timing),
    typeof(Yield),
    typeof(Nutrition)
};

// Для самого Dish — некоторые поля исключаются из триггера UpdatedAt:
private static readonly string[] DishExcludedProperties =
{
    nameof(Dish.PublishedVersionData),
    nameof(Dish.PublishedVersionUpdatedAt),
    nameof(Dish.RatingAvg),
    nameof(Dish.RatingCount),
    nameof(Dish.ViewsCount),
    nameof(Dish.FavoritesCount)
};
```

См. раздел 5.3 для подробностей.

### 10.2. В `Recipe.cs`

```csharp
// TODO: ExpectedYieldWeightGrams — Этап 8+: вес готового блюда с учётом уварки.
//   Используется вместе с CookingLossCoefficient для точного КБЖУ.
// TODO: RecipeVersion — Этап 8+: история версий рецепта.
// TODO: связь M:M с Equipment — Этап 8+
// TODO: ChefsSecret (отдельное поле помимо AuthorTips) — Этап 8+
```

### 10.3. В `RecipeStep.cs`

```csharp
// TODO: связь M:M с Equipment — Этап 8+ (какое оборудование нужно на этом шаге)
```

### 10.4. В `RecipeIngredient.cs`

```csharp
// Поле GroupId уже заложено. TODO: связь на IngredientGroup — Этап 8+
public Guid? GroupId { get; private set; }
```

### 10.5. В `Category.cs`

```csharp
// TODO: IconMediaId — на Этапе 2 указывает на media.MediaFiles без FK.
//   При проектировании Media решить вопрос системного владельца
//   (системный Guid или nullable OwnerUserId в MediaFile).
public Guid? IconMediaId { get; private set; }
```

### 10.6. В `Ingredient.cs`

```csharp
// TODO: Description + ImageMediaId — Этап 8+: вынести в отдельную сущность
//   IngredientDetails (по аналогии с Dish → Recipe).
//   Появятся поля: HistoryText, SubstitutionTips, SeasonalityInfo, StorageTips.
//   При миграции перенести данные из Ingredient в IngredientDetails, поля удалить.
public string? Description { get; private set; }
public Guid? ImageMediaId { get; private set; }
```

### 10.7. В `IngredientSpec.cs`

```csharp
// TODO: Расширение — Этап 8+
//   - Admin CRUD API для управления сортами
//   - UI выбора сорта при добавлении в рецепт
//   - Приоритет КБЖУ: IngredientSpec.Nutrition > Ingredient.DefaultNutrition
//   - Seed: популярные сорта для топовых ингредиентов
```

### 10.8. Новые таблицы (когда появятся)

Файлы-заглушки **не создаём** — это нарушает Clean Architecture (пустая сущность без смысла). Вместо этого в `Dishes.Domain/Entities/` оставляем `README.md` со списком запланированных сущностей:

```markdown
# TODO — будущие сущности модуля Dishes

- **DishHistory** (Этап 8+) — вынос `Dish.HistoryText` в отдельную таблицу
- **IngredientDetails** (Этап 8+) — вынос `Ingredient.Description` и `Ingredient.ImageMediaId` в отдельную сущность-развёрнутую карточку
- **IngredientGroup** (Этап 8+) — группы взаимозаменяемости ингредиентов
- **Equipment** (Этап 8+) — справочник оборудования
- **CookingLossCoefficient** (Этап 8+) — коэффициенты потерь при термообработке
- **ServiceSpec** (Этап 8+) — бракераж (температура подачи, срок годности)
- **UserCollection** (Этап 5) — пользовательские подборки рецептов
- **DishOwnership** (Этап 4+) — side-таблица для блюд PlatformOwned (конкурсы)
- **DishVersion** (Этап 4+ или 8+) — история всех опубликованных версий
- **DishSnapshotInvalidation** (Этап 8+) — журнал устаревших снепшотов для фоновой пересборки
```

---

## 11. Seed-данные — план

Согласно договорённости — детально обсуждаем ближе к Этапу 4 (когда будет веб-интерфейс для тестирования). На Этапе 2 — минимальный набор для unit- и integration-тестов.

### 11.1. Минимум на Этапе 2 (для тестов)

- **MeasureUnit** — полный справочник единиц (~15 записей). Без этого рецепты не создать.
- **Ingredient** — 3–5 примеров для тестов (Мука, Молоко, Сахар, Яйцо куриное, Соль).
- **Category** — 3–5 категорий для тестов (без иерархии).
- **Nutrition** — базовые КБЖУ для ингредиентов из seed.

### 11.2. Расширенный набор (готовим к Этапу 4)

- **Category** — ~20 категорий с иерархией (2 уровня):
  - Основные блюда → Мясные / Рыбные / Овощные
  - Супы → Горячие / Холодные
  - Выпечка → Сладкая / Несладкая
  - Закуски, Напитки, Десерты, Салаты и т.д.
- **Ingredient** — ~100 самых популярных ингредиентов с базовыми КБЖУ.
- **MeasureUnit** — тот же набор (15 шт).

Формат: JSON-файлы в `Dishes.Infrastructure/Persistence/Seed/`, применяются при первой миграции через `HasData` в `OnModelCreating` или через отдельный `SeedService`.

---

## 12. Что дальше

### 12.1. Остались открытыми по Dishes

- **Развилка №5** (хранение локальных файлов) и **№6** (проверка владельца медиа) — перенесены в обсуждение модуля Media.
- Детальный список Use Cases с разбивкой «Core / Stub-with-XML / Deferred» — следующий шаг.

### 12.2. Дальнейший план работы над Этапом 2

1. **Модуль Media** — такое же детальное проектирование (сущности, поля, связи). Вернуться к развилкам 5 и 6.
2. **Use Cases** — составить полный список команд и запросов для Dishes и Media, определить что реализуется сейчас, а что — заглушками с XML-документацией.
3. **Контракты между модулями** — `IMediaService` в `Media.Application/Contracts/` (по аналогии с `IAuthUserService`).
4. **Каркас проектов** — создать `Dishes.Domain`, `Dishes.Application`, `Dishes.Infrastructure` и аналогично для Media. AssemblyReference, Errors, DbContext-заглушки.
5. **Заглушки Use Cases** — пустые команды/запросы с XML-описанием назначения и параметров.
6. **Реализация в порядке зависимостей**: сначала справочники (`MeasureUnit`, `Ingredient`, `Category`, `Tag`), затем `Dish/Recipe`, затем поисковые запросы.

---

## Связанные страницы

- [[05_Дорожная-карта]] — Этап 2, общий план
- [[02_Архитектура]] — архитектурные паттерны (DDD, CQRS, Clean)
- [[13_Структура-проекта]] — структура кодовой базы
- [[08_Разработка-(Development-Guide)]] — шаги создания нового модуля
