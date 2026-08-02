# ADR-0003: CQRS + MediatR как модель обработки запросов

**Status:** Accepted
**Date:** 2026-03-04
**Updated:** 2026-08-02 — финализация при переносе в `docs/public/adr/`; добавлена заметка про формат positional record для доменных событий (Areas of Caution)
**Stage:** 0

---

## Related (Связи)
- **Связанные ADR:** [ADR-0001](./ADR-0001-modular-monolith.md) (события — асинхронный канал между модулями), [ADR-0002](./ADR-0002-clean-architecture.md) (Clean Architecture — CQRS живёт в Application-слое), [ADR-0014](./ADR-0014-discriminated-unions-in-cqrs.md) (Discriminated Unions в CQRS — расширение паттерна)
- **Связанные модули:** все — каждый модуль использует ICommand/IQuery для use cases
- **Связанная документация:** `<wiki>/02_Архитектура.md` §3 (CQRS-паттерн), `<wiki>/08_Разработка-(Development-Guide).md` (валидация через FluentValidation + MediatR Pipeline Behavior)

---

## 1. Context (Контекст)

В рамках Clean Architecture (ADR-0002) Application-слой оркестрирует use cases. Нужен механизм, который:

- Разделяет операции записи (создать блюдо, отменить заказ) и чтения (получить блюдо, список заказов) — они имеют разные требования к оптимизации, валидации и кешированию.
- Обеспечивает единый конвейер обработки запроса: валидация → авторизация → бизнес-логика → публикация событий — без дублирования кода в каждом обработчике.
- Разъединяет контроллер и бизнес-логику: контроллер не знает, какой класс обрабатывает запрос, — он отправляет «сообщение» и получает результат.
- Позволяет публиковать доменные события и обрабатывать их подписчиками без прямых вызовов между модулями.

Платформа .NET имеет де-факто стандартное решение для этих задач — библиотеку MediatR.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — Прямые вызовы сервисов из контроллера

Контроллер вызывает `DishService.CreateAsync(...)` напрямую. Сервис содержит бизнес-логику, валидацию, обращение к репозиторию.

**Плюсы:** прямолинейно, нет дополнительных абстракций, минимум boilerplate.
**Минусы:** нет единого конвейера — валидацию, логирование, авторизацию приходится вызывать в каждом методе вручную. Невозможно централизованно добавить cross-cutting concern (например, автоматическую валидацию всех входящих команд). Контроллер жёстко привязан к конкретному сервису. Публикация событий — ручная в каждом методе.

**Причина отклонения:** при 60+ use cases в одном модуле (Dishes) дублирование cross-cutting логики становится неуправляемым. Нет механизма pipeline behaviors.

### Вариант B — Собственная реализация Mediator-паттерна

Написать свой диспатчер: интерфейсы `ICommand`/`IQuery`, ручное сканирование обработчиков, свой pipeline.

**Плюсы:** полный контроль, обучающий эффект, нет зависимости от сторонней библиотеки.
**Минусы:** время на разработку, тестирование и поддержку. Нет экосистемы (Pipeline Behaviors, Notification Handlers). При подключении нового разработчика — нестандартное решение, которое нужно изучать.

**Причина отклонения:** обсуждено в начале Этапа 0. Разработчик выбрал MediatR как промышленный стандарт, но с подробными объяснениями работы библиотеки для обучения.

### Вариант C — MediatR с CQRS-абстракциями и Pipeline Behaviors ⭐

MediatR как диспатчер. Собственные обёртки (`ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`) поверх `IRequest<T>` и `IRequestHandler<T, R>` для семантической ясности. `Result<T>` как единый тип возврата. Pipeline Behaviors для cross-cutting concerns (валидация, логирование).

**Плюсы:** стандарт .NET-экосистемы, богатая функциональность (Behaviors, Notifications), минимум кода (обёртки — по 3 строки). Контроллер знает только `ISender.Send(command)` — полная развязка. Notifications для доменных событий.
**Минусы:** зависимость от сторонней библиотеки. Дополнительная абстракция поверх прямого вызова (незначительный overhead). Debugging: стек-трейс проходит через MediatR pipeline.

---

## 3. Decision (Принятое решение)

Использовать **MediatR** как реализацию паттерна Mediator с разделением операций на **Commands** (запись) и **Queries** (чтение).

### CQRS-абстракции (Common.Application)

```
ICommand : IRequest<Result>                    — команда без возвращаемого значения
ICommand<TResponse> : IRequest<Result<TResponse>> — команда с возвращаемым значением
IQuery<TResponse> : IRequest<Result<TResponse>>  — запрос (всегда возвращает данные)

ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
```

Все обработчики возвращают `Result` или `Result<T>` — исключения не используются для бизнес-ошибок.

### MediatR Pipeline

```
HTTP Request → Controller → ISender.Send(command)
    ↓
MediatR Pipeline:
    1. ValidationBehavior     — FluentValidation (автоматически, до handler'а)
    2. [будущее: LoggingBehavior, AuthorizationBehavior]
    3. CommandHandler / QueryHandler
    ↓
Result<T> → Controller → HTTP Response
```

### Доменные события (Notifications)

`IDomainEvent : INotification` — доменные события диспатчатся через MediatR. `AggregateRoot` накапливает события через `RaiseDomainEvent()`, Infrastructure диспатчит их после `SaveChanges`. `TaskWhenAllPublisher` — параллельная обработка подписчиками.

### Регистрация

Каждый модуль регистрирует свои обработчики через `RegisterServicesFromAssembly` и валидаторы через `AddValidatorsFromAssembly` в `Add{ModuleName}Module()`.

### Разделение пакетов

- `MediatR.Contracts` — в Common.Domain (только `INotification` для `IDomainEvent`)
- `MediatR` (полный) — в Common.Application (интерфейсы обработчиков, pipeline)
- `FluentValidation` — в Common.Application и модулях (валидаторы)

---

## 4. Rationale (Обоснование)

1. **Единый конвейер.** `ValidationBehavior` автоматически перехватывает все команды и запросы, находит валидаторы через DI, запускает валидацию **до** обработчика. Новый валидатор добавляется одним файлом рядом с командой — без изменений в pipeline или контроллере.

2. **Развязка Controller ↔ Handler.** Контроллер вызывает `_sender.Send(new CreateDishCommand(...))` и не знает, какой класс обрабатывает команду. Замена обработчика, добавление декоратора или перенос в другой модуль не трогает контроллер.

3. **Доменные события без связанности.** Модуль Auth публикует `UserRegisteredEvent`, модуль Users подписывается через `INotificationHandler<UserRegisteredEvent>`. Auth не знает о Users. При выделении в микросервисы — `INotification` заменяется на RabbitMQ-событие без изменения подписчика.

4. **Result вместо исключений.** Все обработчики возвращают `Result<T>`. Контроллер маппит `ErrorType` → HTTP Status Code в базовом `ApiController`. Исключения — только для инфраструктурных сбоев (БД недоступна), перехватываются `GlobalExceptionHandlingMiddleware`.

5. **Стандарт экосистемы.** MediatR используется в подавляющем большинстве .NET Clean Architecture проектов. Любой .NET-разработчик узнает паттерн.

---

## 5. Consequences (Последствия)

### Positive (Положительные)
- Добавление нового use case = один record (команда) + один класс (handler) + опциональный валидатор. Минимум церемонии.
- Cross-cutting concerns (валидация, будущее логирование) добавляются один раз в pipeline — работают для всех обработчиков автоматически.
- Доменные события обрабатываются параллельно (`TaskWhenAllPublisher`) — сбой одного подписчика не блокирует другие.
- ~240 unit-тестов к Этапу 2 — обработчики тестируются без HTTP-контекста.

### Negative / Trade-offs (Отрицательные / компромиссы)
- Стек-трейс при отладке проходит через MediatR internals — менее читаемый, чем прямой вызов.
- Overhead: reflection при сканировании сборок, DI-резолв обработчика на каждый запрос. На практике незначителен (микросекунды).
- `ValidationBehavior` использует рефлексию для поддержки `Result` и `Result<T>` — fallback бросает `InvalidOperationException` при неподдерживаемом `TResponse`.
- Известное расхождение формата: ошибки валидации (400) сериализуются как объект `Error`, а не ProblemDetails (RFC 7807). Приемлемо для дипломного проекта; улучшение запланировано.

### Areas of Caution (На что обратить внимание)
- Валидаторы кладутся рядом с командой в Application (не в Domain, не в Infrastructure).
- Команды — `record` (immutable). Не использовать `class` с `set`-свойствами.
- **Формат доменных событий — positional record с init-timestamp.** Все доменные события в проекте — `public sealed record XxxEvent(...positional-параметры...) : IDomainEvent`, где positional-параметры несут бизнес-данные (`DishId`, `AuthorUserId` и т.п.), а `OccurredOn` и `EventId` — init-свойства с дефолтами `DateTimeOffset.UtcNow` и `Guid.NewGuid()` в теле record. Init-свойства в теле — сознательный обход ограничения C# на compile-time-константы в default-значениях позиционных параметров: `DateTimeOffset.UtcNow` не является константой и не допустима как `default(...)`. Пример: `public sealed record DishCreatedEvent(Guid DishId, Guid AuthorUserId) : IDomainEvent { public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow; public Guid EventId { get; init; } = Guid.NewGuid(); }`. Единый шаблон для всех модулей — при добавлении нового события сверяться с уже существующими (`UserRegisteredEvent`, `DishCreatedEvent`, `DishPublishedEvent`).
- `IQuery` не должен изменять состояние — это инвариант CQRS. Если query handler пишет в БД — это баг.
- При добавлении нового модуля не забывать `RegisterServicesFromAssembly` в `Add{ModuleName}Module()` — иначе MediatR не найдёт обработчики.

---

## 6. Future Scope (Будущие направления)

- **Этап 8+:** `LoggingBehavior` — автоматическое логирование входа/выхода каждого обработчика с CorrelationId и временем выполнения.
- **Этап 8+:** `AuthorizationBehavior` — централизованная проверка прав доступа через pipeline (вместо `[Authorize]` на контроллерах для бизнес-правил).
- **Этап 8+:** Outbox Pattern — доменные события пишутся в outbox-таблицу в той же транзакции, фоновый процесс отправляет в RabbitMQ. Замена `TaskWhenAllPublisher` на outbox publisher.
- **При выделении в микросервисы:** `INotification` → RabbitMQ Integration Events. Обработчики остаются, меняется только механизм доставки.

---

## 7. Implementation Reference (Связь с кодовой базой)

- `src/Common/GastronomePlatform.Common.Application/Messaging/` — `ICommand.cs`, `IQuery.cs`, `ICommandHandler.cs`, `IQueryHandler.cs`.
- `src/Common/GastronomePlatform.Common.Application/Behaviors/ValidationBehavior.cs` — MediatR Pipeline Behavior для FluentValidation.
- `src/Common/GastronomePlatform.Common.Domain/Events/IDomainEvent.cs` — `IDomainEvent : INotification`.
- `src/Common/GastronomePlatform.Common.Domain/Primitives/AggregateRoot.cs` — `RaiseDomainEvent()`, `ClearDomainEvents()`.
- `src/GastronomePlatform.WebAPI/Program.cs` — `AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies(...); cfg.NotificationPublisher = new TaskWhenAllPublisher(); })`.
- Пример handler'а: `src/Modules/Auth/GastronomePlatform.Modules.Auth.Application/Commands/Register/RegisterCommandHandler.cs`.
- Пример валидатора: `src/Modules/Auth/GastronomePlatform.Modules.Auth.Application/Commands/Register/RegisterCommandValidator.cs`.
- Примеры доменных событий (positional record + init-timestamp): `src/Modules/Auth/GastronomePlatform.Modules.Auth.Domain/Events/UserRegisteredEvent.cs`, `src/Modules/Dishes/GastronomePlatform.Modules.Dishes.Domain/Events/DishCreatedEvent.cs`.

---

## История изменений

- **2026-03-04:** Accepted. CQRS-абстракции реализованы в Common.Application на Этапе 0.
- **2026-04-18:** ValidationBehavior + FluentValidation добавлены на Этапе 1 (v0.7.0).
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`; Implementation Reference сверен с кодом (`TaskWhenAllPublisher` в `Program.cs`, пути Messaging/Behaviors). Добавлена заметка про формат positional record для доменных событий (Areas of Caution) — систематизация паттерна, применённого от `UserRegisteredEvent` (Этап 1) до всех событий модуля Dishes (Этап 2); мотивация — обход ограничения C# на compile-time-константы в default-значениях позиционных параметров записи.
