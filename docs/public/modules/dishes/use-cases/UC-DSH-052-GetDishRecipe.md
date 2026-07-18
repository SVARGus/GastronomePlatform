# UC-DSH-052: Получить рецепт блюда

**Version:** 1.1 (Premium-гейт по гранту `FullRecipes`) | **Date:** 2026-07-18

---

## Actors (Инициаторы)

- Primary: любой аутентифицированный пользователь платформы (любая роль кроме `Guest`). Гости получают `401`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `Recipe` в составе агрегата `Dish` со всеми вложенными сущностями (`Timing`, `Yield`, `Nutrition`, `RecipeStep`, `RecipeIngredient` — полиморфно по природе catalog/freeform).
- Identifier: `Dish.Id` (`Guid`) — передаётся в path-параметре эндпоинта; рецепт связан с блюдом 1:1.
- Action: Read (single).

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required. Эндпоинт защищён `[Authorize(Policy = VALID_ACTOR)]` — требуется валидный JWT с claim `sub`. Гости получают `401`.

### Authorization (Авторизация)

Решение о видимости принимается в Handler-е, не на уровне политики. Условия:

- **Есть `PublishedVersionData`** — рецепт отдаётся всем аутентифицированным пользователям. Для автора и `Admin` дополнительно поднимается флаг `HasUnsavedChanges`, если `UpdatedAt > PublishedAt`.
- **Нет `PublishedVersionData`** (статус `Draft` / `Unpublished`) — рецепт отдаётся только автору (`Dish.AuthorUserId == _currentUser.UserId`) или роли `Admin`. Остальные получают `404`.
- **`Status = Archived`** — `404` всем (включая автора и admin). Доступ admin к архивированным блюдам появится на Этапе 8+.

POL-001 здесь **не применяется** напрямую — это политика модификаций. Здесь — собственная логика чтения: ownership + статус + наличие снепшота (симметрично UC-DSH-050).

### State Constraints (Ограничения по состоянию)

- `Dish.Status = Archived` → `404` всем.
- `Dish.Status ∈ {Draft, Unpublished}` без `PublishedVersionData` → видит только автор/admin.

### Contextual Constraints (Контекстуальные ограничения)

Просмотр опубликованного рецепта требует гранта `FullRecipes` (POL-004 §4.4). Проверка выполняется через `ISubscriptionAccessService.HasFeatureAsync`; при отсутствии гранта возвращается `403` с кодом `DISHES.PREMIUM_REQUIRED`.

Автор блюда и `Admin` проходят без проверки: требовать подписку за просмотр собственного контента бессмысленно, а `Admin` — операционная роль, которую платные гейты блокировать не должны.

Гейт применяется **к возможности, а не к отдельному блюду**: маркировки «это Premium-блюдо» в модели нет — платной является сама функция просмотра полных рецептов. Пер-блюдная маркировка потребовала бы нового поля в агрегате и отдельного решения.

---

## API Contract (Контракт API)

### Endpoint

```
GET /api/dishes/{id}/recipe
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Headers:**

- `Authorization: Bearer <JWT>` — обязательно.

Query Parameters: нет. (Параметр `?version=working` для явного запроса рабочей версии автором отложен до Этапа 5+ — см. связь с UC-DSH-083.)

Body: отсутствует.

### Response (Ответ)

- Status: `200 OK`.
- Body: `DishRecipeDto`.

Состав `DishRecipeDto`:

| Поле | Тип | Описание |
|------|-----|----------|
| `dishId` | `Guid` | Идентификатор блюда (повторяется в теле для самодостаточности ответа). |
| `isPublishedVersion` | `bool` | `true`, если данные из `PublishedVersionData` (jsonb-снепшот); `false`, если из основных таблиц (рабочая версия). |
| `hasUnsavedChanges` | `bool?` | Для автора/admin: `true`, если `UpdatedAt > PublishedAt`. Для остальных — `null` (приватная информация о состоянии редактирования). |
| `recipe` | `RecipeViewDto` | Рецепт со всеми вложенными сущностями. |

Состав `RecipeViewDto`:

| Поле | Тип | Описание |
|------|-----|----------|
| `introductionText` | `string?` | Вводный текст рецепта. |
| `servingsDefault` | `int` | Количество порций по умолчанию (≥ 1). |
| `isAlcoholic` | `bool` | Признак содержания алкоголя. |
| `authorTips` | `string?` | Советы автора. |
| `servingSuggestions` | `string?` | Рекомендации по сервировке. |
| `notes` | `string?` | Дополнительные заметки. |
| `timing` | `TimingViewDto` | Времена этапов приготовления. |
| `yield` | `YieldViewDto` | Выход готового продукта и размер порции. |
| `nutrition` | `NutritionViewDto?` | Пищевая ценность. `null`, если автор не задал. |
| `steps` | `RecipeStepViewDto[]` | Шаги, упорядоченные по `order`. |
| `ingredients` | `RecipeIngredientViewDto[]` | Ингредиенты (полиморфно по природе), упорядоченные по `order`. |

Состав `RecipeIngredientViewDto` (абстрактный, полиморфный):

- Поле-дискриминатор JSON — `"type": "catalog" | "freeform"`.
- Общие поля: `id`, `order`, `quantity`, `measureUnitId`, `isOptional`, `preparationNote?`.
- `CatalogRecipeIngredientViewDto` (`"type": "catalog"`) — дополнительно `ingredientId`, `ingredientSpecId?`.
- `FreeformRecipeIngredientViewDto` (`"type": "freeform"`) — дополнительно `freeformText`.

Состав вложенных DTO (`TimingViewDto`, `YieldViewDto`, `NutritionViewDto`, `RecipeStepViewDto`) — зеркало соответствующих snapshot-DTO (см. файлы `Application/Snapshots/Dtos/Published*Dto.cs`).

**Что НЕ возвращается:**

- Денормализованные имена справочников (категорий, тегов, ингредиентов, единиц измерения). Клиент резолвит их отдельными запросами:
  - `UC-DSH-058 GetCategoryById` / `UC-DSH-057 GetCategoryTree` — для категорий.
  - `UC-DSH-063 GetIngredientById` — для ингредиентов из справочника.
  - `UC-DSH-064 GetMeasureUnits` — для единиц измерения.
- Публичная карточка блюда (`Name`, `Slug`, `Description`, …) — отдаётся через UC-DSH-050.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | `DishId` пустой (`Guid.Empty`). На практике маршрут `{id:guid}` отсечёт невалидные значения на уровне ASP.NET Core. |
| 401 | — | Отсутствует или невалиден JWT (проверка политики `VALID_ACTOR`). |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует, или `Status = Archived`, или нет `PublishedVersionData` и запрашивающий — не автор/admin. |

---

## Preconditions (Предусловия)

- HTTP-маршрут `GET /api/dishes/{id:guid}/recipe` корректно резолвится (ASP.NET Core валидирует формат `Guid` на уровне роутинга).
- Аутентификационный middleware успешно валидировал JWT.

---

## Invariants (Инварианты домена)

- Чтение не меняет состояние агрегата.
- `Dish.AuthorUserId` иммутабельно — ownership-чек стабилен между запросами.
- Содержимое `DishRecipeDto` отражает срез данных на момент чтения; никаких блокировок не накладывается.
- Полиморфная сериализация `RecipeIngredientViewDto` гарантирует наличие поля-дискриминатора `type` у каждого ингредиента в ответе и в jsonb-снепшоте (ADR-0012).

---

## Main Flow (Основной поток)

1. **Запрос.** Клиент отправляет `GET /api/dishes/{id}/recipe` с JWT в заголовке `Authorization`.
2. **Аутентификация и политика.** ASP.NET Core middleware валидирует JWT; политика `VALID_ACTOR` гарантирует валидный claim `sub`.
3. **Контроллер.** `DishesController.GetRecipeAsync(Guid id, CancellationToken ct)`:
   1. Собирает `GetDishRecipeQuery(DishId = id)`.
   2. Делегирует через `ISender.Send(query, ct)`.
4. **Валидация.** `ValidationBehavior` запускает `GetDishRecipeQueryValidator` — проверка `DishId.NotEmpty()`.
5. **Handler — `GetDishRecipeQueryHandler.Handle(...)`:**
   1. **Загрузка.** `Dish? dish = await _dishRepository.GetByIdAsync(request.DishId, ct)` — без `Recipe`.
   2. **NotFound / Archived.** Если `dish is null || dish.Status == DishStatus.Archived` → `return DishesErrors.DishNotFound` → `404`.
   3. **Определение пользователя.**
      - `currentUserId = _currentUser.UserId`.
      - `isOwner = currentUserId.HasValue && currentUserId.Value == dish.AuthorUserId`.
      - `isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN)`.
      - `isOwnerOrAdmin = isOwner || isAdmin`.
   4. **Ветка «есть снепшот».** Если `dish.PublishedVersionData is not null`:
      - `PublishedDishSnapshot snapshot = _snapshotReader.Read(dish.PublishedVersionData!)`.
      - `hasUnsavedChanges = isOwnerOrAdmin ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value) : null`.
      - Возврат `MapFromSnapshot(dish.Id, snapshot, isPublishedVersion: true, hasUnsavedChanges)`.
   5. **Ветка «нет снепшота».**
      - Если `!isOwnerOrAdmin` → `return DishesErrors.DishNotFound` → `404` (намеренно — не утечка существования черновика чужому пользователю).
      - Иначе повторная загрузка с полным агрегатом: `dish = await _dishRepository.GetByIdWithFullRecipeAsync(request.DishId, ct)`. На случай гонок (блюдо могло быть архивировано/удалено между запросами) — повторный `null`/`Archived` check → `DishesErrors.DishNotFound`.
      - Возврат `MapFromWorking(dish, isPublishedVersion: false, hasUnsavedChanges: false)`.
6. **Маппинг ответа.** `ApiController.MapResult<DishRecipeDto>(Result<DishRecipeDto>)` → `200 OK` с телом.

---

## Alternative Flows (Альтернативные потоки)

- **AF-1 «Гость пытается получить рецепт».** JWT отсутствует или невалиден → политика `VALID_ACTOR` отвечает `401`. Handler не вызывается.
- **AF-2 «Аутентифицированный читает опубликованное блюдо».** `PublishedVersionData != null`, не автор/admin → `200 OK` с публичной версией; `hasUnsavedChanges = null`.
- **AF-3 «Автор читает свой черновик».** `PublishedVersionData = null`, `isOwner = true` → `200 OK` с рабочей версией; `isPublishedVersion = false`, `hasUnsavedChanges = false`.
- **AF-4 «Автор читает опубликованное блюдо с правками».** `PublishedVersionData != null`, `isOwner = true`, `UpdatedAt > PublishedAt` → `200 OK` с публичной версией; `isPublishedVersion = true`, `hasUnsavedChanges = true`. UI рисует кнопку «Открыть черновик» (отдельный будущий UC).

---

## Edge Cases (Граничные случаи)

- **EC-1. Аутентифицированный пользователь читает чужой черновик.** `PublishedVersionData = null`, `isOwnerOrAdmin = false` → `404` (а не `403`). Намеренное поведение: ответ `403` подтвердил бы существование блюда; `404` единообразен с «не существует».
- **EC-2. `Archived` блюдо.** `404` всем (включая автора). Это часть статусной модели Этапа 2; admin-доступ к архиву придёт на Этапе 8+.
- **EC-3. Маршрут с невалидным `Guid`.** ASP.NET Core роутинг (`{id:guid}`) отвергает запрос с `404` (не доходит до Handler).
- **EC-4. Параллельная архивация блюда между двумя загрузками.** В working-ветке между `GetByIdAsync` и `GetByIdWithFullRecipeAsync` другой запрос мог архивировать блюдо. Повторный null/Archived check во второй загрузке защищает: возвращается `DishesErrors.DishNotFound`.
- **EC-5. `PublishedVersionData != null`, но `PublishedAt = null`.** Теоретически невозможно (доменная инвариантность `Dish.Publish`). `hasUnsavedChanges` устойчив: `dish.PublishedAt.HasValue && ...` отдаёт `false`.
- **EC-6. Битый jsonb-снепшот.** `PublishedDishSnapshotReader.Read(...)` бросает `JsonException`. Это нештатное состояние (баг в Publish или ручная порча БД), обрабатывается `GlobalExceptionHandlingMiddleware` как `500`. Защищать не пытаемся — это не публичный класс ошибок.
- **EC-7. Полиморфный ингредиент без поля `type` в снепшоте.** Возможно только при ручном редактировании БД или несоответствии формата. `System.Text.Json` бросает `JsonException` при десериализации. См. EC-6.

---

## Postconditions (Постусловия)

- Состояние БД не меняется.
- Никаких побочных эффектов: счётчик просмотров не инкрементируется из этого UC. UC-DSH-070 IncrementDishViews — отдельный публичный эндпоинт `POST /api/dishes/{id}/views`, пингом которого управляет клиент. Если пользователь открывает карточку и рецепт одного блюда, клиент шлёт один пинг просмотра (обычно после рендера карточки, не на каждый под-запрос).
- `DishRecipeDto` сериализуется с enum-полями как строки (через глобальный `JsonStringEnumConverter`), camelCase именованием.
- Поле-дискриминатор `type` присутствует у каждого элемента `ingredients[]` (System.Text.Json по атрибутам `[JsonPolymorphic]`/`[JsonDerivedType]`).

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Полностью идемпотентен — повторный вызов при неизменном состоянии возвращает идентичный результат.
- **Rate Limit.** Не реализован на Этапе 2.
- **Performance.** Целевое < 60 мс для snapshot-ветки (один SELECT + парсинг JSON, ~5–10 мс на крупных рецептах). Для working-ветки — < 100 мс (два SELECT с подколлекциями).
- **Consistency.** Read committed. Без блокировок.
- **Audit.** Стандартное HTTP-логирование (Serilog). Отдельно факт чтения рецепта не логируется.

---

## Реализация Этапа 2 — что в наличии

### Реализовано (MVP)

- Полный контракт `DishRecipeDto` + вложенные ViewDto.
- Snapshot-ветка работает: `IPublishedDishSnapshotReader` парсит jsonb из `Dish.PublishedVersionData` → ViewDto.
- Working-ветка работает: для автора/admin при отсутствии снепшота возвращается рабочий рецепт из основных таблиц.
- `404` для гостей/чужих пользователей при отсутствии снепшота.
- `404` для `Archived` всем.

### Отложено

- **Пер-блюдная маркировка Premium** — сейчас гейт закрывает функцию целиком, а не отдельные блюда.
- **Параметр `?version=working`** для явного запроса рабочей версии автором — Этап 5+ (UC-DSH-083 «Откатить мои несохранённые правки»).

---

## Связанные документы

- `docs/public/modules/dishes/domain-model.md` — двухслойная модель публикации, формат рецепта.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC модуля.
- `docs/public/modules/dishes/use-cases/UC-DSH-050-GetDishById.md` — карточка блюда (симметричная семантика выбора слоя).
- `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — Publish собирает снепшот, который читает этот UC.
- `docs/public/modules/dishes/use-cases/UC-DSH-053-GetMyDrafts.md` — списочное чтение черновиков.
- `docs/public/adr/ADR-0012-recipe-ingredient-discriminated-union.md` — полиморфизм `RecipeIngredient` на write и read.
- `docs/public/adr/ADR-0014-discriminated-unions-in-cqrs.md` — общий принцип DU в CQRS.
