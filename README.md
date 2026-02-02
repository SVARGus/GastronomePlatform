# 🍳 GastronomePlatform

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/aspnet)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16+-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Documentation](https://img.shields.io/badge/Documentation-Wiki-blue)](https://github.com/SVARGus/GastronomePlatform/wiki)

**Серверная часть многофункциональной кулинарной платформы, разработанная как модульный монолит на .NET 8 с event-driven взаимодействием.**

GastronomePlatform — это дипломный проект, представляющий собой back-end экосистему для любителей кулинарии. Платформа объединяет каталог рецептов, систему подписок, маркетплейс для заказа готовых блюд и социальные функции, предоставляя единое API для веб- и мобильных клиентов.

## ✨ Ключевые особенности

*   **🏗️ Модульная архитектура:** Чёткое разделение на доменные модули (Рецепты, Заказы, Подписки) внутри единой кодовой базы для простоты разработки и поддержки.
*   **⚡ Event-Driven ядро:** Асинхронная коммуникация между модулями через RabbitMQ для обеспечения отказоустойчивости и масштабируемости длительных процессов (оформление заказа, интеграции).
*   **🎯 Гибкая монетизация:** Многоуровневая система подписок (Free, Premium, Business) для пользователей, самозанятых поваров и ресторанов.
*   **🔗 Готовность к эволюции:** Архитектура спроектирована для плавного перехода к микросервисной модели с минимальными затратами.
*   **📱 Мультиплатформенные клиенты:** Единое API для веб-сайта (ASP.NET Core), мобильного (.NET MAUI) и десктопного (Avalonia UI) приложений.

## 🛠️ Технологический стек

| Компонент | Технология |
| :--- | :--- |
| **Бэкенд / Платформа** | C# 12, .NET 8, ASP.NET Core |
| **Архитектура** | Модульный монолит, Clean Architecture, CQRS |
| **База данных** | PostgreSQL 16+ (с разделением по схемам) |
| **Асинхронная коммуникация** | RabbitMQ |
| **Клиентские фреймворки** | ASP.NET Core MVC/Blazor, .NET MAUI, Avalonia UI |
| **Контейнеризация** | Docker, Docker Compose |
| **Тестирование** | xUnit, Moq, FluentAssertions, TestServer |

## 🚀 Быстрый старт

Для локального запуска back-end части потребуются [.NET 8 SDK](https://dotnet.microsoft.com/download) и [Docker](https://www.docker.com/get-started).

1.  **Клонируйте репозиторий:**
    ```bash
    git clone https://github.com/SVARGus/GastronomePlatform.git
    cd GastronomePlatform
    ```

2.  **Запустите зависимости (БД, брокер):**
    ```bash
    docker-compose -f docker-compose.infrastructure.yml up -d
    ```

3.  **Запустите приложение:**
    ```bash
    cd src/GastronomePlatform.Web
    dotnet run
    ```

4.  Приложение будет доступно по адресу `https://localhost:5001`, а Swagger UI для изучения API — по адресу `https://localhost:5001/swagger`.

> **Примечание:** Это базовая инструкция. Подробное руководство по установке, настройке окружения и миграциям БД доступно в [Wiki: Установка и запуск](https://github.com/SVARGus/GastronomePlatform/wiki/06_Установка-и-запуск).

## 📁 Структура проекта (кратко)
    ```text
    GastronomePlatform/
    ├── src/ # Исходный код
    │ ├── GastronomePlatform.Domain/ # Доменные модели и интерфейсы
    │ ├── GastronomePlatform.Application/ # Бизнес-логика (CQRS, сервисы)
    │ ├── GastronomePlatform.Infrastructure/ # Репозитории, внешние сервисы
    │ ├── GastronomePlatform.Web/ # Веб-API (точка входа)
    │ └── Modules/ # Доменные модули
    │ ├── Identity/ # Аутентификация и пользователи
    │ ├── Dishes/ # Каталог рецептов (ядро системы)
    │ └── ... # Orders, Subscriptions, Social
    ├── tests/ # Автоматические тесты
    │ ├── Unit/ # Модульные тесты
    │ ├── Integration/ # Интеграционные тесты
    │ └── API/ # API-тесты
    └── docs/ # Дополнительная документация
    ```

Полное описание структуры, назначения проектов и их зависимостей смотрите в [Wiki: Структура проекта](https://github.com/SVARGus/GastronomePlatform/wiki/13_Структура-проекта).

## 📖 Документация и ресурсы

Полная техническая документация, архитектурные решения, руководства и API-справочник собраны в **Wiki** этого репозитория:

*   **[🎯 Общее понимание](https://github.com/SVARGus/GastronomePlatform/wiki/01_Описание-проекта)** — Цели, аудитория, требования.
*   **[🏗️ Архитектура и дизайн](https://github.com/SVARGus/GastronomePlatform/wiki/02_Архитектура)** — Принципы, паттерны, стек технологий.
*   **[🚀 Эксплуатация и DevOps](https://github.com/SVARGus/GastronomePlatform/wiki/06_Установка-и-запуск)** — Развертывание, CI/CD, мониторинг.
*   **[🔧 Руководство для разработчика](https://github.com/SVARGus/GastronomePlatform/wiki/08_Разработка-(Development-Guide))** — Стандарты кода, ветвление, коммиты.
*   **[📱 Клиентские приложения](https://github.com/SVARGus/GastronomePlatform/wiki/14_Веб-сайт-(ASP.NET-Core-MVC---Blazor))** — Документация по веб-, мобильному и десктопному клиентам.
*   **[🗺️ Планирование](https://github.com/SVARGus/GastronomePlatform/wiki/05_Дорожная-карта)** — Дорожная карта развития проекта.

## 👥 Команда проекта

Проект разрабатывается в рамках выполнения выпускной квалификационной работы (ВКР).
*   **Архитектор и единственный разработчик:** [SVARGus](https://github.com/SVARGus) – Единственный разработчик.

## 📄 Лицензия

Этот проект распространяется под лицензией MIT. Подробнее см. файл [LICENSE](https://github.com/SVARGus/GastronomePlatform/blob/master/LICENSE.txt).

---
*Разработано с ❤️ и большим количеством виртуального кофе.*
