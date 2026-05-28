# UC-DSH-053: Получить мои черновики

**Version:** 1.0 | **Date:** 2026-05-26

---

## Actors (Инициаторы)
- Primary: Авторизованный пользователь (любая роль кроме Guest).
- Secondary: —

---

## Resource (Ресурс)
- Entity: `Dish` (агрегат модуля Dishes).
- Identifier: текущий пользователь (`Dish.AuthorUserId == ActorUserId`).
- Action: Read (list).

---

## Security (Безопасность)

### Authentication (Аутентификация)
- Required.

### Authorization (Авторизация)
- Policy: `AuthorizationPolicies.VALID_ACTOR` — проверка валидного `sub` claim в JWT на уровне инфраструктуры.
- Roles: любая аутентифицированная роль.
- Ownership: пользователь видит **только свои** черновики. Фильтрация по `AuthorUserId == ActorUserId` встроена в репозиторный метод и не зависит от входных параметров запроса (нельзя запросить чужие черновики).

POL-001 (DishOwnership) здесь **не применяется** — он рассчитан на операции с конкретным `Dish`, а не на выборку списка.

### State Constraints (Ограничения по состоянию)
- Возвращаются только блюда с `Status = Draft`. Опубликованные (`Published`), снятые (`Unpublished`) и архивные (`Archived`) — не входят в выборку.

---

## API Contract (Контракт API)

### Endpoint

```
GET /api/dishes/my-drafts
```

### Request (Запрос)

Query Parameters:
- `page` — номер страницы, начиная с 1. По умолчанию `1`.
- `pageSize` — количество элементов на странице. Допустимый диапазон `1..25`. По умолчанию `5`.

Headers:
- `Authorization: Bearer <JWT>` — обязательный.

Body: отсутствует.

### Response (Ответ)
- Status: `200 OK`.
- Body: `GetMyDraftsResult`:
  - `items` — массив `DishDraftListItemDto` (может быть пустым).
  - `totalCount` — общее число черновиков пользователя без учёта пагинации.
  - `page` — возвращённый номер страницы.
  - `pageSize` — использованный размер страницы.

Состав одного элемента `DishDraftListItemDto`:

| Поле | Тип | Описание |
|------|-----|----------|
| `id` | `Guid` | Идентификатор блюда. |
| `slug` | `string` | URL-friendly идентификатор для построения ссылок в UI. |
| `name` | `string` | Отображаемое название. |
| `shortDescription` | `string?` | Краткая подводка для карточки списка. |
| `mainImageId` | `Guid?` | Идентификатор главного фото в Media. |
| `difficultyLevel` | `DifficultyLevel` | Сериализуется строкой (`"Medium"`, не `1`). |
| `costEstimate` | `CostEstimate` | Сериализуется строкой. |
| `dietLabelsMask` | `DietLabels` | Битовая маска диетических меток. |
| `allergensMask` | `AllergenType` | Битовая маска аллергенов, выведенная из состава. |
| `hasUnverifiedAllergens` | `bool` | `true`, если рецепт содержит freeform-ингредиенты и маска аллергенов может быть неполной. |
| `createdAt` | `DateTimeOffset` | Момент создания черновика. |
| `updatedAt` | `DateTimeOffset` | Момент последнего изменения автором. |

**Что НЕ возвращается в превью списка** (для детального просмотра — отдельный UC):
- Вложенный `Recipe` (intro, шаги, ингредиенты, Timing, Yield, Nutrition).
- Полные тексты `Description`, `HistoryText`.
- Денормализованная публичная статистика `RatingAvg`, `RatingCount`, `ViewsCount`, `FavoritesCount` — для черновика не имеет смысла.
- Снепшот публичной версии `PublishedVersionData`, `PublishedAt`, `PublishedVersionUpdatedAt` — у черновика всегда `null`.

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | `page < 1`, `pageSize < 1`, `pageSize > 25`. |
| 401 | — (отсутствует тело) | Запрос без `Authorization` или с невалидным/истёкшим JWT. |

---

## Preconditions (Предусловия)
- Пользователь аутентифицирован валидным JWT.
- В JWT присутствует claim `sub` с валидным `Guid`.

---

## Invariants (Инварианты домена)
- `Dish.AuthorUserId` иммутабельно — выборка по нему стабильна между запросами.
- Ни один черновик чужого автора не может попасть в результат (фильтрация в репозиторном методе, не управляется входом).

---

## Main Flow (Основной поток)
1. Контроллер собирает `GetMyDraftsQuery(Page, PageSize)` из query-параметров и отправляет через MediatR.
2. `ValidationBehavior` запускает `GetMyDraftsQueryValidator` — проверка диапазонов `Page` и `PageSize`. При нарушении — `Result.Failure(VALIDATION.ERROR)`, Handler не вызывается.
3. `GetMyDraftsQueryHandler` получает идентификатор автора через `ICurrentUserService.UserId` (гарантирован политикой `VALID_ACTOR`).
4. Handler вызывает `IDishRepository.ListDraftsByAuthorAsync(authorUserId, page, pageSize, ct)`. Репозиторий выполняет два запроса в одной сессии:
   - `COUNT(*) WHERE AuthorUserId = @userId AND Status = Draft`.
   - `SELECT ... ORDER BY UpdatedAt DESC OFFSET ((page - 1) * pageSize) LIMIT pageSize`. Используется `AsNoTracking()`. Подколлекции (`Recipe`, `Categories`, `Tags`) не подгружаются.
5. Handler маппит каждую сущность `Dish` в `DishDraftListItemDto` и возвращает `GetMyDraftsResult`.
6. Контроллер через базовый `MapResult<T>` отдаёт `200 OK` с телом ответа.

---

## Alternative Flows (Альтернативные потоки)
- AF-1 «Пустая выборка»: у автора нет ни одного черновика → возврат `200 OK`, `items = []`, `totalCount = 0`. Это нормальный успешный ответ, а не ошибка.
- AF-2 «Запрошенная страница за пределами выборки»: `page > ceil(totalCount / pageSize)` → возврат `200 OK`, `items = []`, корректные `totalCount`/`page`/`pageSize`. Решение по обработке такой ситуации остаётся за UI (показать «нет данных» или сбросить page = 1).

---

## Edge Cases (Граничные случаи)
- EC-1: Граничные значения параметров. `page = 1`, `pageSize = 25` — успех. `page = 0` или `pageSize = 26` — `400` с сообщением валидатора.
- EC-2: Конкурентное создание/удаление черновика в момент запроса. Между `COUNT` и `SELECT` пользователь мог создать или удалить черновик. Считается приемлемым: `totalCount` отражает состояние на момент `COUNT`, `items` — на момент `SELECT`; небольшая рассинхронизация допустима для read-only пагинации.
- EC-3: Свежевосстановленный из Archived в Draft (Этап 8+). Когда появится UC восстановления — такие блюда автоматически попадут в результат UC-DSH-053. Сейчас сценарий неактуален.

---

## Postconditions (Постусловия)
- Состояние БД не меняется (read-only).
- Никаких побочных эффектов (счётчики просмотров, аналитика и т.п. при списочной выборке черновиков не инкрементируются).

---

## Non-Functional (Нефункциональные требования)
- Idempotency: запрос идемпотентен — повторные вызовы с теми же параметрами при неизменном состоянии БД возвращают идентичный результат.
- Performance: целевое время ответа `< 100 мс` для типичного автора (десятки черновиков). Запрос использует индексы по `Dish.AuthorUserId` и `Dish.Status`.
- Consistency: strong consistency в рамках одной транзакции чтения (`COUNT` + `SELECT` выполняются последовательно без блокировок).
- Audit: запрос не логируется отдельно от стандартного HTTP-логирования (Serilog).

---

## Открытые расширения

- **Фильтры** (поиск по `Name`, `DifficultyLevel`, `CostEstimate` и т.п.) — будут добавлены при появлении соответствующей потребности UI. Точки расширения подготовлены: параметры добавляются в `GetMyDraftsQuery`, `GetMyDraftsQueryValidator` и сигнатуру `IDishRepository.ListDraftsByAuthorAsync`.
- **Поле `?withUnsavedChanges=true`** для отображения черновиков с несохранёнными правками относительно публичной версии (индикатор в личном кабинете) — потребует семантики «есть несохранённые правки» (`UpdatedAt > PublishedAt`), которая появится только после реализации UC-DSH-004 PublishDish. На текущем этапе у всех Draft-блюд `PublishedAt = NULL`, фильтр не имеет смысла.
- **Сортировка по выбору пользователя** (`CreatedAt`, `Name`) — текущий фиксированный порядок `UpdatedAt DESC` соответствует сценарию «продолжить работу над недавним». При появлении сценариев с другой сортировкой — параметр добавляется в Query без ломающих изменений контракта.
