# UC-DSH-050: Получить публичную карточку блюда по ID

**Version:** 1.0 (MVP — без snapshot-ветки; ожидает UC-DSH-004 PublishDish) | **Date:** 2026-05-28

---

## Actors (Инициаторы)

- Primary: Любой клиент — гость (без JWT) или аутентифицированный пользователь любой роли. Видимость варьируется в зависимости от того, является ли клиент автором блюда или `Admin`.
- Secondary: нет.

---

## Resource (Ресурс)

- Entity: `Dish` — публичная карточка без вложенного `Recipe` (рецепт — отдельный UC-DSH-052).
- Identifier: `Dish.Id` (`Guid`) — передаётся в path-параметре эндпоинта.
- Action: Read (single).

---

## Security (Безопасность)

### Authentication (Аутентификация)

Optional. Endpoint анонимный (без атрибута `[Authorize]`). Если JWT передан и валиден — `_currentUser.UserId` заполнен и используется для определения видимости рабочего слоя. Иначе — `_currentUser.UserId is null` (гость).

### Authorization (Авторизация)

Решение о видимости принимается в Handler-е, не на уровне политики. Условия:

- **Есть `PublishedVersionData`** — отдаётся всем (включая гостей). Для автора и `Admin` дополнительно поднимается флаг `HasUnsavedChanges`, если `UpdatedAt > PublishedAt`.
- **Нет `PublishedVersionData`** (статус `Draft` / `Unpublished`) — отдаётся только автору (`Dish.AuthorUserId == _currentUser.UserId`) или роли `Admin`. Остальные получают `404`.
- **`Status = Archived`** — `404` всем. Доступ admin к архивированным блюдам появится на Этапе 8+.

POL-001 здесь **не применяется** напрямую — это политика модификаций. Здесь — собственная логика чтения: ownership + статус + наличие снепшота.

### State Constraints (Ограничения по состоянию)

- `Dish.Status = Archived` → `404` всем (включая автора и admin).
- `Dish.Status ∈ {Draft, Unpublished}` без `PublishedVersionData` → видит только автор/admin.

### Contextual Constraints (Контекстуальные ограничения)

N/A на Этапе 2.

---

## API Contract (Контракт API)

### Endpoint

```
GET /api/dishes/{id}
```

### Request (Запрос)

**Path Parameters:**

- `id` — `Guid` идентификатор блюда.

**Headers:**

- `Authorization: Bearer <JWT>` — опционально. Без заголовка endpoint доступен (гость).

Body: отсутствует.

### Response (Ответ)

- Status: `200 OK`.
- Body: `DishDetailDto`.

Состав `DishDetailDto`:

| Поле | Тип | Описание |
|------|-----|----------|
| `id` | `Guid` | Идентификатор блюда. |
| `authorUserId` | `Guid` | Идентификатор автора (иммутабелен, не обнуляется при удалении аккаунта). |
| `name` | `string` | Отображаемое название. |
| `slug` | `string` | URL-friendly идентификатор для построения ссылок. |
| `shortDescription` | `string?` | Краткая подводка. |
| `description` | `string?` | Полное описание (markdown). |
| `historyText` | `string?` | Историко-культурный контекст. |
| `mainImageId` | `Guid?` | Идентификатор главного фото в Media. |
| `status` | `DishStatus` | Сериализуется строкой (`"Draft"`, `"Published"`, ...). Возвращается всем (не приватная мета). |
| `difficultyLevel` | `DifficultyLevel` | Сериализуется строкой. |
| `costEstimate` | `CostEstimate` | Сериализуется строкой. |
| `ownerType` | `OwnerType` | Сериализуется строкой. |
| `dietLabelsMask` | `DietLabels` | Битовая маска диетических меток. |
| `allergensMask` | `AllergenType` | Битовая маска аллергенов. |
| `hasUnverifiedAllergens` | `bool` | `true`, если есть freeform-ингредиенты. |
| `ratingAvg` | `decimal` | Средний рейтинг (0–5). |
| `ratingCount` | `int` | Количество оценок. |
| `viewsCount` | `long` | Количество просмотров. |
| `favoritesCount` | `int` | Количество добавлений в избранное. |
| `publishedAt` | `DateTimeOffset?` | Момент последней публикации. `null`, если никогда не публиковалось. |
| `createdAt` | `DateTimeOffset` | Момент создания. |
| `updatedAt` | `DateTimeOffset` | Момент последнего изменения автором. |
| `isPublishedVersion` | `bool` | `true`, если данные из `PublishedVersionData`; `false`, если из основных таблиц. |
| `hasUnsavedChanges` | `bool?` | Для автора/admin: `true`, если `UpdatedAt > PublishedAt`; для остальных — `null` (приватная информация). |

**Что НЕ возвращается** (отдельные UC):

- Вложенный `Recipe` со всеми составными частями → **UC-DSH-052 GetDishRecipe**.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | `DishId` пустой (`Guid.Empty`). На практике маршрут `{id:guid}` отсечёт невалидные значения на уровне ASP.NET Core. |
| 404 | `DISHES.DISH_NOT_FOUND` | Блюдо отсутствует, или `Status = Archived`, или нет `PublishedVersionData` и запрашивающий — не автор/admin. |

---

## Preconditions (Предусловия)

- HTTP-маршрут `GET /api/dishes/{id:guid}` корректно резолвится (ASP.NET Core валидирует формат `Guid` на уровне роутинга).

---

## Invariants (Инварианты домена)

- Чтение не меняет состояние агрегата.
- `Dish.AuthorUserId` иммутабельно — ownership-чек стабилен между запросами.
- Содержимое `DishDetailDto` отражает срез данных на момент чтения; никаких блокировок не накладывается.

---

## Main Flow (Основной поток)

1. **Запрос.** Клиент отправляет `GET /api/dishes/{id}` с опциональным JWT.
2. **Аутентификация.** ASP.NET Core middleware валидирует JWT, если передан. Без JWT — `HttpContext.User` остаётся `ClaimsPrincipal` без identity.
3. **Контроллер.** `DishesController.GetByIdAsync(Guid id, CancellationToken ct)`:
   1. Собирает `GetDishByIdQuery(DishId = id)`.
   2. Делегирует через `ISender.Send(query, ct)`.
4. **Валидация.** `ValidationBehavior` запускает `GetDishByIdQueryValidator` — проверка `DishId.NotEmpty()`.
5. **Handler — `GetDishByIdQueryHandler.Handle(...)`:**
   1. **Загрузка.** `Dish? dish = await _dishRepository.GetByIdAsync(request.DishId, ct)` — без `Recipe`.
   2. **NotFound / Archived.** Если `dish is null || dish.Status == DishStatus.Archived` → `return DishesErrors.DishNotFound` → `404`.
   3. **Определение пользователя.**
      - `currentUserId = _currentUser.UserId` (может быть `null`).
      - `isOwner = currentUserId.HasValue && currentUserId.Value == dish.AuthorUserId`.
      - `isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN)`.
      - `isOwnerOrAdmin = isOwner || isAdmin`.
   4. **Ветка «есть снепшот».** Если `dish.PublishedVersionData is not null`:
      - `hasUnsavedChanges = isOwnerOrAdmin ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value) : null`.
      - Возврат `Map(dish, isPublishedVersion: true, hasUnsavedChanges)`.
      - **Стаб на Этапе 2:** маппинг ведётся из текущих полей агрегата, а не из jsonb-снепшота. Ветка недостижима, пока UC-DSH-004 PublishDish не реализован. При появлении Publish — переключить источник данных на парсинг `PublishedVersionData`.
   5. **Ветка «нет снепшота».**
      - Если `!isOwnerOrAdmin` → `return DishesErrors.DishNotFound` → `404` (намеренно — не утечка существования черновика чужому пользователю).
      - Иначе возврат `Map(dish, isPublishedVersion: false, hasUnsavedChanges: false)`.
6. **Маппинг ответа.** `ApiController.MapResult<DishDetailDto>(Result<DishDetailDto>)` → `200 OK` с телом.

---

## Alternative Flows (Альтернативные потоки)

- **AF-1 «Гость читает опубликованное блюдо».** `PublishedVersionData != null` + анонимный запрос → `200 OK` с публичной версией; `isPublishedVersion = true`, `hasUnsavedChanges = null`. На Этапе 2 ветка недостижима без UC-DSH-004.
- **AF-2 «Автор читает свой черновик».** `PublishedVersionData = null` + `isOwner = true` → `200 OK` с рабочей версией; `isPublishedVersion = false`, `hasUnsavedChanges = false`. **Единственная активная ветка на Этапе 2.**
- **AF-3 «Автор читает опубликованное блюдо с правками».** `PublishedVersionData != null` + `isOwner = true` + `UpdatedAt > PublishedAt` → `200 OK` с публичной версией; `isPublishedVersion = true`, `hasUnsavedChanges = true`. UI рисует кнопку «Открыть черновик». Недостижима без UC-DSH-004.

---

## Edge Cases (Граничные случаи)

- **EC-1. Гость пытается прочитать чужой черновик.** `PublishedVersionData = null`, `isOwnerOrAdmin = false` → `404` (а не `403`). Намеренное поведение: ответ `403` подтвердил бы существование блюда; `404` единообразен с «не существует».
- **EC-2. Аутентифицированный пользователь читает чужой черновик.** Аналогично EC-1 — `404`.
- **EC-3. `Archived` блюдо.** `404` всем (включая автора). Это часть статусной модели Этапа 2; admin-доступ к архиву придёт на Этапе 8+.
- **EC-4. Маршрут с невалидным `Guid`.** ASP.NET Core роутинг (`{id:guid}`) отвергает запрос с `404` (не доходит до Handler).
- **EC-5. Параллельное изменение блюда между загрузкой и сериализацией.** Между `GetByIdAsync` и маппингом другой запрос мог обновить `Dish`. Считается приемлемым: возвращаются данные на момент `GetByIdAsync`.
- **EC-6. `Admin` читает свой собственный черновик.** Совмещение `isOwner = true` и `isAdmin = true` — обе роли активны; поведение идентично «обычному автору».
- **EC-7. `PublishedVersionData != null`, но `PublishedAt = null`.** Теоретически невозможно (доменная инвариантность Publish/Unpublish/Archive). В Handler-е выражение `hasUnsavedChanges` устойчиво: `dish.PublishedAt.HasValue && ...` отдаёт `false`. Стаб корректен по контракту, но `HasUnsavedChanges` будет всегда `false` для этой комбинации.

---

## Postconditions (Постусловия)

- Состояние БД не меняется.
- Никаких побочных эффектов: счётчик просмотров не инкрементируется (UC-DSH-070 IncrementDishViews ещё не реализован; при реализации он будет вызываться параллельно с UC-050).
- `DishDetailDto` сериализуется с enum-полями строками (через глобальный `JsonStringEnumConverter`).

---

## Non-Functional (Нефункциональные требования)

- **Idempotency.** Полностью идемпотентен — повторный вызов при неизменном состоянии возвращает идентичный результат.
- **Rate Limit.** Не реализован на Этапе 2. Endpoint публичный — в будущем потребует общего HTTP-уровневого ограничения для защиты от scraping.
- **Performance.** Целевое < 50 мс. Один SELECT по PK с `AsNoTracking` (через `GetByIdAsync`). После реализации UC-DSH-004 — добавится парсинг JSON-снепшота, что добавит ~1–5 мс на крупных рецептах.
- **Consistency.** Read committed (стандарт PostgreSQL). Без блокировок.
- **Audit.** Стандартное HTTP-логирование (Serilog). Отдельно факт чтения карточки не логируется.

---

## Реализация Этапа 2 — что в наличии и что отложено

### Реализовано (MVP)

- Ветка «нет снепшота, читает автор/admin» (AF-2) — полностью работает с корректным маппингом и флагами.
- `404` для гостей и чужих пользователей при отсутствии снепшота (EC-1, EC-2).
- `404` для `Archived` всем (EC-3).
- Полный контракт `DishDetailDto`, рассчитанный сразу под двухслойную модель.

### Не реализовано (зависит от UC-DSH-004 PublishDish)

- Ветка «есть снепшот» — в Handler-е помечена `TODO`. Сейчас источник данных — текущие поля агрегата; должен стать парсинг jsonb-снепшота `Dish.PublishedVersionData`. На Этапе 2 ветка недостижима (ни одно блюдо не опубликовано), но при появлении Publish этот переключатель — единственная правка, которая потребуется в UC-050.
- Параллельный вызов `UC-DSH-070 IncrementDishViews` — отдельный UC, появится позже.

---

## Связанные документы

- `docs/public/modules/dishes/domain-model.md` — двухслойная модель публикации, поля `Dish`, `PublishedVersionData`.
- `docs/public/modules/dishes/use-cases/README.md` — индекс UC модуля.
- `docs/public/modules/dishes/use-cases/UC-DSH-053-GetMyDrafts.md` — списочное чтение черновиков (симметричный Query).
- `docs/public/modules/dishes/use-cases/UC-DSH-051-GetDishBySlug.md` — будущий UC чтения по slug (только опубликованные).
- `docs/public/modules/dishes/use-cases/UC-DSH-052-GetDishRecipe.md` — будущий UC чтения рецепта.
- `docs/public/modules/dishes/use-cases/UC-DSH-004-PublishDish.md` — будущий UC, после которого UC-050 получит активную snapshot-ветку.
