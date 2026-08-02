# Architecture Decision Records (ADR)

> **Назначение папки.** Реестр архитектурных решений, принятых в проекте GastronomePlatform. Каждый файл — отдельное решение в формате ADR (Context → Considered Alternatives → Decision → Rationale → Consequences → Future Scope → Implementation Reference).
>
> **Шаблон:** [`../DocTemplates/ADR-template.md`](../DocTemplates/ADR-template.md).
>
> **Соглашения по ID и именам файлов:** [`../documentation-conventions.md`](../documentation-conventions.md) §4.2.

---

## Принципы

- **ADR — иммутабельный документ.** Если решение пересмотрено — создаётся новый ADR со статусом `Accepted`; старый получает статус `Superseded by ADR-MMMM` со ссылкой на новый.
- **Статусы:** `Proposed` / `Accepted` / `Deprecated` / `Superseded`.
- **ID не переиспользуется.** Если ADR отменён или заменён — номер остаётся занятым.
- **Узкие vs общие.** Узкий ADR описывает конкретное решение для одной сущности/модуля. Общий ADR фиксирует переиспользуемый принцип; на него ссылаются узкие ADR-применения.

---

## Действующие ADR

| ID | Title | Status | Date | Stage | Scope |
|----|-------|--------|------|-------|-------|
| [ADR-0001](./ADR-0001-modular-monolith.md) | Modular Monolith как архитектурный стиль | Accepted | 2026-02-23 | 0 | Cross-cutting |
| [ADR-0002](./ADR-0002-clean-architecture.md) | Clean Architecture как внутренняя организация модулей | Accepted | 2026-03-04 | 0 | Cross-cutting |
| [ADR-0003](./ADR-0003-cqrs-mediatr.md) | CQRS + MediatR как модель обработки запросов | Accepted | 2026-03-04 | 0 | Cross-cutting |
| [ADR-0004](./ADR-0004-postgresql-schema-per-module.md) | PostgreSQL с разделением по схемам | Accepted | 2026-02-23 | 0 | Cross-cutting |
| [ADR-0005](./ADR-0005-rabbitmq-event-driven.md) | RabbitMQ для Event-Driven взаимодействия между модулями | Accepted | 2026-02-23 | 0 | Cross-cutting |
| [ADR-0006](./ADR-0006-dish-recipe-single-aggregate.md) | Dish + Recipe как один агрегат | Accepted | 2026-04-26 | 2 | Dishes |
| [ADR-0007](./ADR-0007-media-polymorphic-entity-links.md) | Кросс-модульные ссылки в Media — полиморфная привязка `EntityType + EntityId` | Accepted | 2026-05-31 | 2 | Media |
| [ADR-0008](./ADR-0008-auth-user-service-contract.md) | `IAuthUserService` как межмодульный контракт между Auth и Users | Accepted | 2026-04-04 | 1 | Auth, Users |
| [ADR-0009](./ADR-0009-dish-versioning-strategy.md) | Стратегия версионирования блюд — только последняя опубликованная версия | Accepted | 2026-04-26 | 2 | Dishes |
| [ADR-0010](./ADR-0010-content-licensing-model.md) | Лицензионная модель пользовательского контента — неисключительная безотзывная лицензия | Accepted | 2026-04-26 | 2 | Dishes, Users, Orders |
| [ADR-0011](./ADR-0011-two-layer-dish-storage.md) | Двухслойное хранение Dish (основные таблицы + jsonb-снепшот) | Accepted | 2026-04-26 | 2 | Dishes |
| [ADR-0012](./ADR-0012-recipe-ingredient-discriminated-union.md) | RecipeIngredient — discriminated union «catalog vs freeform» в модуле Dishes | Accepted | 2026-05-30 | 2 | Dishes |
| [ADR-0013](./ADR-0013-publish-spam-protection.md) | Защита `Dish.Publish` от спама `DishPublishedEvent` — Domain-инвариант | Accepted | 2026-05-30 | 2 | Dishes |
| [ADR-0014](./ADR-0014-discriminated-unions-in-cqrs.md) | Discriminated Unions в CQRS-архитектуре проекта | Accepted | 2026-05-30 | — | Cross-cutting |
| [ADR-0015](./ADR-0015-publish-precheck-before-snapshot-build.md) | Pre-check инвариантов публикации до сборки jsonb-снепшота — `Dish.CheckCanPublish` | Accepted | 2026-06-06 | 2 | Dishes |
| [ADR-0016](./ADR-0016-diet-conflicts-mask.md) | Источник конфликтов диетических меток — поле `Ingredient.DietConflictsMask` | Accepted | 2026-06-07 | 2 | Dishes |
| [ADR-0017](./ADR-0017-recurring-payments-yookassa.md) | Рекуррентные платежи через ЮKassa — `IPaymentGateway`, суточный сборщик + webhook-обработчик, идемпотентность и reconciliation | Accepted | 2026-06-28 | 3 | Subscriptions |
| [ADR-0018](./ADR-0018-web-frontend-stack.md) | Стек веб-интерфейса — React + TypeScript как SPA поверх существующего API | Accepted | 2026-07-19 | 4 | Web |
| [ADR-0019](./ADR-0019-dish-updated-at-interceptor.md) | Автоматическое обновление `Dish.UpdatedAt` через `SaveChangesInterceptor` | Accepted | 2026-05-23 | 2 | Dishes |
| [ADR-0020](./ADR-0020-allergens-mask-on-dish-aggregate.md) | Денормализованные маркеры состава на корне Dish (`AllergensMask`, `HasUnverifiedAllergens`) | Accepted | 2026-05-17 | 2 | Dishes |

---

## Как добавить новый ADR

1. Выбрать следующий свободный номер — после максимального действующего ID.
2. Скопировать шаблон [`../DocTemplates/ADR-template.md`](../DocTemplates/ADR-template.md) под именем `ADR-<NNNN>-<kebab-case-title>.md`.
3. Заполнить разделы согласно правилам шаблона. Опциональные разделы (`Stage`, `Future Scope`, `Related`) убирать, если не применимы.
4. Добавить запись в таблицу «Действующие ADR» выше.
5. Из доменной модели / use-cases / других ADR расставить обратные ссылки на новый ADR в местах, где он влияет на интерпретацию правил.
