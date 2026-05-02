# POL-003: Media Ownership Policy (Право на изменение медиафайла)

> **Расположение в проекте:** `docs/public/policies/POL-003-media-ownership.md` (после создания папки `policies/`)
>
> **Применяется к:** Use Cases модуля Media, выполняющие модификацию или удаление файла его владельцем

---

## Метаданные

| Атрибут | Значение |
|---------|----------|
| **ID** | POL-003 |
| **Название** | Media Ownership Policy |
| **Статус** | Active |
| **Версия** | 1.0 |
| **Дата создания** | 2026-04-26 |
| **Последнее изменение** | 2026-04-26 |
| **Этапы реализации** | Этап 2 (базовая), Этап 8+ (роли Moderator, Support) |

---

## 1. Назначение политики

Политика определяет, какие пользователи имеют право модифицировать или удалять медиафайл — то есть менять его метаданные или переводить в состояние `Deleted`.

Эта политика — **единый источник истины** для авторизации операций модификации/удаления в модуле Media. Use Cases ссылаются на политику, а не дублируют правила.

---

## 2. Применимость

### 2.1. UC, использующие политику

На Этапе 2 политика применяется к одному пользовательскому UC:

- `UC-MED-005` — Удалить свой файл (пользователь)

При появлении в будущем дополнительных UC модификации (например, «обновить метаданные файла», «переименовать файл», «заменить содержимое») — они также будут использовать эту политику.

### 2.2. UC, НЕ использующие политику

- `UC-MED-001 UploadFile` — создание нового файла. Политика владения тут неприменима (файл ещё не существует, у него нет владельца). Авторизация — Authenticated.
- `UC-MED-002, 003, 004, 006` — чтение. Используют POL-002 (для чтения других пользователей) или Authenticated + проверку `OwnerUserId == ActorUserId` (для UC-MED-006).
- `UC-MED-101 UploadSystemFile`, `UC-MED-102 DeleteAnyFile`, `UC-MED-103 GetUserFilesAsAdmin` — админские UC; авторизация — атрибут `[Authorize(Roles = "Admin")]`.
- `UC-MED-200..204` — внутренние методы `IMediaService`; вызываются из доверенных модулей. Логика владения внутри этих методов есть (например, в `AttachToEntityAsync`), но это часть внутреннего контракта, не самостоятельная политика.

---

## 3. Правило (Этап 2)

Действие разрешено, если выполняется хотя бы одно из следующих условий:

### 3.1. Условие «Owner»

`MediaFile.OwnerUserId IS NOT NULL` и `MediaFile.OwnerUserId == ActorUserId`.

> **Системные файлы** (`OwnerUserId IS NULL`, например иконки категорий) — пользователь **не может** удалить через UC-MED-005, даже если он Admin (для админского удаления используется UC-MED-102).

### 3.2. Условие «Admin»

Пользователь имеет роль `Admin` (из `PlatformRoles`).

> Для Admin фактически предоставляется доступ к UC-MED-005 как удобный способ удаления собственных файлов админа. Для удаления чужих файлов есть отдельный UC-MED-102 без проверки владения.

---

## 4. Дополнительные ограничения по состоянию файла

Помимо проверки прав, выполнение действия зависит от текущего состояния файла.

### 4.1. По полю `MediaFile.Status`

| Status | Owner | Admin |
|--------|-------|-------|
| `Uploaded`, `Processing` | Может удалить | Может удалить |
| `Ready` | Может удалить (с условием по `EntityType`, см. ниже) | Может удалить |
| `Failed` | Может удалить | Может удалить |
| `Deleted` | **Запрещено** (повторное удаление) | **Запрещено** |

### 4.2. По полю `MediaFile.EntityType`

Если файл всё ещё привязан к сущности (`EntityType IS NOT NULL`) — удаление запрещено и возвращается ошибка `MEDIA.STILL_ATTACHED`. Сначала автор должен либо открепить файл от сущности (через UC модуля-владельца), либо удалить саму сущность (что приведёт к каскадному `IMediaService.DeleteByEntityAsync`).

> **Почему такое ограничение:** если у блюда `Status = Published` и `MainImageId` ссылается на этот файл, удаление файла «из-под блюда» нарушит инвариант «опубликованное блюдо имеет MainImage». Должен быть явный шаг — отвязать в Dishes, потом удалить в Media.

---

## 5. Расширения политики (будущие этапы)

### 5.1. Этап 8+ — Роль Moderator

В `PlatformRoles` добавляется роль `Moderator`. Появляется в условии «Admin» наравне с админом — для модерации контента (например, удаление загруженных, но не привязанных файлов с нарушающим контентом).

### 5.2. Этап 8+ — Условие «Support с открытым тикетом»

Появляется роль `Support` и модуль обращений. Условие:

```
Пользователь имеет роль Support
И существует открытый тикет, связанный с этим mediaId или с владельцем файла
```

Используется для технической поддержки пользователей («помогите удалить, не получается через UI»).

### 5.3. Будущие UC модификации

Если в будущем появятся UC «обновить метаданные файла» или «заменить содержимое» — они также будут использовать POL-003. Политика расширится дополнительными ограничениями по `Status` (например, нельзя редактировать файл в статусе `Processing`).

---

## 6. Реализация в коде

### 6.1. Структура

Политика реализуется в `Media.Application/Authorization/MediaOwnershipPolicy.cs`:

```csharp
namespace GastronomePlatform.Modules.Media.Application.Authorization;

public interface IMediaOwnershipPolicy
{
    /// <summary>
    /// Проверяет, может ли actor модифицировать или удалить файл.
    /// </summary>
    Task<r> AuthorizeModificationAsync(
        Guid mediaId,
        Guid actorUserId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken ct = default);
}
```

### 6.2. Использование в Handler'е

```csharp
public async Task<r> Handle(DeleteOwnFileCommand cmd, CancellationToken ct)
{
    // 1. Проверка политики POL-003
    var authResult = await _ownershipPolicy.AuthorizeModificationAsync(
        cmd.MediaId,
        _currentUser.UserId,
        _currentUser.Roles,
        ct);

    if (authResult.IsFailure)
        return authResult;

    // 2. Загрузка файла, проверка состояния, soft delete
    var file = await _mediaRepository.GetByIdAsync(cmd.MediaId, ct);
    if (file is null) return MediaErrors.NotFound;
    if (file.Status == MediaStatus.Deleted) return MediaErrors.AlreadyDeleted;
    if (file.EntityType != null) return MediaErrors.StillAttached;

    file.SoftDelete(_clock);
    await _unitOfWork.SaveChangesAsync(ct);

    return Result.Success();
}
```

### 6.3. Ошибки

| Код | HTTP | Условие |
|-----|------|---------|
| `MEDIA.NOT_FOUND` | 404 | Файл не существует |
| `MEDIA.FORBIDDEN_NOT_OWNER` | 403 | ActorUserId ≠ OwnerUserId и нет роли Admin |
| `MEDIA.FORBIDDEN_SYSTEM_FILE` | 403 | Файл системный (`OwnerUserId IS NULL`); пользовательское удаление недоступно |
| `MEDIA.STILL_ATTACHED` | 409 | Файл привязан к сущности; сначала открепить |
| `MEDIA.ALREADY_DELETED` | 409 | Файл уже в статусе Deleted |

Коды ошибок — в `MediaErrors.cs` модуля Media.

---

## 7. Тесты политики

Минимальный набор unit-тестов для `MediaOwnershipPolicy`:

- Owner может удалить свой файл → разрешено
- Owner НЕ может удалить чужой файл → 403 NOT_OWNER
- Owner НЕ может удалить системный файл (через UC-MED-005) → 403 SYSTEM_FILE
- Admin может удалить любой файл (включая системный) → разрешено
- Несуществующий файл → 404 NOT_FOUND
- Файл в статусе Deleted → 409 ALREADY_DELETED
- Файл с заполненным EntityType → 409 STILL_ATTACHED
- (Этап 8+) Moderator → разрешено
- (Этап 8+) Support без тикета → 403 NOT_OWNER
- (Этап 8+) Support с открытым тикетом → разрешено

Тесты пишутся в `Media.UnitTests/Authorization/MediaOwnershipPolicyTests.cs`.

---

## 8. История изменений

| Версия | Дата | Изменение |
|--------|------|-----------|
| 1.0 | 2026-04-26 | Первая версия политики (Этап 2) |

---

## 9. Связанные документы

- `domain-model.md` модуля Media — определения сущности `MediaFile` и enum `MediaStatus`
- `use-cases-media-draft.md` — Use Cases, использующие политику
- `POL-001-dish-ownership.md` — образец политики; общие правила POL-N см. в Wiki
- `POL-002-media-access.md` — политика чтения файлов

---

## 10. Открытые вопросы и пометки

- **Q-1.** На Этапе 8+ при появлении модуля обращений — как именно конструируется условие «Support с открытым тикетом для этого файла»? Тикет может быть привязан к пользователю-владельцу, к самому файлу, или к какой-то другой сущности. Решение — при проектировании модуля Support.
- **Q-2.** Если у пользователя превышен лимит `MaxFilesPerUser` или `MaxTotalSizeMbPerUser` (см. конфигурацию `Media.Limits` в `domain-model.md` модуля Media) — должна ли политика блокировать создание новых файлов? Это смежная политика, скорее не для POL-003 (она про модификацию), а про отдельную POL-N для upload. На Этапе 2 — лимиты ещё не активны.
