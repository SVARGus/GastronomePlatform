# ADR-0002: Clean Architecture как внутренняя организация модулей

**Status:** Accepted
**Date:** 2026-03-04
**Updated:** 2026-08-02 — финализация при переносе в `docs/public/adr/`
**Stage:** 0

---

## Related (Связи)

- **Связанные ADR:** [ADR-0001](./ADR-0001-modular-monolith.md) (Modular Monolith — внешняя структура; данный ADR — внутренняя), [ADR-0003](./ADR-0003-cqrs-mediatr.md) (CQRS + MediatR — модель Application-слоя), [ADR-0008](./ADR-0008-auth-user-service-contract.md) (межмодульные контракты в `Domain/Contracts`), [ADR-0007](./ADR-0007-media-polymorphic-entity-links.md) (известное отклонение: контракт `IMediaService` в Application), [ADR-0019](./ADR-0019-dish-updated-at-interceptor.md) (пример инфраструктурной логики, не проникающей в Domain)
- **Связанные модули:** все — каждый модуль следует этой организации
- **Связанная документация:** `<wiki>/02_Архитектура.md` §3 (архитектурные паттерны), `<wiki>/13_Структура-проекта.md` (зависимости между проектами), `<wiki>/08_Разработка-(Development-Guide).md` (шаблон модуля)

---

## 1. Context (Контекст)

В ADR-0001 принято решение строить систему как модульный монолит из 8 бизнес-модулей + Shared Kernel. Необходимо определить, как организовать код **внутри** каждого модуля, чтобы:

- Бизнес-логика была изолирована от инфраструктурных деталей (БД, HTTP, файловая система).
- Код был тестируемым: unit-тесты доменной и прикладной логики без поднятия БД или HTTP-сервера.
- При выделении модуля в микросервис менялась только инфраструктура, а не бизнес-логика.
- Junior-разработчик мог по структуре папок и проектов понять, куда класть новый код.

Проект на .NET 8 / ASP.NET Core, где Clean Architecture — де-факто стандарт enterprise-приложений.

---

## 2. Considered Alternatives (Рассмотренные альтернативы)

### Вариант A — N-Layer (классический трёхслойный)

Presentation → Business Logic → Data Access, все слои в одном-двух проектах.

**Плюсы:** простота, привычность, минимум проектов.
**Минусы:** Business Logic зависит от Data Access (прямые вызовы EF Core из сервисов). Смена ORM требует переписывания бизнес-логики; тестирование бизнес-правил невозможно без мока БД; нет разделения «что система делает» (use cases) и «как» (инфраструктура).

**Причина отклонения:** зависимость бизнес-логики от инфраструктуры делает код хрупким и плохо тестируемым; при выделении в микросервис пришлось бы переписывать бизнес-слой.

### Вариант B — Vertical Slice Architecture

Каждый use case — отдельная папка со всеми слоями; горизонтального разделения нет.

**Плюсы:** высокая когезия внутри фичи, минимальные конфликты при параллельной разработке, легко удалить фичу целиком.
**Минусы:** доменные сущности и инварианты дублируются между слайсами; нет единой точки для инвариантов агрегата (например, правила публикации Dish). Один разработчик не получает выгоды от изоляции слайсов.

**Причина отклонения:** при 60+ use cases в модуле Dishes дублирование доменных правил между слайсами стало бы неуправляемым.

### Вариант C — Clean Architecture (Domain → Application → Infrastructure) ⭐

Три .NET-проекта на модуль с направлением зависимостей **внутрь к домену**: Domain не знает ни о чём, Application зависит только от Domain, Infrastructure реализует интерфейсы Domain/Application, WebAPI компонует всё через DI.

**Плюсы:** бизнес-логика изолирована и тестируема, зависимости однонаправленны, структура навигабельна для любого .NET-разработчика, стандарт индустрии.
**Минусы:** больше проектов в solution, немного boilerplate (`AssemblyReference`, `ServiceCollectionExtensions`). Для простых CRUD-справочников может казаться избыточным.

---

## 3. Decision (Принятое решение)

Каждый модуль (и Shared Kernel) организован по **Clean Architecture** с тремя физически разделёнными проектами.

### Структура одного модуля

```
Modules/{ModuleName}/
├── GastronomePlatform.Modules.{ModuleName}.Domain/
│   ├── Entities/            # Сущности, агрегаты
│   ├── ValueObjects/        # Объекты-значения
│   ├── Enums/               # Доменные перечисления
│   ├── Events/              # Доменные события
│   ├── Errors/              # Доменные ошибки ({ModuleName}Errors.cs)
│   ├── Repositories/        # Интерфейсы репозиториев (IXxxRepository)
│   ├── Contracts/           # Межмодульные интерфейсы (если модуль — поставщик)
│   └── AssemblyReference.cs
│
├── GastronomePlatform.Modules.{ModuleName}.Application/
│   ├── Commands/            # Команды + обработчики + валидаторы
│   ├── Queries/             # Запросы + обработчики + DTO ответов
│   └── AssemblyReference.cs
│
└── GastronomePlatform.Modules.{ModuleName}.Infrastructure/
    ├── Persistence/         # DbContext, EF-конфигурации, Migrations/, интерсепторы
    ├── Repositories/        # Реализации репозиториев
    ├── Services/            # Реализации внешних интерфейсов
    └── Extensions/          # ServiceCollectionExtensions (Add{ModuleName}Module)
```

### Правило зависимостей (Dependency Rule)

```
WebAPI (Presentation)
    ↓ ссылается на
Infrastructure
    ↓ ссылается на
Application
    ↓ ссылается на
Domain ← ничего не знает о внешнем мире
```

**Строго запрещено:** Domain → Application/Infrastructure/WebAPI; Application → Infrastructure/WebAPI; Infrastructure модуля A → Infrastructure модуля B.

**Межмодульные зависимости** — только на Domain (предпочтительно, паттерн `Domain/Contracts`, ADR-0008) или Application потребляемого модуля (известное исключение `IMediaService` — ADR-0007).

### Shared Kernel (Common)

Та же структура: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result`, `Error`, `IDomainEvent` (Common.Domain); CQRS-абстракции, `ValidationBehavior` (Common.Application); middleware, `DateTimeProvider`, `CurrentUserService` (Common.Infrastructure).

---

## 4. Rationale (Обоснование)

1. **Тестируемость.** Domain и Application тестируются без БД, HTTP и файловой системы — репозитории и внешние сервисы мокаются. Результат: ~240 unit-тестов к Этапу 2 без единого обращения к реальной инфраструктуре.
2. **Защита бизнес-логики.** Domain не зависит от EF Core, ASP.NET Core, Serilog — единственная внешняя зависимость `MediatR.Contracts` (ради `IDomainEvent`). Смена ORM затрагивает только Infrastructure.
3. **Навигабельность.** Три проекта отвечают на вопрос «куда класть код»: бизнес-правила — Domain, оркестрация — Application, БД/HTTP/файлы — Infrastructure.
4. **Соответствие ADR-0001.** При выделении модуля в микросервис Domain и Application переезжают без изменений, переписывается только Infrastructure.
5. **Стандарт индустрии** — новый .NET-разработчик узнаёт структуру без обучения.

---

## 5. Consequences (Последствия)

### Positive (Положительные)

- Domain изолирован — бизнес-инварианты защищены компилятором (из Domain нет доступа к DbContext).
- Unit-тесты бизнес-логики выполняются мгновенно (нет I/O).
- Модуль — самодостаточный набор из 3 проектов; шаблон нового модуля задокументирован в Development Guide.
- Нарушение Dependency Rule видно в `<ProjectReference>` на ревью; циклы не компилируются.

### Negative / Trade-offs (Отрицательные / компромиссы)

- Проектов много: 5 реализованных модулей × 3 + 3 Common + WebAPI + тесты (30+ при всех 8 модулях). Solution Folders помогают, но build time растёт.
- Boilerplate на модуль: `AssemblyReference.cs`, `ServiceCollectionExtensions`, `DbContext`. Шаблон в документации минимизирует ручную работу.
- Для простых справочников (Category, Tag) три слоя избыточны — но единообразие важнее экономии на паре файлов.

### Areas of Caution (На что обратить внимание)

- На ревью проверять направление `<ProjectReference>`; Infrastructure не ссылается на чужой Infrastructure.
- Интерфейсы репозиториев — в Domain, реализации — в Infrastructure; не допускать утечку EF-типов (`IQueryable<T>`) в интерфейсы.
- `MediatR.Contracts` — единственная допустимая внешняя зависимость Domain; потребность в другой — сигнал, что код не в том слое.
- XML-документация Domain не ссылается через `<see cref>` на типы верхних слоёв — компилятор не резолвит cref (CS1574) и каскадно валит сборку; использовать текстовые `<c>...</c>`.

---

## 6. Implementation Reference (Связь с кодовой базой)

- `src/Common/GastronomePlatform.Common.Domain/` — `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result`, `Error`, `IDomainEvent`; в `.csproj` — единственный `PackageReference`: `MediatR.Contracts`.
- `src/Common/GastronomePlatform.Common.Application/` — `ICommand`/`IQuery`/`ICommandHandler`/`IQueryHandler`, `ValidationBehavior`, `IDateTimeProvider`, `ICurrentUserService`.
- `src/Common/GastronomePlatform.Common.Infrastructure/` — middleware (`GlobalExceptionHandlingMiddleware`, `CorrelationIdMiddleware`), реализации сервисов, DI-расширения.
- `src/Modules/Auth/` — эталонный модуль: `Auth.Domain` (`RefreshToken`, `Domain/Contracts/IAuthUserService`), `Auth.Application` (Register/Login/Refresh/Logout), `Auth.Infrastructure` (`AuthDbContext`, `JwtService`, ASP.NET Identity).
- `src/GastronomePlatform.WebAPI/Program.cs` — композиция модулей.
- `<wiki>/08_Разработка-(Development-Guide).md` — пошаговый шаблон создания модуля.

---

## История изменений

- **2026-03-04:** Accepted. Структура Shared Kernel реализована на Этапе 0.
- **2026-05-23:** Правило подтверждено практикой Этапов 1–2 (Auth, Users, Dishes, Media); зафиксированы defense-in-depth-дополнения на стороне БД (CHECK-constraints, осознанные OnDelete-стратегии) — как принадлежность Infrastructure, не Domain (детали — ADR-0004).
- **2026-08-02:** Финализация при переносе в `docs/public/adr/`: структура папок сверена с фактической (`Persistence/Migrations`, `Repositories/` в Infrastructure), количество тестов уточнено (~240 к Этапу 2), добавлено предостережение про `<see cref>` из Domain на верхние слои.
