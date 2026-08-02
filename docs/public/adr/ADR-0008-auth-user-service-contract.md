# ADR-0008: `IAuthUserService` как межмодульный контракт между Auth и Users

**Status:** Accepted
**Date:** 2026-04-04
**Updated:** 2026-08-02 — финализация при переносе в `docs/public/adr/`
**Stage:** 1

---

## Related (Связи)

- **Связанные ADR:** [ADR-0001](./ADR-0001-modular-monolith.md) (Modular Monolith — межмодульное взаимодействие через интерфейсы), [ADR-0002](./ADR-0002-clean-architecture.md) (Clean Architecture — направление зависимостей), [ADR-0004](./ADR-0004-postgresql-schema-per-module.md) (нет FK между схемами, связь через Id), [ADR-0007](./ADR-0007-media-polymorphic-entity-links.md) (`IMediaService` — тот же паттерн, известное отклонение по расположению контракта)
- **Связанные модули:** Auth (поставщик контракта), Users (потребитель)
- **Связанная документация:** `<wiki>/02_Архитектура.md` §8, `<wiki>/08_Разработка-(Development-Guide).md` (смена email/телефона/никнейма)
- **Связь с кодом:** `IAuthUserService` — `Auth.Domain/Contracts/`, `AuthUserService` — `Auth.Infrastructure/Services/`; `AuthHttpClient` — не реализован (точка замены при выделении Auth в микросервис)

---

## 1. Context (Контекст)

Модуль Users нуждается в доступе к данным Auth по двум сценариям:

**Сценарий 1 — Смена учётных данных.** Пользователь меняет `Email`, `PhoneNumber` или `UserName` через интерфейс профиля (модуль Users). Эти поля хранятся в `auth.AspNetUsers` (источник правды) и зеркалируются в `users.UserProfiles` (для отображения). Проверка уникальности и фактическое изменение должны происходить в Auth — нельзя дублировать эту логику в Users.

**Сценарий 2 — Получение ролей.** Users нужны роли пользователя (`auth.AspNetUserRoles`) для эндпоинта `GET /api/users/me/roles`. Users не имеет прямого доступа к Identity-таблицам.

Ограничение по ADR-0004: нет FK между схемами, нет навигационных свойств EF Core через границы модулей. Прямой доступ Users к `AuthDbContext` или `UserManager<ApplicationUser>` нарушил бы изоляцию модулей.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Прямая зависимость Users → Auth.Infrastructure

Users.Infrastructure ссылается на Auth.Infrastructure и напрямую вызывает `UserManager<ApplicationUser>` или `AuthDbContext`.

**Плюсы:** никаких промежуточных абстракций, минимум файлов.

**Минусы:** нарушает изоляцию модулей — два Infrastructure знают друг о друге, что запрещено ADR-0002 (Infrastructure модуля A → Infrastructure модуля B). При выделении Auth в микросервис — Users.Infrastructure придётся полностью переписать. Невозможно протестировать Users в изоляции (нужна реальная Identity-инфраструктура). Identity-типы (`ApplicationUser`) утекают в модуль Users.

**Причина отклонения:** прямое нарушение Dependency Rule ADR-0002 и принципа изоляции модулей ADR-0001.

### Вариант B — Интерфейс в Common.Domain или Common.Application

`IAuthUserService` объявляется в Shared Kernel (`Common.Domain` или `Common.Application`).

**Плюсы:** Users не зависит от Auth вообще — зависит только от Common.

**Минусы:** Common содержит только технические примитивы (`Entity`, `Result`, CQRS-абстракции) — бизнес-контракты конкретных модулей в Common создают семантическую путаницу. Common разрастается логикой, специфичной для пары Auth–Users. При добавлении аналогичных контрактов для других модулей Common превратится в сборник межмодульных интерфейсов.

**Причина отклонения:** нарушает назначение Shared Kernel как технического ядра без бизнес-логики.

### Вариант C — Событийный подход (Event-Driven)

Users публикует `EmailChangeRequestedEvent`, Auth подписывается, выполняет проверку и публикует `EmailChangedEvent` или `EmailChangeRejectedEvent`. Users подписывается на ответ и обновляет зеркало.

**Плюсы:** полная изоляция модулей на уровне кода — никаких прямых зависимостей.

**Минусы:** смена email — синхронная операция с точки зрения пользователя: он ждёт ответа «изменено» или «уже занято». Асинхронная обработка через события создаёт плохой UX (ответ «обрабатывается») и сложную обработку ошибок (что делать если Auth отклонил запрос уже после ответа Users клиенту). В рамках монолита с единой БД асинхронность лишена смысла — не существует сетевых задержек или недоступности сервисов.

**Причина отклонения:** избыточное усложнение для синхронной операции в монолите. Паттерн уместен для действительно асинхронных процессов (уведомления, заказы).

### Вариант D — Интерфейс в Auth.Domain/Contracts/ ⭐

`IAuthUserService` объявляется в `Auth.Domain/Contracts/` — публичный контракт модуля Auth для потребителей. Реализация `AuthUserService` — в `Auth.Infrastructure/Services/`. `Users.Domain` ссылается на `Auth.Domain` (только на интерфейс, не на реализацию). `Users.Application` вызывает `IAuthUserService` через DI.

**Плюсы:** синхронная операция — пользователь получает мгновенный ответ. Изоляция через интерфейс — Users не знает о `UserManager`, `ApplicationUser`, EF Core Identity. Единственная точка проверки уникальности. Тестируется через мок `IAuthUserService`. При выделении Auth в микросервис — реализация заменяется на `AuthHttpClient` без изменений в Users.

**Минусы:** `Users.Domain → Auth.Domain` — слабая, но всё же зависимость между модулями на уровне Domain. Допустимое исключение для монолита, явно зафиксированное в документации.

---

## 3. Decision (Принятое решение)

Объявить публичный контракт `IAuthUserService` в `Auth.Domain/Contracts/`. Реализацию `AuthUserService` разместить в `Auth.Infrastructure/Services/`. Зарегистрировать в DI как `AddScoped<IAuthUserService, AuthUserService>()` внутри `AddAuthModule()`.

### Контракт интерфейса

```csharp
// Auth.Domain/Contracts/IAuthUserService.cs
public interface IAuthUserService
{
    // Проверки уникальности
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByPhoneAsync(string phone, CancellationToken ct = default);
    Task<bool> ExistsByUserNameAsync(string userName, CancellationToken ct = default);

    // Изменение учётных данных (инициируется из Users)
    Task<Result> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken ct = default);
    Task<Result> ChangePhoneAsync(Guid userId, string newPhone, CancellationToken ct = default);
    Task<Result> ChangeUserNameAsync(Guid userId, string newUserName, CancellationToken ct = default);

    // Получение ролей
    Task<IReadOnlyCollection<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default);
}
```

### Поведение методов Change*

1. Найти пользователя по `userId` — если не найден, вернуть `AuthErrors.UserNotFound`.
2. Если новое значение совпадает с текущим — вернуть `Result.Success()` (идемпотентность, без записи).
3. Проверить уникальность нового значения среди **других** пользователей (`u.Id != userId`) — если занято, вернуть соответствующую ошибку.
4. Применить изменение через `UserManager` — вернуть `Result.Success()`.

### Протокол обновления зеркала в Users

После успешного вызова `IAuthUserService.Change*Async()` обработчик в Users вызывает `UserProfile.UpdateAuthMirrorData(...)` и сохраняет изменения в `users.UserProfiles`. Два шага в двух транзакциях — принятый компромисс (см. Consequences).

### Зависимость между проектами

```
Users.Domain.csproj → Auth.Domain.csproj  (только IAuthUserService, допустимое исключение)
Users.Application → IAuthUserService через DI (не знает о реализации)
Auth.Infrastructure → реализует IAuthUserService через UserManager<ApplicationUser>
```

---

## 4. Rationale (Обоснование)

1. **Синхронность операции.** Смена email — пользователь ждёт ответа «изменено» или «уже занято». Событийный подход (Вариант C) создал бы асинхронную семантику там, где она не нужна и вредна.

2. **Единственная точка проверки уникальности.** `auth.AspNetUsers` — источник правды для email/phone/userName. Проверять уникальность в Users означало бы дублировать логику и создавать race condition (два запроса одновременно пройдут проверку в разных таблицах). `IAuthUserService` гарантирует одно место проверки.

3. **Тестируемость.** Users.Application тестируется через мок `IAuthUserService` без Identity, `UserManager` и БД. 74 unit-теста в `Auth.UnitTests` и 106 тестов в `Users.UnitTests` работают в изоляции именно благодаря этой границе.

4. **Путь к микросервисам.** При выделении Auth в микросервис меняется только строка в DI: `AddScoped<IAuthUserService, AuthUserService>()` → `AddHttpClient<IAuthUserService, AuthHttpClient>(...)`. Ни один обработчик, доменный метод или тест в Users не затрагивается. Граница реализации проходит там, где была спроектирована.

5. **Паттерн стал типовым для проекта.** `IAuthUserService` — первый межмодульный контракт в `Domain/Contracts` поставщика; по тому же образцу реализованы `ISubscriptionAccessService` (`Subscriptions.Domain/Contracts`, Premium-гейт) и `IMediaService` (с зафиксированным отклонением по расположению — ADR-0007). Порты внешних систем (`IFileStorage`, `IPaymentGateway` из ADR-0017) следуют тому же принципу «интерфейс у потребителя семантики, реализация в Infrastructure».

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- Users.Application не импортирует ни одного Identity-типа — изоляция домена сохранена.
- При смене провайдера аутентификации (например, переход с ASP.NET Identity на Keycloak) — изменения только в `Auth.Infrastructure`, Users не трогается.
- Моки `IAuthUserService` дают полную изоляцию тестов Users без поднятия Identity-инфраструктуры.
- При выделении Auth в микросервис — одна строка в DI, нулевые изменения в Users.

### Negative / Trade-offs (Отрицательные / компромиссы)

- `Users.Domain → Auth.Domain` — допустимое, но зафиксированное исключение из правила «модули не зависят друг от друга на уровне Domain». В документации явно помечено как временное решение для монолита.
- Два шага в двух транзакциях: Auth меняет `auth.AspNetUsers`, Users меняет `users.UserProfiles`. При падении второго шага — рассинхронизация зеркала. Вероятность в монолите с единой БД минимальна; при необходимости — reconciliation job.
- `GetUserRolesAsync` добавляет запрос к Auth при каждом вызове `GET /api/users/me/roles` — нет кеширования ролей на стороне Users. Приемлемо для текущего масштаба.

### Areas of Caution (На что обратить внимание)

- При добавлении новых методов в `IAuthUserService` — проверить что реализация `AuthUserService` не возвращает Identity-специфичные типы (только `Result`, примитивы, `IReadOnlyCollection<string>`).
- Не допускать прямых ссылок `Users → Auth.Infrastructure` в .csproj — только `Users.Domain → Auth.Domain`.
- При реализации `AuthHttpClient` (при выделении Auth в микросервис) — обеспечить идентичное поведение идемпотентности (тихий успех при совпадении значений) и обработку ошибок.

---

## 6. Future Scope (Будущие направления)

- **При выделении Auth в микросервис:** создать `AuthHttpClient : IAuthUserService` в `Users.Infrastructure/ExternalServices/`. Добавить retry (Polly), circuit breaker, timeout. Добавить internal API в Auth: `PATCH /internal/users/{id}/email`, `PATCH /internal/users/{id}/phone`, `PATCH /internal/users/{id}/username`, `GET /internal/users/{id}/roles`. Защитить internal API межсервисной аутентификацией (API Key или mTLS).
- **Этап 5+ (Уведомления):** при изменении email добавить `UserProfile.RaiseDomainEvent(new UserEmailChangedEvent(...))` для отправки письма на старый адрес. Это потребует перевода `UserProfile` с `Entity<Guid>` на `AggregateRoot<Guid>` (одна строка).
- **Этап 8+ (Outbox):** если рассинхронизация зеркала станет проблемой — обернуть смену учётных данных в Saga или добавить reconciliation job.

---

## 7. Implementation Reference (Связь с кодовой базой)

- `src/Modules/Auth/GastronomePlatform.Modules.Auth.Domain/Contracts/IAuthUserService.cs` — интерфейс контракта.
- `src/Modules/Auth/GastronomePlatform.Modules.Auth.Infrastructure/Services/AuthUserService.cs` — реализация через `UserManager<ApplicationUser>` и `AuthDbContext`.
- `src/Modules/Auth/GastronomePlatform.Modules.Auth.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — регистрация `AddScoped<IAuthUserService, AuthUserService>()` внутри `AddAuthModule()`.
- `src/Modules/Users/GastronomePlatform.Modules.Users.Domain/GastronomePlatform.Modules.Users.Domain.csproj` — `<ProjectReference>` на `Auth.Domain` (зафиксированное исключение).
- `src/Modules/Users/GastronomePlatform.Modules.Users.Application/Commands/ChangeEmail/ChangeEmailCommandHandler.cs` — пример потребителя: вызов `IAuthUserService.ChangeEmailAsync()`, затем `UserProfile.UpdateAuthMirrorData()`.
- `src/Modules/Users/GastronomePlatform.Modules.Users.Application/Queries/GetUserRoles/GetUserRolesQueryHandler.cs` — потребитель `IAuthUserService.GetUserRolesAsync()`.

---

## История изменений

- **2026-04-04:** Accepted. Решение принято и реализовано на Этапе 1 (v0.6.0). `IAuthUserService`, `AuthUserService`, `ChangeEmail/Phone/UserNameCommandHandler` реализованы.
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`. Отмечено повторное применение паттерна (`ISubscriptionAccessService` на Этапе 3, `IMediaService` на Этапе 2 — ADR-0007).
