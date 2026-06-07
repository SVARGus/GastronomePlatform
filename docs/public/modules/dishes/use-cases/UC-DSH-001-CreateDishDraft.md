# UC-DSH-001: Создать черновик блюда

**Version:** 1.1 | **Date:** 2026-05-24

---

## Actors (Инициаторы)

- Primary: Авторизованный пользователь (любая роль, кроме Guest). Реальная роль (`USER` / `PREMIUM` / `CHEF` / `RESTAURANT` / `ADMIN`) определяет производное поле `Dish.OwnerType`, но не блокирует создание.
- Secondary: нет. На Этапе 2 модуль Media не задействован — `MainImageId` устанавливается отдельным сценарием `UC-DSH-011 ChangeDishMainImage` после реализации Media.

---

## Resource (Ресурс)

- Entity: `Dish` — новый агрегат каталога с автоматически создаваемым вложенным `Recipe` (1:1), `Timing` (1:1) и `Yield` (1:1).
- Identifier: `Id` (`Guid`) — генерируется фабрикой `Dish.Create(...)`, возвращается клиенту в теле ответа и заголовке `Location`.
- Action: Create.

---

## Security (Безопасность)

### Authentication (Аутентификация)

Required (JWT Bearer в заголовке `Authorization`).

### Authorization (Авторизация)

- Policy: **`AuthorizationPolicies.VALID_ACTOR`** — требует, чтобы JWT содержал claim `sub`, парсящийся как `Guid`. Применяется атрибутом `[Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]` на эндпоинте. Гарантирует валидный идентификатор пользователя на уровне инфраструктуры, без дублирования defense-in-depth проверок в Handler-е.
- `POL-001 Dish Ownership Policy` **не применяется** к созданию — она явно исключает Create из своей области (см. `POL-001-dish-ownership.md` §2.2). Любой пользователь с валидным актором может создать своё блюдо.
- Roles: любой `Authenticated`. Роль пользователя влияет на производное поле `Dish.OwnerType` (см. Main Flow §5.2), но не блокирует создание.
- Ownership: создаваемый ресурс автоматически становится собственностью автора — `Dish.AuthorUserId = ActorUserId`. Поле иммутабельно после создания.

### State Constraints (Ограничения по состоянию)

N/A — новый ресурс ещё не существует.

### Contextual Constraints (Контекстуальные ограничения)

N/A на Этапе 2. На Этапе 3+ возможны лимиты по тарифу подписки (например, `Free` — не более N черновиков), они появятся как отдельная проверка в Handler.

---

## API Contract (Контракт API)

### Endpoint

```
POST /api/dishes
```

### Request (Запрос)

**Headers:**

- `Authorization: Bearer <JWT>` — обязательный.
- `Content-Type: application/json` — обязательный.

**Body (JSON):**

| Поле | Тип | Обяз. | Ограничения | Описание |
|------|-----|-------|-------------|----------|
| `name` | `string` | ✅ | длина 3–200 | Отображаемое название блюда |
| `difficultyLevel` | `string` (enum) | ✅ | `Easy` \| `Medium` \| `Hard` \| `Pro` | Уровень сложности приготовления |
| `costEstimate` | `string` (enum) | ✅ | `Budget` \| `Moderate` \| `Expensive` | Грубая оценка стоимости |
| `shortDescription` | `string?` | ✗ | длина ≤ 500 | Краткая подводка для карточек каталога |
| `description` | `string?` | ✗ | длина ≤ 4000 | Полное «аппетитное» описание (markdown) |
| `dietLabelsMask` | `int?` | ✗ | биты из enum `DietLabels` | Битовая маска диетических меток |
| `historyText` | `string?` | ✗ | длина ≤ 4000 | Историко-культурный контекст |

**Не принимаются в Create** (устанавливаются отдельными UC):
- `MainImageId` → `UC-DSH-011 ChangeDishMainImage` (после реализации Media).
- `categoryIds[]` → `UC-DSH-007 SetCategories`.
- `tagIds[]` → `UC-DSH-008 SetTags`.
- Поля `Recipe` (`IntroductionText`, `AuthorTips`, etc.) → `UC-DSH-003 UpdateRecipe` и связанные.
- Шаги, ингредиенты, тайминг, выход, КБЖУ → отдельные UC-DSH-020..042.

`OwnerType` **не принимается** — определяется на сервере из ролей JWT (см. Main Flow §5.2).

`Slug` **не принимается** — генерируется на сервере из `Name` (см. Main Flow §5.3).

### Response (Ответ)

**Success:**

- Status: `201 Created`
- Headers: `Location: /api/dishes/{id}`
- Body:

```json
{
  "id": "8f3b9c1a-...",
  "slug": "borsh-ukrainskij-klassicheskij"
}
```

### Errors (Ошибки)

| HTTP Status | Код ошибки домена | Условие |
|-------------|-------------------|---------|
| 400 | `VALIDATION.ERROR` | Провал FluentValidation: `Name` вне диапазона 3–200, неизвестный enum, `DietLabelsMask` содержит неподдерживаемые биты, `Description`/`HistoryText` > 4000, `ShortDescription` > 500 |
| 400 | `DISHES.SLUG_GENERATION_EXHAUSTED` | AF-1 — превышен лимит в 30 попыток подобрать уникальный slug. Семантически серверная ошибка (500), но `ApiController.MapError` маппит `Failure → 400` |
| 401 | — | JWT отсутствует, просрочен или невалиден (отдаёт ASP.NET Core Authentication middleware до Handler) |
| 403 | — | Политика `VALID_ACTOR` не пропустила запрос — claim `sub` отсутствует или не парсится как `Guid`. Эту проверку выполняет инфраструктура авторизации до Handler-а |
| 500 | — | Конкурентная коллизия по `Slug` UNIQUE-индексу — на Этапе 2 принимаем как известное ограничение, см. EC-1 |

Domain-ошибок при `Dish.Create(...)` не возникает — фабрика не проверяет инвариантов, валидация полностью на уровне Application через FluentValidation.

---

## Preconditions (Предусловия)

- Пользователь аутентифицирован (валидный JWT в заголовке `Authorization`).
- `ICurrentUserService.UserId` имеет значение (claim `sub` присутствует и корректен).

---

## Invariants (Инварианты домена)

Гарантируются доменной моделью и фабрикой `Dish.Create(...)`:

- `Dish.AuthorUserId == ActorUserId` — устанавливается при создании, иммутабельно.
- `Dish.Status == DishStatus.Draft`.
- `Dish.ModerationStatus == ModerationStatus.Approved` (дефолт Этапа 2, на Этапе 8+ может стать `Pending`).
- `Dish.CreatedAt == Dish.UpdatedAt == utcNow` (одна точка времени из `IDateTimeProvider.UtcNow`).
- `Dish.Slug` уникален в рамках таблицы `dishes.Dishes` (UNIQUE-индекс).
- `Dish.MainImageId == null`.
- `Dish.PublishedAt == null`, `Dish.PublishedVersionData == null`, `Dish.PublishedVersionUpdatedAt == null`.
- `Dish.AllergensMask == AllergenType.None`, `Dish.HasUnverifiedAllergens == false`.
- `Dish.RatingAvg == 0`, `Dish.RatingCount == 0`, `Dish.ViewsCount == 0`, `Dish.FavoritesCount == 0`.
- В агрегате создан вложенный `Recipe` с `ServingsDefault = 1`, пустым `Timing` (`TotalTimeMinutes = 0`, `IsTotalManual = true`) и базовым `Yield`.
- Поднято доменное событие `DishCreatedEvent { DishId, AuthorUserId }`.

---

## Main Flow (Основной поток)

1. **Запрос.** Клиент отправляет `POST /api/dishes` с JWT и JSON-body.
2. **Аутентификация.** ASP.NET Core Authentication middleware валидирует JWT → заполняет `HttpContext.User` claims-ами (`sub`, `email`, `role`).
3. **Контроллер.** `DishesController.CreateDish(CreateDishDraftRequest body, CancellationToken ct)`:
   1. Собирает `CreateDishDraftCommand` из тела запроса.
   2. Делегирует MediatR через `ISender.Send(command, ct)`.
4. **Валидация.** `ValidationBehavior<CreateDishDraftCommand, Result<CreateDishDraftResult>>` запускает `CreateDishDraftCommandValidator`:
   - `Name`: `NotEmpty`, `Length(3, 200)`.
   - `DifficultyLevel`: `IsInEnum` (на уровне типа параметра достаточно, но добавим `Must` для устойчивости).
   - `CostEstimate`: аналогично.
   - `ShortDescription`: `MaximumLength(500)` (если задано).
   - `Description`: `MaximumLength(4000)` (если задано).
   - `HistoryText`: `MaximumLength(4000)` (если задано).
   - `DietLabelsMask`: `Must(mask => (mask & ~ValidFlags) == 0)` — все биты в пределах enum `DietLabels`.
5. **Handler — `CreateDishDraftCommandHandler.Handle(...)`:**
   1. **ActorUserId.** `actorUserId = _currentUser.UserId!.Value`. Гарантирован валидным политикой `VALID_ACTOR` на эндпоинте — Handler не выполняет дополнительной defense-in-depth проверки.
   2. **OwnerType.** Определяет через `OwnerTypeResolver.ResolveFromRoles(_currentUser.Roles)`. Приоритет:
      - содержит `PlatformRoles.RESTAURANT` → `OwnerType.Restaurant`;
      - иначе содержит `PlatformRoles.CHEF` → `OwnerType.Chef`;
      - иначе → `OwnerType.User`.
   3. **Slug — базовая генерация.** `baseSlug = _slugGenerator.Generate(command.Name)`. При пустом результате — `baseSlug = "dish-" + Guid.NewGuid().ToString("N").Substring(0, 8)` (AF-2).
   4. **Slug — разрешение коллизий.** Цикл (максимум 30 итераций):
      - Если `await _dishRepository.SlugExistsAsync(slug, ct) == false` → выход из цикла, slug принят.
      - Иначе → `slug = $"{baseSlug}-{++attempt}"` (начиная с `-2`).
      - Если итераций > 30 → `Result.Failure(DishesErrors.SlugGenerationExhausted)` — теоретическая защита.
   5. **utcNow.** `var utcNow = _clock.UtcNow`.
   6. **Создание агрегата.** `var dish = Dish.Create(actorUserId, command.Name, slug, command.DifficultyLevel, command.CostEstimate, ownerType, utcNow)`. Поднимается `DishCreatedEvent`.
   7. **Опциональные поля карточки.** Если задано хотя бы одно из `ShortDescription` / `Description` — вызов `dish.UpdateCard(name, shortDescription, description, difficultyLevel, costEstimate, ownerType, utcNow)`. Поднимается `DishUpdatedEvent`.
   8. **Опциональные диетические метки.** Если `DietLabelsMask` задана — отдельный вызов `dish.SetDietLabels(command.DietLabelsMask.Value, noConflicts, utcNow)`, где `noConflicts` — пустой словарь (рецепт черновика пуст, конфликтов быть не может; Reject-проверка ADR-0016 проходит по структурному инварианту). `DietLabelsMask` намеренно не часть `UpdateCard` — это constrained declaration с проверкой совместимости с составом ингредиентов через UC-DSH-009 (ADR-0016). Поднимается `DishUpdatedEvent`.
   9. **Опциональная история.** Если `HistoryText` задано — `dish.UpdateHistory(command.HistoryText, utcNow)`. Поднимается `DishUpdatedEvent`.
   10. **Сохранение.** `await _dishRepository.AddAsync(dish, ct); await _dishRepository.SaveChangesAsync(ct);` — один транзакционный коммит (Unit of Work).
   11. **Доменные события.** После сохранения собранные `dish.DomainEvents` публикуются вручную через `IPublisher`, затем `dish.ClearDomainEvents()`.
   12. **Результат.** `return Result.Success(new CreateDishDraftResult(dish.Id, dish.Slug))`.
6. **Маппинг ответа.** `ApiController` (базовый класс):
   - `Result.Success(value)` для Create → `201 Created` + `Location: /api/dishes/{value.Id}` + JSON-body `value`.
   - `Result.Failure(error)` → `ErrorType → HTTP Status` по правилам ApiController (`Validation → 400`, `Failure → 500`, и т.д.).

---

## Alternative Flows (Альтернативные потоки)

- **AF-1. Slug-коллизия (одиночная или последовательная).** Условие: `await _dishRepository.SlugExistsAsync(baseSlug) == true`. → Handler добавляет суффикс `-2`, повторяет проверку. При повторной коллизии — `-3`, и так далее. Итог: блюдо создаётся с уникальным slug вида `borsh-ukrainskij-2`. Лимит итераций — 30 (защита от бага в генераторе или экзотических сценариев; превышение возвращает `DishesErrors.SlugGenerationExhausted`).
- **AF-2. Slug-генератор вернул пустую строку.** Условие: после `_slugGenerator.Generate(name)` результат `""` или `null` (например, `Name` состоит из эмодзи, спецсимволов или нестандартных символов, для которых нет правил транслита). → Использовать fallback `"dish-{Guid.NewGuid().ToString("N").Substring(0, 8)}"`. Коллизии маловероятны (теоретическая вероятность < 2⁻³²), но всё равно проходят через цикл AF-1.

---

## Edge Cases (Граничные случаи)

- **EC-1. Конкурентные запросы с одинаковым `Name`.** Два запроса A и B с одинаковым `Name`, выполняются параллельно. Оба генерируют один базовый slug → оба видят `SlugExistsAsync == false` → оба пытаются `INSERT` с одинаковым slug → второй получит `DbUpdateException` от UNIQUE-constraint на `dishes.Dishes.Slug`. **Ожидаемое поведение Этапа 2:** ошибка пробрасывается, `GlobalExceptionHandlingMiddleware` отдаёт `500 Internal Server Error`. На Этапе 4+ — отдельная обработка `DbUpdateException → 409 Conflict + retry на уровне Handler`. Сейчас принимаем как известное ограничение (вероятность сценария низкая при одиночном пользователе и небольшой нагрузке).
- **EC-2. JWT валиден, но `UserId` claim отсутствует или невалиден.** Сценарий: токен прошёл валидацию подписи в Authentication middleware, но claim `sub` отсутствует / имеет невалидный формат. → Политика `AuthorizationPolicies.VALID_ACTOR` на эндпоинте не пропускает запрос, инфраструктура авторизации отдаёт `403 Forbidden` до Handler-а. В самом Handler-е defense-in-depth проверка не нужна — `_currentUser.UserId!.Value` гарантированно валиден.
- **EC-3. `Name` содержит только пробелы или управляющие символы.** → `Validator` провалит правило `NotEmpty().Length(3, 200)` (после `Trim`-проверки в FluentValidation). 400 `VALIDATION.ERROR`.
- **EC-4. `DietLabelsMask` имеет неподдерживаемые биты.** Например, передан `999999` — не соответствует определённым в enum `DietLabels` флагам. → `Validator` проверяет: `(mask & ~ValidDietLabelsMask) == 0`. При нарушении — 400 `VALIDATION.ERROR`.
- **EC-5. Слишком длинный `Name` (например, 10000 символов).** → ASP.NET Core отклонит запрос на стадии model binding (если превышены лимиты `MaxRequestBodySize`) или `Validator` отдаст 400. В обычной ситуации `Length(3, 200)` ловит первым.

---

## Postconditions (Постусловия)

При успехе (Status 201):

- В таблице `dishes.Dishes` появилась 1 новая запись с `Id`, `Slug`, `Name`, `Status = Draft`, `AuthorUserId = ActorUserId`, всеми обязательными полями.
- В таблицах `dishes.Recipes`, `dishes.Timings`, `dishes.Yields` — по 1 новой записи (вложенные части агрегата).
- В журнале логов записан INFO-уровень: `"Dish created. Id={DishId}, Slug={Slug}, AuthorUserId={AuthorUserId}"` с `CorrelationId` (автоматически из `CorrelationIdMiddleware`).
- Доменное событие `DishCreatedEvent` опубликовано через MediatR `IPublisher`. Если применялись опциональные поля — также опубликованы 1–2 `DishUpdatedEvent`. На Этапе 2 нет подписчиков — события «выстреливают вхолостую». На Этапе 5+ появятся handlers (индексация в поиске, кэш, статистика).

При неуспехе (любой не-2xx статус):

- Подразумевается отсутствие изменений в системе — `_dishRepository.AddAsync(...)` без `SaveChangesAsync(...)` не коммитит, EF Core отбрасывает отслеживаемые изменения вместе со scope-ом DbContext (Scoped lifetime в DI).

---

## Non-Functional (Нефункциональные требования)

- **Idempotency:** не идемпотентно — повторный запрос с теми же данными создаст новое блюдо с новым `Id` и новым `Slug` (с суффиксом `-2`, `-3`, и т.д. через AF-1). Если идемпотентность нужна для UI (защита от double-submit) — реализовать через `Idempotency-Key` заголовок на Этапе 4+.
- **Rate Limit:** не реализован на Этапе 2 (перенесён в Этап 4 по дорожной карте). На Этапе 4+ — общий rate limit на `/api/*` через `AddRateLimiter()`, например, 100 запросов / минуту / IP. Защита от автоматического создания спам-блюд.
- **Performance:** целевое < 100 мс при условии ≤ 5 итераций slug-коллизий. Профиль запросов:
  - 1 SELECT на `Dish.Slug = ?` для проверки коллизии (B-tree индекс).
  - 0–N дополнительных SELECT при коллизиях (в среднем 0).
  - 1 INSERT для `Dish` + по 1 INSERT для `Recipe`, `Timing`, `Yield`.
  - 1 коммит транзакции.
- **Consistency:** strong consistency в рамках одной транзакции — `Dish`, `Recipe`, `Timing`, `Yield` создаются атомарно. Доменные события публикуются через `IPublisher` синхронно в том же scope — после `SaveChangesAsync` (в Common.Application через MediatR pipeline).
- **Audit:** логирование через Serilog (`ILogger<CreateDishDraftCommandHandler>`). Структурированные поля: `DishId`, `AuthorUserId`, `Slug`. `CorrelationId` пробрасывается автоматически через `LogContext` в `CorrelationIdMiddleware`.

---

## Implementation Notes (Карта артефактов реализации)

> Раздел сверх стандартного шаблона — карта где что будет лежать. Помогает реверс-навигации «UC → код».

### Application (новые файлы)

```
src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Application/
├── Commands/
│   └── CreateDishDraft/
│       ├── CreateDishDraftCommand.cs           # ICommand<CreateDishDraftResult> — record-параметры
│       ├── CreateDishDraftCommandHandler.cs    # ICommandHandler<CreateDishDraftCommand, CreateDishDraftResult>
│       ├── CreateDishDraftCommandValidator.cs  # AbstractValidator<CreateDishDraftCommand>
│       └── CreateDishDraftResult.cs            # record (Guid Id, string Slug)
│
└── Helpers/
    └── OwnerTypeResolver.cs                    # internal static — ResolveFromRoles(IReadOnlyCollection<string>) → OwnerType. Общий для UC-DSH-001, UC-DSH-002 и будущих UC, меняющих OwnerType
```

> Следуем эталонной структуре модулей (см. `08_Разработка-(Development-Guide).md` §1): папка `Commands/` (не `UseCases/`), внутри — папка по имени UC с тремя файлами + опциональный `Result`. `DTOs/` зарезервированы под переиспользуемые DTO (как `UserProfileDto` в Users). `Helpers/` — для статических утилит уровня Application, не подходящих под Command/Query/Behavior.

### Infrastructure (новые файлы)

```
src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Infrastructure/
└── Repositories/
    └── DishRepository.cs                       # IDishRepository — первая реализация (создана по правилу «вместе с первым UC-потребителем»)
```

> `Repositories/` лежит **на одном уровне с `Persistence/`**, не внутри (см. эталон Users.Infrastructure и Auth.Infrastructure).

### Common (новые файлы — общие абстракции и реализация slug)

```
src/Common/
├── GastronomePlatform.Common.Application/
│   └── Abstractions/
│       └── ISlugGenerator.cs                   # Контракт: string Generate(string source)
│
└── GastronomePlatform.Common.Infrastructure/
    └── Services/
        └── SlugGenerator.cs                    # Реализация: ASCII-only (транслит ru→lat по таблице), lowercase, дефисы между словами, удаление спецсимволов и эмодзи. sealed, Singleton
```

> **Стратегия транслита на Этапе 2 — вариант A (ASCII-only).** Альтернативный подход (мультиязычные slug-и — UTF-8 кириллица для русскоязычной локали, отдельный английский slug для англоязычной) — на Этап 8+, см. TODO 2.15 в `docs/_private/private_TODO-будущие-этапы.md`.

### WebAPI (правки существующих)

```
src/GastronomePlatform.WebAPI/
└── Controllers/
    └── Dishes/
        └── DishesController.cs                 # + метод CreateDish (POST /api/dishes), + вложенный record CreateDishDraftRequest
```

### DI-регистрация

- В `Common.Infrastructure.ServiceCollectionExtensions.AddCommonInfrastructure()` — добавить `services.AddSingleton<ISlugGenerator, SlugGenerator>()`.
- В `Dishes.Infrastructure.ServiceCollectionExtensions.AddDishesModule()` — добавить `services.AddScoped<IDishRepository, DishRepository>()`.

### Тестирование

Unit-тесты модуля Dishes отложены в техдолг ВКР по решению из v0.8.0. Этот UC покрытием тестов не сопровождается на Этапе 2.

---

## Связанные документы

- `docs/public/modules/dishes/domain-model.md` — определение агрегата `Dish` и связанных сущностей.
- `docs/public/policies/POL-001-dish-ownership.md` — общая политика модификации блюд (на UC-DSH-001 не действует, но определяет правила для последующих UC).
- `docs/public/modules/dishes/use-cases/README.md` — индекс всех UC модуля Dishes.
