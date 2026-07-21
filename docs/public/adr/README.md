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
| [ADR-0012](./ADR-0012-recipe-ingredient-discriminated-union.md) | RecipeIngredient — discriminated union «catalog vs freeform» в модуле Dishes | Accepted | 2026-05-30 | 2 | Dishes |
| [ADR-0013](./ADR-0013-publish-spam-protection.md) | Защита `Dish.Publish` от спама `DishPublishedEvent` — Domain-инвариант | Accepted | 2026-05-30 | 2 | Dishes |
| [ADR-0014](./ADR-0014-discriminated-unions-in-cqrs.md) | Discriminated Unions в CQRS-архитектуре проекта | Accepted | 2026-05-30 | — | Cross-cutting |
| [ADR-0015](./ADR-0015-publish-precheck-before-snapshot-build.md) | Pre-check инвариантов публикации до сборки jsonb-снепшота — `Dish.CheckCanPublish` | Accepted | 2026-06-06 | 2 | Dishes |
| [ADR-0016](./ADR-0016-diet-conflicts-mask.md) | Источник конфликтов диетических меток — поле `Ingredient.DietConflictsMask` | Accepted | 2026-06-07 | 2 | Dishes |
| [ADR-0017](./ADR-0017-recurring-payments-yookassa.md) | Рекуррентные платежи через ЮKassa — `IPaymentGateway`, суточный сборщик + webhook-обработчик, идемпотентность и reconciliation | Accepted | 2026-06-28 | 3 | Subscriptions |
| [ADR-0018](./ADR-0018-web-frontend-stack.md) | Стек веб-интерфейса — React + TypeScript как SPA поверх существующего API | Accepted | 2026-07-19 | 4 | Web |

---

## Зарезервированные номера

Диапазон `ADR-0001..0011` зарезервирован за базовыми архитектурными решениями проекта, которые приняты и реализованы, но формально как ADR пока не оформлены. Список — в приватном `docs/_private/private_TODO-будущие-этапы.md` §1.2. По мере оформления — каждый занимает свой зарезервированный номер.

| Зарезервированный ID | Тема | Статус |
|----------------------|------|--------|
| ADR-0001 | Modular Monolith | Не оформлен |
| ADR-0002 | Clean Architecture | Не оформлен |
| ADR-0003 | CQRS + MediatR | Не оформлен |
| ADR-0004 | PostgreSQL с разделением по схемам | Не оформлен |
| ADR-0005 | RabbitMQ для Event-Driven | Не оформлен |
| ADR-0006 | Dish + Recipe как один агрегат | Не оформлен |
| ADR-0007 | Кросс-модульные ссылки в Media (`EntityType + EntityId`) | Не оформлен |
| ADR-0008 | `IAuthUserService` как контракт между модулями | Не оформлен |
| ADR-0009 | Стратегия версионирования блюд | Не оформлен |
| ADR-0010 | Лицензионная модель пользовательского контента | Не оформлен |
| ADR-0011 | Двухслойное хранение Dish (основные таблицы + jsonb-снепшот) | Не оформлен |

---

## Как добавить новый ADR

1. Выбрать следующий свободный номер. Если решение из зарезервированного списка — взять его номер; иначе — следующий после максимального действующего ID.
2. Скопировать шаблон [`../DocTemplates/ADR-template.md`](../DocTemplates/ADR-template.md) под именем `ADR-<NNNN>-<kebab-case-title>.md`.
3. Заполнить разделы согласно правилам шаблона. Опциональные разделы (`Stage`, `Future Scope`, `Related`) убирать, если не применимы.
4. Добавить запись в таблицу «Действующие ADR» выше.
5. Из доменной модели / use-cases / других ADR расставить обратные ссылки на новый ADR в местах, где он влияет на интерпретацию правил.
