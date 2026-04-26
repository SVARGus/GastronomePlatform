# Модуль Media — Доменная модель (Этап 2)

> **Статус:** Проектирование
> **Этап дорожной карты:** 2 (Контент и медиа)
> **Дата:** 2026-04-20
> **Связанные документы:** [[05_Дорожная-карта]], [[02_Архитектура]], [[13_Структура-проекта]], [[08_Разработка-(Development-Guide)]], [[Этап-2_Модуль-Dishes_Доменная-модель]]

---

## Содержание

1. [Назначение модуля и границы ответственности](#1-назначение-модуля-и-границы-ответственности)
2. [Ключевые архитектурные решения](#2-ключевые-архитектурные-решения)
3. [Стратегия реализации](#3-стратегия-реализации)
4. [Сводная таблица сущностей](#4-сводная-таблица-сущностей)
5. [Enums](#5-enums)
6. [Core-сущности — детальный разбор](#6-core-сущности--детальный-разбор)
7. [Deferred-сущности](#7-deferred-сущности)
8. [Константы `MediaEntityTypes`](#8-константы-mediaentitytypes)
9. [Абстракция хранилища `IFileStorage`](#9-абстракция-хранилища-ifilestorage)
10. [Межмодульный контракт `IMediaService`](#10-межмодульный-контракт-imediaservice)
11. [Валидации](#11-валидации)
12. [Структура файлов на диске и в S3](#12-структура-файлов-на-диске-и-в-s3)
13. [Конфигурация](#13-конфигурация)
14. [Заметки на будущее (TODO в коде)](#14-заметки-на-будущее-todo-в-коде)
15. [Что дальше](#15-что-дальше)

---

## 1. Назначение модуля и границы ответственности

### Что модуль делает

- Принимает загрузку файлов от пользователей и администраторов.
- Хранит метаданные файлов в схеме `media` PostgreSQL.
- Хранит сами файлы в абстрактном хранилище (`IFileStorage`) — локально на Этапе 2, S3-совместимо на Этапе 8+.
- Валидирует типы, размеры и содержимое файлов.
- Генерирует миниатюры (thumbnails) изображений.
- Стримит файлы клиентам через контроллер с проверкой доступа.
- Отслеживает жизненный цикл файла: `Uploaded → Processing → Ready → Deleted`.
- Обеспечивает мягкую привязку к сущностям других модулей через `EntityType + EntityId`.
- Маркирует файлы по категории данных (`Public` / `Personal`) для будущего compliance с 152-ФЗ.

### Что модуль НЕ делает

- Не знает о бизнес-семантике других модулей (Dish, Category, User).
- Не решает, «кому можно показывать этот файл» — это забота модулей-владельцев сущностей (Dishes/Subscriptions проверяют доступ к рецептам, Users — к аватарам).
- Не транскодирует видео (планируется на Этапе 8+).
- Не применяет watermark/распознавание изображений — Этап 8+.
- Не оптимизирует SEO (прямые URL для `og:image` и подобное) — отдельное архитектурное решение на Этапе 4.

### Кандидат на выделение в микросервис

Да — согласно дорожной карте, `Media` выделяется одним из первых (наряду с `Auth` → Identity Server). Всё проектирование на Этапе 2 учитывает эту цель:

- Нет FK между схемами (`dishes`, `users`, `media`).
- Публичный контракт через `IMediaService` (по аналогии с `IAuthUserService`).
- Абстракция `IFileStorage` — подменяется в DI без касания Domain/Application.

---

## 2. Ключевые архитектурные решения

Все развилки, принятые на этапе проектирования модуля `Media`.

| № | Вопрос | Решение |
|---|--------|---------|
| 5 | Хранение локальных файлов: `wwwroot/uploads/` или вне wwwroot + контроллер | **Вне wwwroot + контроллер** `GET /api/media/{id}`. Контроллер проверяет доступ, единообразие с S3 |
| 6 | Проверка владельца медиа при привязке к сущности | **Да, проверяем**. `AttachToEntityAsync` проверяет, что `actorUserId == media.OwnerUserId` (или `actorUserId` — Admin для системных файлов) |
| — | Системный владелец (иконки категорий, фото ингредиентов) | **`MediaFile.OwnerUserId` — nullable.** NULL означает системный файл |
| — | Мягкая привязка к сущностям | **Поля `EntityType` (varchar(50)) + `EntityId` (uuid?)** в `MediaFile`. Заменяют отдельный флаг `IsAttached` |
| — | Категория данных для 152-ФЗ | **Поле `DataCategory` enum (Public / Personal)**. Маршрутизация по регионам хранения — Этап 8+ |
| — | Когда привязывать медиа (eager vs lazy) | **Eager** — при создании Draft сущности сразу вызываем `AttachToEntityAsync` |
| — | Thumbnails: отдельная таблица или JSON в `MediaFile` | **Отдельная таблица `MediaThumbnail`**. Гибкость для новых размеров без миграции |
| — | Генерация thumbnails на Этапе 2 | **Синхронно** в процессе upload. Асинхронно через RabbitMQ — Этап 8+ |
| — | Форматы изображений для пользователей на Этапе 2 | **JPEG, PNG**. WebP, AVIF — Этап 8+ |
| — | SVG для пользователей | **Запрещён** (XSS-риск при прямой отдаче). Админам разрешён через отдельный use case |
| — | Ограничения пользовательского upload | Max 10 MB, max 4096×4096 px, min 100×100 px |
| — | Статус `Temporary` | **Не нужен** — дублирует `EntityType IS NULL + ExpiresAt < now` |
| — | Логирование обращений к файлам (`MediaAccessLog`) | **Deferred** — Этап 8+ |
| — | Watermark на изображениях | **Deferred** — Этап 8+ |
| — | Публичные прямые URL через nginx для SEO | Отдельное обсуждение позже |

---

## 3. Стратегия реализации

По аналогии с Dishes — три уровня:

| Статус | Что делаем сейчас (Этап 2) |
|--------|---------------------------|
| 🟢 **Core** | Полная реализация: Domain-модель, EF-конфигурация, репозитории, use cases, контроллер, IFileStorage+LocalFileStorage, IMediaService реализация, unit-тесты |
| 🟡 **Stub** | На Этапе 2 stub-сущностей в Media нет (все запланированные — либо Core, либо Deferred) |
| ⚪ **Deferred** | Не трогаем. В коде — `// TODO: <Entity> — Этап N` там, где эта сущность появится |

---

## 4. Сводная таблица сущностей

**Легенда связей:**
- `A → B (1:1)` — один к одному, FK у стороны `A`
- `A → B (M:1)` — многие к одному, FK у стороны `A`
- `A ← B (1:M)` — один ко многим, FK у стороны `B`
- `A ↔ B (M:M)` — многие ко многим через связующую таблицу

| № | Русское имя | Имя в проекте | Статус | Описание | Ключевые поля | Связи |
|---|-------------|---------------|--------|----------|---------------|-------|
| 1 | Медиафайл | `MediaFile` | 🟢 | Метаданные файла (сам файл — в хранилище) | Id, OwnerUserId, EntityType, EntityId, DataCategory, MediaType, ContentType, OriginalFileName, StorageProvider, StorageKey, SizeBytes, Width, Height, DurationSeconds, Status, ExpiresAt, AttachedAt, DeletedAt, CreatedAt, UpdatedAt | `MediaFile ← MediaThumbnail (1:M)`; кросс-модульные ссылки через `EntityType + EntityId` (без FK) |
| 2 | Миниатюра | `MediaThumbnail` | 🟢 | Уменьшенная/оптимизированная копия | Id, MediaFileId, Size, Format, StorageKey, Width, Height, SizeBytes, CreatedAt | `MediaThumbnail → MediaFile (M:1)` |
| 3 | Журнал доступа | `MediaAccessLog` | ⚪ | Логи обращений для аналитики и аудита PII | — | Этап 8+ |
| 4 | Job обработки | `MediaProcessingJob` | ⚪ | Отслеживание асинхронных job'ов (thumbnails/transcode) | — | Этап 8+ |
| 5 | Watermark-правило | `WatermarkRule` | ⚪ | Правила наложения водяного знака | — | Этап 8+ |

**Итого на Этапе 2:** 2 Core-таблицы в схеме `media`.

---

## 5. Enums

Все enums размещаются в `Media.Domain/Enums/`. В БД хранятся как `int`.

### 5.1. MediaType

Тип медиа-контента.

| Значение | int | Этап | Описание |
|----------|-----|------|----------|
| `Image` | 0 | 2 | Изображение |
| `Video` | 1 | 8+ | Видео. На Этапе 2 не поддерживается |

### 5.2. MediaStatus

Жизненный цикл файла.

| Значение | int | Описание |
|----------|-----|----------|
| `Uploaded` | 0 | Файл принят, ждёт обработки (валидация, генерация thumbnails) |
| `Processing` | 1 | В процессе обработки (Этап 2 — очень короткое состояние, на Этапе 8+ станет значимым для асинхронной обработки) |
| `Ready` | 2 | Готов к отдаче клиенту |
| `Failed` | 3 | Ошибка обработки. `StorageKey` может быть NULL или указывать на сырой файл |
| `Deleted` | 4 | Soft-delete. Физически удаляется фоновой задачей |

### 5.3. MediaDataCategory

Категория данных для compliance с 152-ФЗ.

| Значение | int | Описание | Примеры `EntityType` |
|----------|-----|----------|---------------------|
| `Public` | 0 | Публичный контент | Dish, RecipeStep, CategoryIcon, IngredientImage |
| `Personal` | 1 | Персональные данные | UserAvatar, UserDocument |

**Правило автоопределения** при upload — на основе `EntityType` (см. `MediaEntityTypes`). Пересматривается в одном месте при добавлении новых типов.

### 5.4. ThumbnailSize

Размер миниатюры.

| Значение | int | Разрешение | Этап |
|----------|-----|-----------|------|
| `Small` | 0 | 150×150 | 8+ |
| `Medium` | 1 | 400×400 | 2 |
| `Large` | 2 | 800×800 | 8+ |

На Этапе 2 генерируем только `Medium`. Остальные размеры — когда появится веб-каталог с разными контекстами отображения.

### 5.5. ThumbnailFormat

Формат миниатюры.

| Значение | int | Этап | Описание |
|----------|-----|------|----------|
| `Jpeg` | 0 | 2 | Совместимость со всеми клиентами |
| `WebP` | 1 | 8+ | ~30% экономия трафика |
| `Avif` | 2 | 8+ | Ещё лучше, но слабая поддержка клиентами |

### 5.6. StorageProvider

Поле `MediaFile.StorageProvider` хранит строковый код (не enum), чтобы легко добавлять новых провайдеров без деплоя.

| Значение | Этап | Описание |
|----------|------|----------|
| `"local"` | 2 | Локальная ФС / Docker volume |
| `"s3"` | 8+ | AWS S3 или совместимое (MinIO, Yandex) |
| `"azure"` | Если понадобится | Azure Blob Storage |

---

## 6. Core-сущности — детальный разбор

### 6.1. MediaFile 🟢

**Назначение.** Центральная сущность модуля. Содержит метаданные файла и ссылку в хранилище. Сам файл лежит в `IFileStorage`, а не в БД.

**Базовый класс:** `AggregateRoot<Guid>` (нужны доменные события на будущих этапах — `MediaUploadedEvent`, `MediaAttachedEvent`).

#### Поля

| Поле | Тип (C#) | БД-тип | Null | Описание |
|------|----------|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `OwnerUserId` | `Guid?` | uuid | NULL | Кто загрузил. NULL — системный файл (иконки категорий, фото ингредиентов) |
| `EntityType` | `string?` | varchar(50) | NULL | Тип сущности-владельца: `Dish`, `RecipeStep`, `CategoryIcon`, ... Константы в `MediaEntityTypes` |
| `EntityId` | `Guid?` | uuid | NULL | ID сущности в её домене |
| `DataCategory` | `MediaDataCategory` | int | NOT NULL | `Public` / `Personal` |
| `MediaType` | `MediaType` | int | NOT NULL | `Image` / `Video` |
| `ContentType` | `string` | varchar(100) | NOT NULL | MIME: `image/jpeg`, `image/png` |
| `OriginalFileName` | `string` | varchar(255) | NOT NULL | «borsch.jpg» |
| `StorageProvider` | `string` | varchar(50) | NOT NULL | `"local"` на Этапе 2 |
| `StorageKey` | `string` | varchar(500) | NOT NULL | Путь в хранилище: `public/dishes/2026/04/<guid>.jpg` |
| `SizeBytes` | `long` | bigint | NOT NULL | Размер исходника |
| `Width` | `int?` | int | NULL | Для изображений |
| `Height` | `int?` | int | NULL | Для изображений |
| `DurationSeconds` | `int?` | int | NULL | Для видео (Этап 8+) |
| `Status` | `MediaStatus` | int | NOT NULL | |
| `ExpiresAt` | `DateTimeOffset?` | timestamptz | NULL | Заполняется при upload, обнуляется при attach. Для orphan cleanup |
| `AttachedAt` | `DateTimeOffset?` | timestamptz | NULL | Когда привязали к сущности |
| `DeletedAt` | `DateTimeOffset?` | timestamptz | NULL | Когда soft-deleted |
| `CreatedAt` | `DateTimeOffset` | timestamptz | NOT NULL | |
| `UpdatedAt` | `DateTimeOffset` | timestamptz | NOT NULL | |

#### Инварианты (Domain)

1. **Парность `EntityType` и `EntityId`**: оба NULL (orphan) или оба заполнены (attached). Комбинация «один заполнен — другой NULL» — запрещена.
2. **Персональные данные требуют владельца**: если `DataCategory = Personal`, то `OwnerUserId IS NOT NULL`. Системные файлы не могут быть персональными.
3. **Ready-файл имеет StorageKey**: если `Status = Ready`, то `StorageKey` непустой.
4. **Иммутабельность storage-пути**: после присвоения `StorageKey` не меняется. Перемещение файла (на S3, в другой регион) = создание новой записи с новым Id или отдельной колонки `MigratedFromId`.
5. **Только orphan-файлы имеют `ExpiresAt`**: при `AttachToEntity(...)` поле обнуляется.
6. **Deleted — конечное состояние**: из `Deleted` нельзя перейти в другой статус. Восстановление = создание новой записи.

#### Индексы

| Индекс | Назначение |
|---|---|
| `(EntityType, EntityId)` | Поиск всех файлов сущности (каскадная очистка при удалении Dish) |
| `(OwnerUserId)` | Получение файлов пользователя (экспорт данных по 152-ФЗ, удаление аккаунта) |
| `(Status, ExpiresAt) WHERE EntityType IS NULL` | Частичный индекс для фоновой очистки сирот. Компактный |
| `(Status, DeletedAt) WHERE Status = Deleted` | Частичный индекс для фоновой задачи физического удаления |

#### Domain API (методы)

| Метод | Назначение |
|---|---|
| `static Upload(ownerUserId, mediaType, contentType, originalFileName, storageProvider, storageKey, sizeBytes, width, height, dataCategory, expiresAt)` | Фабричный метод. Создаёт запись в статусе `Uploaded`. Возвращает `Result<MediaFile>` |
| `MarkAsProcessing()` | `Uploaded → Processing`. Вызывается перед генерацией thumbnails |
| `MarkAsReady()` | `Processing → Ready` |
| `MarkAsFailed(string reason)` | Любой статус (кроме Deleted) → `Failed` |
| `AttachToEntity(entityType, entityId, clock)` | Привязка к сущности. Обнуляет `ExpiresAt`, проставляет `AttachedAt` |
| `DetachFromEntity(clock)` | Отвязка. Проставляет `ExpiresAt = clock.UtcNow + orphanTimeout` |
| `SoftDelete(clock)` | `Ready → Deleted`. Проставляет `DeletedAt` |
| `AddThumbnail(size, format, storageKey, w, h, sizeBytes)` | Добавление миниатюры в агрегат |

#### Кросс-модульные ссылки

`EntityType + EntityId` — мягкая привязка к сущностям других модулей. Нет FK-constraint на уровне БД.

**Целостность обеспечивается на уровне приложения:**
- При удалении `Dish` → `IMediaService.DeleteByEntityAsync("Dish", dishId)`.
- При удалении `UserProfile` → `DeleteByEntityAsync("UserAvatar", userId)`.
- Периодическая проверка целостности (Этап 8+): фоновая задача проверяет, что `EntityId` по-прежнему существует в целевой схеме. Если нет — файл помечается на удаление.

---

### 6.2. MediaThumbnail 🟢

**Назначение.** Миниатюра / оптимизированная копия медиафайла. Принадлежит `MediaFile` (часть агрегата, но физически — отдельная таблица).

**Базовый класс:** `Entity<Guid>`.

#### Поля

| Поле | Тип | БД-тип | Null | Описание |
|------|-----|--------|------|----------|
| `Id` | `Guid` | uuid | NOT NULL | PK |
| `MediaFileId` | `Guid` | uuid | NOT NULL | FK на `MediaFile` с `ON DELETE CASCADE` |
| `Size` | `ThumbnailSize` | int | NOT NULL | `Small` / `Medium` / `Large` |
| `Format` | `ThumbnailFormat` | int | NOT NULL | `Jpeg` / `WebP` / `Avif` |
| `StorageKey` | `string` | varchar(500) | NOT NULL | Путь в хранилище |
| `Width` | `int` | int | NOT NULL | Фактическое разрешение (может отличаться от номинального при сохранении aspect ratio) |
| `Height` | `int` | int | NOT NULL | |
| `SizeBytes` | `long` | bigint | NOT NULL | |
| `CreatedAt` | `DateTimeOffset` | timestamptz | NOT NULL | |

#### Инварианты

- Уникальность: `(MediaFileId, Size, Format)` — один формат и размер на файл.
- `Width > 0` и `Height > 0`.
- `StorageKey` непустой.

#### Индексы

- Уникальный индекс `(MediaFileId, Size, Format)`.
- Внешний ключ `MediaFileId` автоматически индексируется EF Core.

---

## 7. Deferred-сущности

Кратко — что появится в будущих этапах.

### 7.1. MediaAccessLog ⚪ (Этап 8+)

Журнал обращений к файлам. Нужен для:
- Аналитики (какие блюда чаще просматривают).
- Аудита доступа к персональным данным (152-ФЗ, GDPR).
- Детектирования аномалий (DDoS, выкачивание всего каталога).

**Предварительные поля:** `Id`, `MediaFileId`, `AccessorUserId` (nullable для гостей), `AccessType` (View / Download / ThumbnailView), `IpAddress`, `UserAgent`, `AccessedAt`.

Объём большой → партиционирование по месяцам, отдельный movement-to-cold-storage после 90 дней.

### 7.2. MediaProcessingJob ⚪ (Этап 8+)

Отслеживание асинхронных job'ов обработки. Актуально когда:
- Генерация thumbnails станет асинхронной (через RabbitMQ).
- Появится транскодирование видео (часы работы на один файл).
- Появится watermark и другие тяжёлые операции.

**Предварительные поля:** `Id`, `MediaFileId`, `JobType` (GenerateThumbnail / Transcode / ApplyWatermark), `Status`, `AttemptsCount`, `LastError`, `StartedAt`, `CompletedAt`.

### 7.3. WatermarkRule ⚪ (Этап 8+)

Правила наложения водяного знака. Вариант применения: автор блюда хочет, чтобы все его фото помечались его ником.

**Предварительные поля:** `Id`, `OwnerUserId`, `WatermarkText` / `WatermarkImageId`, `Position` (enum), `Opacity`, `IsEnabled`.

Применение — при upload или on-the-fly при отдаче.

---

## 8. Константы `MediaEntityTypes`

Размещаются в `Media.Domain/Constants/MediaEntityTypes.cs`. Используются всеми модулями, работающими с медиа — аналогично `PlatformRoles` в `Common.Domain`.

```csharp
namespace GastronomePlatform.Modules.Media.Domain.Constants;

/// <summary>
/// Константы типов сущностей-владельцев медиафайлов.
/// Хранятся в поле <see cref="MediaFile.EntityType"/>.
/// </summary>
public static class MediaEntityTypes
{
    // Public (Этап 2)
    public const string DISH              = "Dish";
    public const string RECIPE_STEP       = "RecipeStep";
    public const string CATEGORY_ICON     = "CategoryIcon";
    public const string INGREDIENT_IMAGE  = "IngredientImage";

    // Personal (Этап 5)
    public const string USER_AVATAR       = "UserAvatar";

    // TODO: Этап 6+
    // public const string BUSINESS_LOGO = "BusinessLogo";
    // public const string USER_DOCUMENT = "UserDocument";
}
```

**Правило маппинга `EntityType → DataCategory`** реализуется отдельным методом в `Media.Application`:

```csharp
public static MediaDataCategory ResolveDataCategory(string entityType) => entityType switch
{
    MediaEntityTypes.USER_AVATAR => MediaDataCategory.Personal,
    _ => MediaDataCategory.Public
};
```

**Почему константы здесь, а не в Common.Domain:**
- Эти значения — часть контракта модуля Media. В Common они были бы избыточны для модулей, не работающих с медиа.
- При будущем выделении Media в микросервис — константы уходят вместе с модулем, и потребители (Dishes, Users) получают их через NuGet-пакет контрактов Media.

**Правило использования:** модули, работающие с медиа, **никогда не передают магические строки**. Только константы из этого класса.

---

## 9. Абстракция хранилища `IFileStorage`

**Расположение:** `Media.Application/Abstractions/IFileStorage.cs`.

**Расположение реализаций:** `Media.Infrastructure/Storage/`.

### Интерфейс

```csharp
namespace GastronomePlatform.Modules.Media.Application.Abstractions;

/// <summary>
/// Абстракция низкоуровневого хранилища файлов.
/// Реализации: LocalFileStorage (Этап 2), S3FileStorage (Этап 8+).
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Имя провайдера ("local", "s3") — сохраняется в MediaFile.StorageProvider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Сохранение потока. Возвращает ключ для последующего доступа.
    /// </summary>
    Task<Result<string>> SaveAsync(
        Stream content,
        string storageKey,
        string contentType,
        CancellationToken ct = default);

    /// <summary>
    /// Получение потока файла. Вызывающий обязан закрыть stream.
    /// </summary>
    Task<Result<Stream>> OpenReadAsync(
        string storageKey,
        CancellationToken ct = default);

    /// <summary>
    /// Проверка существования без чтения содержимого.
    /// </summary>
    Task<bool> ExistsAsync(
        string storageKey,
        CancellationToken ct = default);

    /// <summary>
    /// Удаление файла. Идемпотентно: отсутствие файла не считается ошибкой.
    /// </summary>
    Task<Result> DeleteAsync(
        string storageKey,
        CancellationToken ct = default);
}
```

### LocalFileStorage (Этап 2)

- Базовый путь из конфигурации: `Media:Storage:LocalBasePath` (по умолчанию `/data/media`).
- В Docker Compose — named volume `media-data:/data/media`.
- При `SaveAsync` — создаёт директории по пути рекурсивно.
- `OpenReadAsync` возвращает `FileStream` с `FileAccess.Read`, `FileShare.Read`.
- `ProviderName => "local"`.

### Путь генерируется до вызова `SaveAsync`

Генератором storage-ключа занимается **`Media.Application`**, не само хранилище. Это позволяет:
- Разные категории данных → разные префиксы (и потенциально разные buckets на Этапе 8+).
- Легко менять схему путей без правки storage-реализаций.

```csharp
public interface IStorageKeyGenerator
{
    string Generate(MediaDataCategory category, string entityType, Guid mediaId, string extension);
}

// Пример реализации:
// Public / Dish / 2026-04 / <guid>.jpg → "public/dishes/2026/04/<guid>.jpg"
```

### S3FileStorage (Этап 8+)

- Та же сигнатура, другая реализация через `AWSSDK.S3`.
- `ProviderName => "s3"`.
- Presigned URL для прямой отдачи клиенту (обходя контроллер) — отдельная возможность через расширенный интерфейс `ISignedUrlStorage : IFileStorage`.

---

## 10. Межмодульный контракт `IMediaService`

**Расположение:** `Media.Application/Contracts/IMediaService.cs`.

Контракт, который используют Dishes, Users и другие модули для взаимодействия с Media.

### Интерфейс

```csharp
namespace GastronomePlatform.Modules.Media.Application.Contracts;

/// <summary>
/// Публичный контракт модуля Media для межмодульного взаимодействия.
/// При выделении Media в микросервис — заменяется на HTTP-клиент без изменений у потребителей.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Метаданные файла без содержимого.
    /// </summary>
    Task<Result<MediaMetadataDto>> GetMetadataAsync(
        Guid mediaId,
        CancellationToken ct = default);

    /// <summary>
    /// Batch-запрос для каталогов (минимизация N+1).
    /// </summary>
    Task<Result<IReadOnlyDictionary<Guid, MediaMetadataDto>>> GetMetadataBatchAsync(
        IReadOnlyCollection<Guid> mediaIds,
        CancellationToken ct = default);

    /// <summary>
    /// Привязка файла к сущности с проверкой владельца (eager attach).
    /// Вызывается из Handler'ов при создании/обновлении сущностей.
    /// </summary>
    Task<Result> AttachToEntityAsync(
        Guid mediaId,
        Guid actorUserId,
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Отвязка. Вызывается при смене главного фото и т.п.
    /// После отвязки файл становится orphan и удаляется через ExpiresAt.
    /// </summary>
    Task<Result> DetachFromEntityAsync(
        Guid mediaId,
        CancellationToken ct = default);

    /// <summary>
    /// Каскадное удаление всех файлов сущности.
    /// Вызывается при удалении Dish, UserProfile и т.п.
    /// </summary>
    Task<Result> DeleteByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken ct = default);
}

public sealed record MediaMetadataDto(
    Guid Id,
    Guid? OwnerUserId,
    MediaDataCategory DataCategory,
    string? EntityType,
    Guid? EntityId,
    int? Width,
    int? Height,
    MediaStatus Status,
    string ContentType);
```

### Правила проверки в `AttachToEntityAsync`

1. Медиа существует → иначе `MEDIA.NOT_FOUND`.
2. Медиа в статусе `Ready` → иначе `MEDIA.NOT_READY`.
3. Медиа ещё не привязано → иначе `MEDIA.ALREADY_ATTACHED` (предотвращает «угон»).
4. **Владелец совпадает**:
   - Если `media.OwnerUserId IS NOT NULL` → должен равняться `actorUserId` → иначе `MEDIA.NOT_OWNED`.
   - Если `media.OwnerUserId IS NULL` (системный файл) → `actorUserId` должен иметь роль Admin → иначе `MEDIA.NOT_OWNED`.
5. `EntityType` — известная константа из `MediaEntityTypes` → иначе `MEDIA.UNKNOWN_ENTITY_TYPE`.

### Схема использования из Dishes

```csharp
// В CreateDishCommandHandler (после валидации и создания Draft):
var attachResult = await _mediaService.AttachToEntityAsync(
    cmd.MainImageId,
    authorUserId,
    MediaEntityTypes.DISH,
    newDish.Id,
    ct);

if (attachResult.IsFailure)
{
    // Откат — удаление черновика блюда
    return attachResult.Error;
}
```

При `DeleteDishCommand`:

```csharp
await _mediaService.DeleteByEntityAsync(MediaEntityTypes.DISH, dishId, ct);
// Удаляет все файлы: MainImage, все RecipeStep.Image
```

### Ошибки `MediaErrors`

```csharp
public static class MediaErrors
{
    public static readonly Error NotFound =
        Error.NotFound("MEDIA.NOT_FOUND", "Медиафайл не найден.");

    public static readonly Error NotReady =
        Error.Validation("MEDIA.NOT_READY", "Медиафайл ещё не готов к использованию.");

    public static readonly Error AlreadyAttached =
        Error.Conflict("MEDIA.ALREADY_ATTACHED", "Медиафайл уже привязан к сущности.");

    public static readonly Error NotOwned =
        Error.Forbidden("MEDIA.NOT_OWNED", "У вас нет прав на использование этого файла.");

    public static readonly Error UnknownEntityType =
        Error.Validation("MEDIA.UNKNOWN_ENTITY_TYPE", "Неизвестный тип сущности-владельца.");

    public static readonly Error InvalidFileType =
        Error.Validation("MEDIA.INVALID_FILE_TYPE", "Тип файла не поддерживается.");

    public static readonly Error FileTooLarge =
        Error.Validation("MEDIA.FILE_TOO_LARGE", "Размер файла превышает допустимый.");

    public static readonly Error InvalidImageDimensions =
        Error.Validation("MEDIA.INVALID_DIMENSIONS", "Размер изображения не соответствует требованиям.");

    public static readonly Error UploadFailed =
        Error.Failure("MEDIA.UPLOAD_FAILED", "Не удалось сохранить файл.");
}
```

---

## 11. Валидации

### 11.1. Пользовательский upload (`UploadFileCommand`)

Валидации применяются в **FluentValidation-валидаторе** + дополнительные runtime-проверки в handler.

| Проверка | Значение | Уровень |
|---|---|---|
| MIME whitelist | `image/jpeg`, `image/png` | Validator |
| Magic bytes (сигнатура файла) | Должны совпадать с заявленным MIME | Handler (runtime) |
| Максимальный размер | 10 MB | Validator |
| Минимальный размер | 5 KB | Validator |
| Max dimensions | 4096×4096 px | Handler (после чтения через ImageSharp) |
| Min dimensions | 100×100 px | Handler |
| Запрет EXE/ZIP сигнатур | `MZ`, `PE\0\0`, `PK\x03\x04` | Handler |

### 11.2. Системный upload (`UploadSystemFileCommand`, только Admin)

Админ может загрузить SVG для иконок категорий. Отдельный use case с отдельной валидацией.

| Проверка | Значение |
|---|---|
| MIME whitelist | `image/jpeg`, `image/png`, `image/svg+xml` |
| SVG sanitization | Обязательно — удаление `<script>`, `on*` атрибутов |
| Максимальный размер | 2 MB |
| Max dimensions (для JPEG/PNG) | 1024×1024 px — иконки не бывают больше |

**SVG sanitization** — ключевой момент. Используем библиотеку `HtmlSanitizer` или `SvgSanitizer`. Без очистки SVG — это XSS-уязвимость при прямой отдаче клиенту.

### 11.3. Иерархия команд

```
Media.Application/Commands/
├── UploadFile/            ← пользовательский (роль User+)
│   ├── UploadFileCommand.cs
│   ├── UploadFileCommandHandler.cs
│   └── UploadFileCommandValidator.cs
├── UploadSystemFile/      ← админский (роль Admin)
│   ├── UploadSystemFileCommand.cs
│   ├── UploadSystemFileCommandHandler.cs
│   └── UploadSystemFileCommandValidator.cs
├── DeleteFile/
└── ...
```

На контроллере разные эндпоинты:
- `POST /api/media/upload` — `[Authorize]`
- `POST /api/media/system/upload` — `[Authorize(Roles = "Admin")]` (или политика)

### 11.4. Библиотеки для реализации

| Задача | Библиотека | Лицензия |
|---|---|---|
| Обработка изображений, thumbnails | `SixLabors.ImageSharp` | ~~Apache 2 до v2, коммерческая с v3~~. На Этапе 2 — v2.1.x (Apache 2) |
| Детектирование MIME по сигнатуре | Собственный helper по magic bytes (или `MimeDetective`) | MIT |
| SVG sanitization | `HtmlSanitizer` | MIT |

> ⚠️ `SixLabors.ImageSharp` начиная с v3 требует коммерческой лицензии для некоторых кейсов. Закрепляем в Directory.Build.props версию v2.1.x чтобы не словить сюрприз.

---

## 12. Структура файлов на диске и в S3

### На Этапе 2 (LocalFileStorage, `/data/media/`)

```
/data/media/
├── public/
│   ├── dishes/
│   │   └── 2026/04/<mediaId>.jpg
│   ├── recipe-steps/
│   │   └── 2026/04/<mediaId>.jpg
│   ├── category-icons/
│   │   └── <mediaId>.svg
│   └── ingredient-images/
│       └── <mediaId>.jpg
│
├── personal/
│   └── user-avatars/
│       └── <userGuid>/<mediaId>.jpg
│
└── thumbnails/
    ├── public/
    │   ├── dishes/<mediaId>_medium.jpg
    │   └── ...
    └── personal/
        └── user-avatars/<mediaId>_medium.jpg
```

**Принципы:**
- Разделение `public` / `personal` на верхнем уровне — готовим почву к разным bucket-ам по регионам (152-ФЗ) на Этапе 8+.
- Сегментация по годам-месяцам внутри больших категорий (`dishes`, `recipe-steps`) — избегаем папок с миллионами файлов.
- `user-avatars` — без годовой сегментации, но сгруппированы по `<userGuid>` для удобного удаления всех данных пользователя.
- Thumbnails в отдельном дереве — проще управлять и очищать.

### Docker Compose

```yaml
services:
  api:
    volumes:
      - media-data:/data/media

volumes:
  media-data:
```

### На Этапе 8+ (S3)

- Bucket `gastronome-media-public-ru` — Public файлы в российском регионе.
- Bucket `gastronome-media-personal-ru` — Personal файлы. Отдельный bucket → отдельные ACL, отдельное шифрование, отдельное логирование.
- Ключи внутри bucket'ов — идентичны путям локального хранилища (без префикса `public/` и `personal/`).
- При необходимости (иностранные пользователи) — дополнительные bucket'ы `-eu`, `-us` с правилами выбора по `UserProfile.Country`.

---

## 13. Конфигурация

`appsettings.json` (без секретов):

```json
{
  "Media": {
    "Storage": {
      "Provider": "Local",
      "LocalBasePath": "/data/media"
    },
    "Validation": {
      "User": {
        "MaxSizeBytes": 10485760,
        "MinSizeBytes": 5120,
        "AllowedMimeTypes": ["image/jpeg", "image/png"],
        "MaxImageDimension": 4096,
        "MinImageDimension": 100
      },
      "System": {
        "MaxSizeBytes": 2097152,
        "AllowedMimeTypes": ["image/jpeg", "image/png", "image/svg+xml"],
        "MaxImageDimension": 1024
      }
    },
    "Thumbnails": {
      "MediumSize": 400,
      "JpegQuality": 85
    },
    "Orphan": {
      "ExpirationHours": 24
    },
    "Limits": {
      "MaxFilesPerUser": 1000,
      "MaxTotalSizeMbPerUser": 500
    }
  }
}
```

Типизируется как `MediaOptions` через `IOptions<T>` в `Media.Infrastructure`.

---

## 14. Заметки на будущее (TODO в коде)

### 14.1. В `MediaFile.cs`

```csharp
// TODO: DataCategory routing — Этап 8+
//   На основе DataCategory и UserProfile.Country выбирать StorageProvider/bucket
//   для соответствия 152-ФЗ. Сейчас все файлы идут в единственный local provider.

// TODO: Connection между MediaFile и real Entity — Этап 8+
//   Фоновая задача проверяет, что EntityId по-прежнему существует в целевой схеме.
//   Несуществующие ссылки → soft delete файла.

// TODO: Video support — Этап 8+
//   Поле DurationSeconds уже есть. Добавить:
//   - Асинхронное транскодирование (через MediaProcessingJob)
//   - Preview (кадр из середины видео)
//   - Adaptive bitrate (HLS/DASH)
```

### 14.2. В `UploadFileCommandHandler.cs`

```csharp
// TODO: Генерация thumbnails — Этап 8+
//   Сейчас: синхронная генерация Medium-размера в том же запросе.
//   Целевое: публикация MediaUploadedEvent → отдельный воркер генерирует
//   Small/Medium/Large в форматах Jpeg/WebP/Avif параллельно.

// TODO: Watermark — Этап 8+
//   Проверка WatermarkRule пользователя, наложение при upload.
```

### 14.3. В `MediaController.cs`

```csharp
// TODO: Подписка на Premium-контент — Этап 3
//   Для RecipeStep-изображений проверять активную подписку через ISubscriptionService.
//   Сейчас: все файлы Ready-статуса отдаются всем авторизованным пользователям.

// TODO: Rate limiting — Этап 4
//   /api/media/upload ограничить: 20 запросов/мин на пользователя.
//   /api/media/{id} — 200 запросов/мин на IP.

// TODO: Presigned URL для S3 — Этап 8+
//   Для крупных файлов (видео) возвращать 302 Redirect на presigned S3 URL.
```

### 14.4. Фоновые задачи (Hosted Services) — Этап 8+

```csharp
// TODO: OrphanCleanupService — Этап 8+ (пока вручную при необходимости)
//   Каждые 4 часа: находит MediaFile WHERE EntityType IS NULL
//   AND ExpiresAt < now → soft delete.

// TODO: HardDeleteService — Этап 8+
//   Каждые 24 часа: MediaFile WHERE Status = Deleted
//   AND DeletedAt < now - 7 days → физически удаляет файл из хранилища,
//   затем DELETE из БД. 7 дней — окно на случай ошибочного удаления.

// TODO: IntegrityCheckService — Этап 8+
//   Еженедельно: проверка что все EntityId по-прежнему существуют.
```

### 14.5. Новые таблицы — README.md в `Media.Domain/Entities/`

```markdown
# TODO — будущие сущности модуля Media

- **MediaAccessLog** (Этап 8+) — журнал обращений к файлам
- **MediaProcessingJob** (Этап 8+) — отслеживание асинхронных job'ов обработки
- **WatermarkRule** (Этап 8+) — правила водяных знаков
```

---

## 15. Что дальше

### 15.1. По модулю Media — что отложено на обсуждение

- **Прямые публичные URL через nginx** — архитектурное решение для SEO/производительности. Обсуждается отдельно при проектировании Этапа 4 (веб-интерфейс).
- **Watermark** — целиком Deferred.
- **Data residency routing** — маршрутизация по регионам согласно 152-ФЗ. Сейчас только заложена структура полей.

### 15.2. Дальнейшая работа над Этапом 2

Теперь, когда доменные модели Dishes и Media согласованы, следующие шаги:

1. **Полный список Use Cases** для Dishes и Media с разбивкой:
   - **Core** — реализуется в коде на Этапе 2
   - **Stub with XML** — заглушка метода с XML-документацией назначения, возвращает `Error.Failure("NOT_IMPLEMENTED", "...")`
   - **Deferred** — не создаётся даже заглушка
2. **Каркас проектов** — `Dishes.Domain`, `Dishes.Application`, `Dishes.Infrastructure` + три аналогичных для Media.
3. **AssemblyReference, Errors, ServiceCollectionExtensions** — по шаблону из `08_Разработка-(Development-Guide)`.
4. **Миграции** — сначала Dishes (там больше таблиц), потом Media.
5. **Порядок реализации Core-сущностей**:
   - Сначала справочники: `MeasureUnit`, `Category`, `Tag`.
   - Затем `Ingredient` (зависит от `MeasureUnit` и `Nutrition`).
   - Затем `MediaFile` + `IFileStorage` + загрузка файлов.
   - Затем `Dish` + `Recipe` (зависит от `Category`, `Tag`, `Ingredient`, `MediaFile`).
   - В конце — сценарии поиска, пагинации, каталога.

---

## Связанные страницы

- [[05_Дорожная-карта]] — Этап 2, общий план
- [[02_Архитектура]] — архитектурные паттерны
- [[13_Структура-проекта]] — структура кодовой базы
- [[Этап-2_Модуль-Dishes_Доменная-модель]] — потребитель Media через `IMediaService`
- [[08_Разработка-(Development-Guide)]] — шаблон создания модуля
