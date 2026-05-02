# POL-002: Media Access Policy (Право на чтение медиафайла)

> **Расположение в проекте:** `docs/public/policies/POL-002-media-access.md` (после создания папки `policies/`)
>
> **Применяется к:** Use Cases модуля Media, выполняющие чтение файла (оригинала, миниатюры, метаданных)

---

## Метаданные

| Атрибут | Значение |
|---------|----------|
| **ID** | POL-002 |
| **Название** | Media Access Policy |
| **Статус** | Active |
| **Версия** | 1.0 |
| **Дата создания** | 2026-04-26 |
| **Последнее изменение** | 2026-04-26 |
| **Этапы реализации** | Этап 2 (базовая), Этап 3+ (проверка подписки), Этап 8+ (расширенные правила Personal) |

---

## 1. Назначение политики

Политика определяет, какие пользователи имеют право читать медиафайлы — скачивать оригиналы, миниатюры и метаданные.

Эта политика — **единый источник истины** для авторизации операций чтения в модуле Media. Use Cases ссылаются на политику, а не дублируют правила.

---

## 2. Применимость

### 2.1. UC, использующие политику

Политика применяется ко всем Query-сценариям модуля Media, отдающим клиенту содержимое или метаданные файла:

- `UC-MED-002` — Получить файл по ID (стриминг оригинала)
- `UC-MED-003` — Получить миниатюру файла
- `UC-MED-004` — Получить метаданные файла

### 2.2. UC, НЕ использующие политику

- `UC-MED-001 UploadFile`, `UC-MED-005 DeleteOwnFile` — изменения, не чтение. Используют POL-003.
- `UC-MED-006 GetMyFiles` — список файлов пользователя; авторизация — Authenticated + проверка `OwnerUserId == ActorUserId` (простая, политика не нужна).
- `UC-MED-101 UploadSystemFile`, `UC-MED-102 DeleteAnyFile`, `UC-MED-103 GetUserFilesAsAdmin` — админские UC; авторизация — атрибут `[Authorize(Roles = "Admin")]`.
- `UC-MED-200..204` — внутренние методы `IMediaService`, вызываются из доверенных модулей; авторизация на уровне UC модуля-вызывающего.

---

## 3. Правило (Этап 2)

Право чтения зависит от трёх факторов: **категории данных файла** (`DataCategory`), **типа запрашиваемого ресурса** (оригинал / миниатюра / метаданные) и **роли вызывающего**.

### 3.1. Сводная таблица правил

| `DataCategory` | Тип ресурса | Гость (неавторизованный) | Авторизованный | Owner / Admin |
|----------------|-------------|--------------------------|-----------------|---------------|
| `Public` | Оригинал | ✓ | ✓ | ✓ |
| `Public` | Миниатюра | ✓ | ✓ | ✓ |
| `Public` | Метаданные | ✓ | ✓ | ✓ |
| `Personal` | Оригинал | ✗ (401) | ✓ | ✓ |
| `Personal` | Миниатюра | ✓ | ✓ | ✓ |
| `Personal` | Метаданные | ✗ (401) | ✓ | ✓ |

> **Примечание про миниатюры Personal:** миниатюра аватара пользователя доступна всем, потому что это публичная часть UI (комментарии, лента, профили). Оригинал (полное разрешение) — только авторизованным, чтобы анонимные сборщики данных не индексировали лица.

### 3.2. Дополнительные ограничения по статусу файла

Независимо от роли, файлы в статусах `Deleted` и `Failed` — **404 Not Found**.

Файлы в статусах `Uploaded` / `Processing` (ещё не готовы к отдаче) — возвращают **425 Too Early** или **404** (выбор реализации; 425 семантически точнее, но не все клиенты его понимают).

### 3.3. Решение «authorized vs anonymous»

На уровне ASP.NET Core:
- Эндпоинт UC-MED-002 (получение оригинала) — атрибут `[AllowAnonymous]`. Внутри handler'а проверка `DataCategory`: для `Personal` без аутентификации — `401 Unauthorized`.
- Эндпоинт UC-MED-003 (миниатюра) — `[AllowAnonymous]` без дополнительных проверок.
- Эндпоинт UC-MED-004 (метаданные) — аналогично UC-MED-002.

---

## 4. Расширения политики (будущие этапы)

### 4.1. Этап 3+ — Проверка подписки для Premium-контента

С появлением модуля Subscriptions добавляется проверка подписки для определённых типов файлов. Конкретно:

- `EntityType = "RecipeStep"` — фотографии шагов рецепта. Доступ только Premium+ или автору блюда.
- (Возможно) `EntityType = "RecipeStep" + DataCategory = Public` — миниатюры остаются доступными всем (тизер), оригинал — Premium+.

Реализация — через дополнительную проверку в начале политики:

```csharp
if (file.EntityType == MediaEntityTypes.RECIPE_STEP)
{
    var hasAccess = await _subscriptionService.HasPremiumAccessAsync(actorUserId)
        || await _dishOwnershipService.IsAuthorAsync(actorUserId, file.EntityId);
    if (!hasAccess) return Result.Failure(MediaErrors.PremiumRequired);
}
```

### 4.2. Этап 8+ — Расширенные правила для Personal

Возможные изменения:
- Правила приватности профиля (UserProfile.PrivacySetting): «открытый профиль» / «только подписчики» / «только друзья». Аватар следует тем же правилам.
- Опция «скрыть мой аватар от поисковиков» (User-Agent-фильтр).

Эти расширения — отдельный ADR, проектируются вместе с расширением модуля Users.

---

## 5. Реализация в коде

### 5.1. Структура

Политика реализуется в `Media.Application/Authorization/MediaAccessPolicy.cs`:

```csharp
namespace GastronomePlatform.Modules.Media.Application.Authorization;

public interface IMediaAccessPolicy
{
    /// <summary>
    /// Проверяет, может ли actor получить запрошенный ресурс файла.
    /// </summary>
    Task<Result> AuthorizeReadAsync(
        Guid mediaId,
        MediaResourceKind resourceKind,    // Original / Thumbnail / Metadata
        Guid? actorUserId,                  // null для гостей
        IReadOnlyCollection<string> actorRoles,
        CancellationToken ct = default);
}

public enum MediaResourceKind
{
    Original = 0,
    Thumbnail = 1,
    Metadata = 2
}
```

### 5.2. Использование в контроллере

```csharp
[HttpGet("{id}")]
[AllowAnonymous]
public async Task<IActionResult> GetFile(Guid id, CancellationToken ct)
{
    var authResult = await _accessPolicy.AuthorizeReadAsync(
        id,
        MediaResourceKind.Original,
        _currentUser.UserId,           // null если не авторизован
        _currentUser.Roles,
        ct);

    if (authResult.IsFailure)
        return ApiControllerBase.Problem(authResult.Error);

    // ... стриминг файла ...
}
```

### 5.3. Ошибки

| Код | HTTP | Условие |
|-----|------|---------|
| `MEDIA.NOT_FOUND` | 404 | Файл не существует, удалён или в статусе Failed |
| `MEDIA.NOT_READY` | 425 | Файл в статусе Uploaded / Processing |
| `MEDIA.UNAUTHORIZED` | 401 | Personal-файл, но actor — гость |
| `MEDIA.PREMIUM_REQUIRED` | 402 / 403 | Этап 3+: для RecipeStep требуется Premium-подписка |

---

## 6. Тесты политики

Минимальный набор unit-тестов для `MediaAccessPolicy`:

**Public файлы:**
- Гость → оригинал → разрешено
- Гость → миниатюра → разрешено
- Авторизованный → оригинал → разрешено

**Personal файлы:**
- Гость → оригинал → 401
- Гость → миниатюра → разрешено
- Авторизованный (не владелец) → оригинал → разрешено
- Владелец → оригинал → разрешено

**По статусам:**
- Файл `Deleted` → любой запрос → 404
- Файл `Failed` → любой запрос → 404
- Файл `Uploaded` → любой запрос → 425
- Файл `Processing` → любой запрос → 425

**Этап 3+** (тесты добавятся при появлении подписок):
- Гость → оригинал RecipeStep → 401 / 403
- User (без Premium) → оригинал RecipeStep → 403 PREMIUM_REQUIRED
- Premium → оригинал RecipeStep → разрешено
- Автор блюда → оригинал своего RecipeStep → разрешено

Тесты пишутся в `Media.UnitTests/Authorization/MediaAccessPolicyTests.cs`.

---

## 7. История изменений

| Версия | Дата | Изменение |
|--------|------|-----------|
| 1.0 | 2026-04-26 | Первая версия политики (Этап 2) |

---

## 8. Связанные документы

- `domain-model.md` модуля Media — определения сущности `MediaFile` и enum `MediaDataCategory`
- `use-cases-media-draft.md` — Use Cases, использующие политику
- `POL-001-dish-ownership.md` — образец политики; общие правила POL-N см. в Wiki
- `POL-003-media-ownership.md` — политика модификации файлов

---

## 9. Открытые вопросы и пометки

- **Q-1.** Использование `425 Too Early` vs `404 Not Found` для статуса `Uploaded`/`Processing` — выбрать при реализации. 425 семантически точнее, но требует поддержки клиентом; 404 — универсальнее.
- **Q-2.** Этап 3+: должна ли миниатюра RecipeStep требовать Premium-подписку, или только оригинал? Вариант «миниатюра — тизер для всех» лучше для UX, хуже для коммерции. Решение — на этапе проектирования модуля Subscriptions.
- **Q-3.** На Этапе 8+ — нужны ли отдельные правила «приватный профиль» для аватаров? См. UserProfile.PrivacySetting.
