# 🍳 GastronomePlatform

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/aspnet)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Documentation](https://img.shields.io/badge/Documentation-Wiki-blue)](https://github.com/SVARGus/GastronomePlatform/wiki)

**Серверная часть многофункциональной кулинарной платформы, разработанная как модульный монолит на .NET 8 с event-driven взаимодействием.**

GastronomePlatform — это дипломный проект, представляющий собой back-end экосистему для любителей кулинарии. Платформа объединяет каталог рецептов, систему подписок, маркетплейс для заказа готовых блюд и социальные функции, предоставляя единое API для веб- и мобильных клиентов.

## ✨ Ключевые особенности

*   **🏗️ Модульная архитектура:** Чёткое разделение на 8 доменных модулей (Auth, Users, Dishes, Media, Subscriptions, Notifications, Social, Orders) внутри единой кодовой базы.
*   **⚡ Event-Driven ядро:** Асинхронная коммуникация между модулями через RabbitMQ.
*   **🎯 Гибкая монетизация:** Многоуровневая система подписок (Free, Premium, Business).
*   **🔗 Готовность к эволюции:** Архитектура для плавного перехода к микросервисам.
*   **📱 Мультиплатформенные клиенты:** Единое API для веб (ASP.NET Core), мобильного (.NET MAUI) и десктопного (Avalonia UI) приложений.

## 🛠️ Технологический стек

| Компонент | Технология |
| :--- | :--- |
| **Бэкенд** | C# 12, .NET 8, ASP.NET Core |
| **Архитектура** | Модульный монолит, Clean Architecture, CQRS |
| **База данных** | PostgreSQL 16+ (с разделением по схемам) |
| **Брокер сообщений** | RabbitMQ |
| **Логирование** | Serilog → Seq |
| **Клиентские фреймворки** | ASP.NET Core MVC/Blazor, .NET MAUI, Avalonia UI |
| **Контейнеризация** | Docker, Docker Compose |
| **Тестирование** | xUnit, Moq, FluentAssertions |

## 🚀 Быстрый старт

Требуется [.NET 8 SDK](https://dotnet.microsoft.com/download) и [Docker Desktop](https://www.docker.com/get-started).

```bash
# 1. Клонировать
git clone https://github.com/SVARGus/GastronomePlatform.git
cd GastronomePlatform

# 2. Запустить инфраструктуру (PostgreSQL, RabbitMQ, Seq)
docker compose up -d

# 3. Запустить приложение
cd src/GastronomePlatform.WebAPI
dotnet run
```

| URL | Описание |
|-----|---------|
| `https://localhost:7093/swagger` | Swagger UI |
| `https://localhost:7093/health/live` | Health Check |
| `http://localhost:8081` | Seq — логи |
| `http://localhost:15672` | RabbitMQ — управление |

> Подробнее: [Wiki: Установка и запуск](https://github.com/SVARGus/GastronomePlatform/wiki/06_Установка-и-запуск)

## 📁 Структура проекта

```text
GastronomePlatform/
├── docker-compose.yml                              # PostgreSQL + RabbitMQ + Seq
├── src/
│   ├── Common/                                     # Shared Kernel
│   │   ├── GastronomePlatform.Common.Domain/       #   Entity, AggregateRoot, ValueObject,
│   │   │                                           #   Result, Error, IDomainEvent
│   │   ├── GastronomePlatform.Common.Application/  #   CQRS (ICommand, IQuery, handlers),
│   │   │                                           #   IDateTimeProvider, ICurrentUserService
│   │   └── GastronomePlatform.Common.Infrastructure/ # Middleware, сервисы, DI-расширения
│   ├── Modules/                                    # Доменные модули
│   │   ├── Auth/                                   #   Регистрация, JWT, refresh tokens
│   │   ├── Users/                                  #   Профили, роли (RBAC)
│   │   ├── Dishes/                                 #   Каталог блюд и рецептов
│   │   ├── Media/                                  #   Загрузка и хранение медиафайлов
│   │   ├── Subscriptions/                          #   Тарифы и монетизация
│   │   ├── Notifications/                          #   Email, push, in-app уведомления
│   │   ├── Social/                                 #   Комментарии, рейтинги, чаты
│   │   └── Orders/                                 #   Маркетплейс заказов
│   └── GastronomePlatform.WebAPI/                  # Точка входа, композиция модулей
├── tests/                                          # Автоматические тесты
└── docs/                                           # Документация
```

Подробнее: [Wiki: Структура проекта](https://github.com/SVARGus/GastronomePlatform/wiki/13_Структура-проекта)

## 🗺️ Дорожная карта

| Этап | Название | Срок | Модули | Статус |
|:-----|:---------|:-----|:-------|:-------|
| 0 | Фундамент | фев–мар 2026 | Common, WebAPI | 🚧 В работе |
| 1 | Аутентификация и пользователи | мар 2026 | Auth, Users | ⏳ Ожидает |
| 2 | Контент и медиа | мар–апр 2026 | Dishes, Media | ⏳ Ожидает |
| 3 | Подписки и монетизация | апр–май 2026 | Subscriptions | ⏳ Ожидает |
| 4 | Веб-интерфейс | май–июн 2026 | Web (MVC/Blazor) | ⏳ Ожидает |
| 5 | Уведомления и социал | июн–июл 2026 | Notifications, Social | ⏳ Ожидает |
| 6 | Маркетплейс заказов | авг+ 2026 | Orders | 📋 Запланировано |
| 7 | Клиентские приложения | 2026–2027 | MAUI, Avalonia | 📋 Запланировано |
| 8 | Продакшен и эволюция | будущее | Все | 📋 Запланировано |

> Подробная дорожная карта с задачами, API-эндпоинтами и критериями готовности: [Wiki: Дорожная карта](https://github.com/SVARGus/GastronomePlatform/wiki/05_Дорожная-карта)

## 📖 Документация и ресурсы

Полная техническая документация, архитектурные решения, руководства и API-справочник собраны в **Wiki** этого репозитория:

*   **[🎯 Описание проекта](https://github.com/SVARGus/GastronomePlatform/wiki/01_Описание-проекта)** — Цели, аудитория, требования.
*   **[🏗️ Архитектура и дизайн](https://github.com/SVARGus/GastronomePlatform/wiki/02_Архитектура)** — Принципы, паттерны, стек технологий.
*   **[🚀 Эксплуатация и DevOps](https://github.com/SVARGus/GastronomePlatform/wiki/06_Установка-и-запуск)** — Развертывание, CI/CD, мониторинг.
*   **[🔧 Руководство для разработчика](https://github.com/SVARGus/GastronomePlatform/wiki/08_Разработка-(Development-Guide))** — Стандарты кода, ветвление, коммиты.
*   **[📋 API-документация](https://github.com/SVARGus/GastronomePlatform/wiki/10_API-документация)** — Форматы ответов, коды ошибок
*   **[📱 Клиентские приложения](https://github.com/SVARGus/GastronomePlatform/wiki/14_Веб-сайт-(ASP.NET-Core-MVC---Blazor))** — Документация по веб-, мобильному и десктопному клиентам.
*   **[🗺️ Дорожная карта](https://github.com/SVARGus/GastronomePlatform/wiki/05_Дорожная-карта)** — Этапы и план развития проекта.

## 👥 Команда проекта

Дипломный проект (ВКР). Архитектор и разработчик: [SVARGus](https://github.com/SVARGus)

## 📄 Лицензия

Этот проект распространяется под лицензией MIT. Подробнее см. файл [LICENSE](https://github.com/SVARGus/GastronomePlatform/blob/master/LICENSE.txt).

---
*Разработано с ❤️ и большим количеством виртуального кофе.*