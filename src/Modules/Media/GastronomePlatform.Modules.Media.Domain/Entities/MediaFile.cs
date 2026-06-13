using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Domain.Constants;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Events;

namespace GastronomePlatform.Modules.Media.Domain.Entities
{
    /// <summary>
    /// Медиафайл — корень агрегата модуля Media. Содержит метаданные загруженного файла
    /// и ссылку на физический объект в хранилище (<c>IFileStorage</c>); коллекцию миниатюр
    /// разных размеров и форматов держит как часть агрегата.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Сам файл хранится в <c>IFileStorage</c>, а не в БД. В таблице — только метаданные
    /// и <see cref="StorageKey"/> для извлечения файла.
    /// </para>
    /// <para>
    /// Кросс-модульная привязка к сущности-владельцу (Dish, RecipeStep, UserAvatar, …)
    /// выполняется через пару <see cref="EntityType"/>+<see cref="EntityId"/> без
    /// FK-constraint в БД. Целостность обеспечивается на уровне приложения через
    /// <c>IMediaService</c> (модули-владельцы при удалении вызывают
    /// <c>DeleteByEntityAsync</c>).
    /// </para>
    /// <para>
    /// Жизненный цикл: <c>Uploaded → Processing → Ready</c>; <c>* → Failed</c> при сбое;
    /// <c>{Uploaded, Processing, Ready, Failed} → Deleted</c> при soft-удалении (с условием
    /// <see cref="EntityType"/> = NULL). Из <c>Deleted</c> нет переходов.
    /// </para>
    /// </remarks>
    public sealed class MediaFile : AggregateRoot<Guid>
    {
        #region Limits

        /// <summary>Максимальная длина <see cref="OriginalFileName"/>.</summary>
        public const int MAX_FILE_NAME_LENGTH = 255;

        #endregion

        // Backing field для коллекции миниатюр. Настраивается в MediaFileConfiguration
        // через HasField("_thumbnails") + PropertyAccessMode.Field.
        private readonly List<MediaThumbnail> _thumbnails = new();

        #region Properties

        /// <summary>
        /// Идентификатор пользователя, загрузившего файл. <see langword="null"/> для
        /// системных файлов (иконки категорий, фото ингредиентов справочника).
        /// Кросс-модульная логическая ссылка на <c>users.UserProfiles.UserId</c>.
        /// </summary>
        public Guid? OwnerUserId { get; private set; }

        /// <summary>
        /// Тип сущности-владельца (значение из <see cref="MediaEntityTypes"/>).
        /// <see langword="null"/>, пока файл — orphan. Заполняется при
        /// <see cref="AttachToEntity"/>.
        /// </summary>
        public string? EntityType { get; private set; }

        /// <summary>
        /// Идентификатор сущности в её собственном домене. Заполняется парно
        /// с <see cref="EntityType"/>.
        /// </summary>
        public Guid? EntityId { get; private set; }

        /// <summary>
        /// Категория данных (Public / Personal). Влияет на политику доступа
        /// (POL-002) и в будущем — на маршрутизацию хранилища (Этап 8+).
        /// </summary>
        public MediaDataCategory DataCategory { get; private set; }

        /// <summary>
        /// Тип медиа-контента (изображение / видео).
        /// </summary>
        public MediaType MediaType { get; private set; }

        /// <summary>
        /// MIME-тип файла, например <c>image/jpeg</c>.
        /// </summary>
        public string ContentType { get; private set; } = string.Empty;

        /// <summary>
        /// Исходное имя файла, как его прислал пользователь («borsch.jpg»).
        /// Хранится для UI и заголовка <c>Content-Disposition</c> при скачивании.
        /// </summary>
        public string OriginalFileName { get; private set; } = string.Empty;

        /// <summary>
        /// Код провайдера хранилища: <c>"local"</c> на Этапе 2, <c>"s3"</c> на Этапе 8+.
        /// Хранится как строка (не enum), чтобы добавлять провайдеров без миграций.
        /// </summary>
        public string StorageProvider { get; private set; } = string.Empty;

        /// <summary>
        /// Путь файла в хранилище (значение от <c>IStorageKeyGenerator</c>).
        /// Иммутабелен после создания — перемещение файла = новая запись с новым Id.
        /// </summary>
        public string StorageKey { get; private set; } = string.Empty;

        /// <summary>
        /// Размер исходного файла в байтах.
        /// </summary>
        public long SizeBytes { get; private set; }

        /// <summary>
        /// Ширина изображения в пикселях. <see langword="null"/> для видео или если не определена.
        /// </summary>
        public int? Width { get; private set; }

        /// <summary>
        /// Высота изображения в пикселях. <see langword="null"/> для видео или если не определена.
        /// </summary>
        public int? Height { get; private set; }

        /// <summary>
        /// Длительность видео в секундах. <see langword="null"/> для изображений (Этап 2 — всегда NULL).
        /// </summary>
        public int? DurationSeconds { get; private set; }

        /// <summary>
        /// Текущий статус жизненного цикла файла.
        /// </summary>
        public MediaStatus Status { get; private set; }

        /// <summary>
        /// Срок жизни orphan-файла. Устанавливается при создании в <c>utcNow + orphanTimeout</c>;
        /// обнуляется при <see cref="AttachToEntity"/>; переустанавливается при
        /// <see cref="DetachFromEntity"/>. Используется фоновой задачей UC-MED-210 (Этап 8+).
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; private set; }

        /// <summary>
        /// Момент привязки файла к сущности. <see langword="null"/> пока файл orphan.
        /// </summary>
        public DateTimeOffset? AttachedAt { get; private set; }

        /// <summary>
        /// Момент soft-delete. <see langword="null"/> пока файл активен.
        /// Используется UC-MED-211 для физического удаления через 7 дней (Этап 8+).
        /// </summary>
        public DateTimeOffset? DeletedAt { get; private set; }

        /// <summary>
        /// Момент создания записи в БД (он же — момент загрузки). Иммутабелен.
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Момент последнего изменения метаданных файла (статус, привязка, миниатюры).
        /// </summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        /// <summary>
        /// Миниатюры файла. Read-only коллекция; добавление — только через
        /// <see cref="AddThumbnail"/>.
        /// </summary>
        public IReadOnlyList<MediaThumbnail> Thumbnails => _thumbnails;

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private MediaFile() : base() { }

        /// <summary>
        /// Приватный конструктор, используется только из <see cref="Upload"/>.
        /// </summary>
        private MediaFile(
            Guid id,
            Guid? ownerUserId,
            MediaType mediaType,
            string contentType,
            string originalFileName,
            string storageProvider,
            string storageKey,
            long sizeBytes,
            int? width,
            int? height,
            int? durationSeconds,
            MediaDataCategory dataCategory,
            TimeSpan orphanTimeout,
            DateTimeOffset utcNow)
            : base(id)
        {
            OwnerUserId = ownerUserId;
            MediaType = mediaType;
            ContentType = contentType;
            OriginalFileName = originalFileName;
            StorageProvider = storageProvider;
            StorageKey = storageKey;
            SizeBytes = sizeBytes;
            Width = width;
            Height = height;
            DurationSeconds = durationSeconds;
            DataCategory = dataCategory;
            Status = MediaStatus.Uploaded;
            EntityType = null;
            EntityId = null;
            AttachedAt = null;
            DeletedAt = null;
            ExpiresAt = utcNow + orphanTimeout;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт запись медиафайла после успешного сохранения бинарника в хранилище.
        /// Вызывается из Application-хендлера UC-MED-001 / UC-MED-101 — после того как
        /// <c>IFileStorage.SaveAsync</c> вернул успешный результат и Application-слой
        /// рассчитал <paramref name="dataCategory"/> по <c>EntityType</c>.
        /// </summary>
        /// <remarks>
        /// Фабрика создаёт файл всегда в статусе <see cref="MediaStatus.Uploaded"/>
        /// и без привязки к сущности (<see cref="EntityType"/> = NULL,
        /// <see cref="ExpiresAt"/> = <paramref name="utcNow"/> + <paramref name="orphanTimeout"/>).
        /// Eager attach — отдельный шаг через <see cref="AttachToEntity"/>, вызываемый
        /// тем же хендлером при наличии целевой сущности.
        /// </remarks>
        /// <param name="id">
        /// Заранее сгенерированный идентификатор файла. Application-слой создаёт его до вызова
        /// <c>IFileStorage.SaveAsync</c>, чтобы использовать в пути хранилища через
        /// <c>IStorageKeyGenerator.Generate</c>, а затем передаёт сюда для согласованности.
        /// </param>
        /// <param name="ownerUserId">
        /// Идентификатор пользователя-загрузчика. <see langword="null"/> для системных файлов
        /// (UC-MED-101). Для <paramref name="dataCategory"/> = <see cref="MediaDataCategory.Personal"/>
        /// обязателен.
        /// </param>
        /// <param name="mediaType">Тип медиа-контента (изображение / видео).</param>
        /// <param name="contentType">MIME-тип файла, проверенный Application-слоем по whitelist + magic bytes.</param>
        /// <param name="originalFileName">Имя файла как его прислал клиент.</param>
        /// <param name="storageProvider">Код провайдера хранилища, в которое реально записан файл.</param>
        /// <param name="storageKey">Путь записи, возвращённый <c>IFileStorage.SaveAsync</c>.</param>
        /// <param name="sizeBytes">Фактический размер записанного файла.</param>
        /// <param name="width">Ширина изображения. <see langword="null"/> для видео или если неопределимо.</param>
        /// <param name="height">Высота изображения. <see langword="null"/> для видео или если неопределимо.</param>
        /// <param name="durationSeconds">Длительность видео в секундах. <see langword="null"/> для изображений.</param>
        /// <param name="dataCategory">Категория данных, рассчитанная Application-слоем по <c>EntityType</c>.</param>
        /// <param name="orphanTimeout">
        /// Длительность жизни orphan-файла без привязки. Передаётся из конфигурации
        /// <c>Media:Orphan:ExpirationHours</c> (по умолчанию 24 часа).
        /// </param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result{TValue}.Success(TValue)"/> с новым <see cref="MediaFile"/>
        /// и зарегистрированным событием <see cref="MediaUploadedEvent"/>; либо
        /// <see cref="Result{TValue}.Failure(Error)"/> с
        /// <see cref="MediaErrors.PersonalRequiresOwner"/>, если для категории Personal
        /// не передан <paramref name="ownerUserId"/>.
        /// </returns>
        public static Result<MediaFile> Upload(
            Guid id,
            Guid? ownerUserId,
            MediaType mediaType,
            string contentType,
            string originalFileName,
            string storageProvider,
            string storageKey,
            long sizeBytes,
            int? width,
            int? height,
            int? durationSeconds,
            MediaDataCategory dataCategory,
            TimeSpan orphanTimeout,
            DateTimeOffset utcNow)
        {
            if (dataCategory == MediaDataCategory.Personal && ownerUserId is null)
            {
                return MediaErrors.PersonalRequiresOwner;
            }

            var media = new MediaFile(
                id,
                ownerUserId,
                mediaType,
                contentType,
                originalFileName,
                storageProvider,
                storageKey,
                sizeBytes,
                width,
                height,
                durationSeconds,
                dataCategory,
                orphanTimeout,
                utcNow);

            media.RaiseDomainEvent(new MediaUploadedEvent(
                media.Id,
                media.OwnerUserId,
                media.DataCategory,
                media.ContentType,
                media.SizeBytes));

            return media;
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Переводит файл в статус <see cref="MediaStatus.Processing"/>. Допустим только
        /// переход <see cref="MediaStatus.Uploaded"/> → <see cref="MediaStatus.Processing"/>.
        /// </summary>
        /// <remarks>
        /// На Этапе 2 фактически не используется — обработка миниатюр синхронна, фабричный
        /// статус сразу переходит из <see cref="MediaStatus.Uploaded"/> в
        /// <see cref="MediaStatus.Ready"/>. Метод подготовлен для асинхронного pipeline
        /// UC-MED-213 (Этап 8+).
        /// </remarks>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="MediaErrors.InvalidStatusTransition"/>, если текущий статус
        /// — не <see cref="MediaStatus.Uploaded"/>.
        /// </returns>
        public Result MarkAsProcessing(DateTimeOffset utcNow)
        {
            if (Status != MediaStatus.Uploaded)
            {
                return MediaErrors.InvalidStatusTransition;
            }

            Status = MediaStatus.Processing;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        /// <summary>
        /// Переводит файл в статус <see cref="MediaStatus.Ready"/>. Допустимые исходные
        /// статусы — <see cref="MediaStatus.Uploaded"/> и <see cref="MediaStatus.Processing"/>.
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="MediaErrors.InvalidStatusTransition"/>.
        /// </returns>
        public Result MarkAsReady(DateTimeOffset utcNow)
        {
            if (Status is not (MediaStatus.Uploaded or MediaStatus.Processing))
            {
                return MediaErrors.InvalidStatusTransition;
            }

            Status = MediaStatus.Ready;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        /// <summary>
        /// Переводит файл в статус <see cref="MediaStatus.Failed"/>. Допустимые исходные
        /// статусы — все, кроме <see cref="MediaStatus.Deleted"/> и
        /// <see cref="MediaStatus.Failed"/> (повторный перевод запрещён).
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="Result.Failure(Error)"/>
        /// с <see cref="MediaErrors.InvalidStatusTransition"/>.
        /// </returns>
        public Result MarkAsFailed(DateTimeOffset utcNow)
        {
            if (Status is MediaStatus.Deleted or MediaStatus.Failed)
            {
                return MediaErrors.InvalidStatusTransition;
            }

            Status = MediaStatus.Failed;
            UpdatedAt = utcNow;
            return Result.Success();
        }

        /// <summary>
        /// Привязывает файл к сущности-владельцу. Обнуляет <see cref="ExpiresAt"/>,
        /// фиксирует <see cref="AttachedAt"/>. Поднимает <see cref="MediaAttachedEvent"/>.
        /// </summary>
        /// <remarks>
        /// Проверка владения (<c>actorUserId == OwnerUserId</c> либо роль Admin для системных
        /// файлов) выполняется на уровне Application в <c>IMediaService.AttachToEntityAsync</c>
        /// до вызова Domain-метода. Domain проверяет только инварианты собственного состояния:
        /// статус, отсутствие уже существующей привязки, известность типа сущности.
        /// </remarks>
        /// <param name="entityType">Тип сущности-владельца. Должен быть из <see cref="MediaEntityTypes.KNOWN_TYPES"/>.</param>
        /// <param name="entityId">Идентификатор сущности в её собственном домене.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> либо <see cref="Result.Failure(Error)"/> с одной из:
        /// <see cref="MediaErrors.NotReady"/>, <see cref="MediaErrors.AlreadyAttached"/>,
        /// <see cref="MediaErrors.UnknownEntityType"/>.
        /// </returns>
        public Result AttachToEntity(string entityType, Guid entityId, DateTimeOffset utcNow)
        {
            if (Status != MediaStatus.Ready)
            {
                return MediaErrors.NotReady;
            }

            if (EntityType is not null)
            {
                return MediaErrors.AlreadyAttached;
            }

            if (!MediaEntityTypes.KNOWN_TYPES.Contains(entityType))
            {
                return MediaErrors.UnknownEntityType;
            }

            EntityType = entityType;
            EntityId = entityId;
            AttachedAt = utcNow;
            ExpiresAt = null;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new MediaAttachedEvent(Id, entityType, entityId));
            return Result.Success();
        }

        /// <summary>
        /// Отвязывает файл от сущности. Обнуляет <see cref="EntityType"/>,
        /// <see cref="EntityId"/>, <see cref="AttachedAt"/>; переустанавливает
        /// <see cref="ExpiresAt"/> в <paramref name="utcNow"/> + <paramref name="orphanTimeout"/>.
        /// Поднимает <see cref="MediaDetachedEvent"/>.
        /// </summary>
        /// <param name="orphanTimeout">Длительность жизни orphan-файла. Передаётся из конфигурации.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> либо <see cref="Result.Failure(Error)"/> с
        /// <see cref="MediaErrors.NotAttached"/>, если файл и так не привязан.
        /// </returns>
        public Result DetachFromEntity(TimeSpan orphanTimeout, DateTimeOffset utcNow)
        {
            if (EntityType is null || EntityId is null)
            {
                return MediaErrors.NotAttached;
            }

            var previousEntityType = EntityType;
            var previousEntityId = EntityId.Value;

            EntityType = null;
            EntityId = null;
            AttachedAt = null;
            ExpiresAt = utcNow + orphanTimeout;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new MediaDetachedEvent(Id, previousEntityType, previousEntityId));
            return Result.Success();
        }

        /// <summary>
        /// Soft-удаляет файл: переводит в статус <see cref="MediaStatus.Deleted"/>,
        /// фиксирует <see cref="DeletedAt"/>. Поднимает <see cref="MediaDeletedEvent"/>.
        /// </summary>
        /// <remarks>
        /// Физическое удаление файла из хранилища выполняется фоновой задачей UC-MED-211
        /// через 7 дней после <see cref="DeletedAt"/> (Этап 8+).
        /// Проверки владения и Admin-роли — на стороне Application (POL-003);
        /// Domain отвечает только за инварианты состояния.
        /// </remarks>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> либо <see cref="Result.Failure(Error)"/> с одной из:
        /// <see cref="MediaErrors.AlreadyDeleted"/>, <see cref="MediaErrors.StillAttached"/>.
        /// </returns>
        public Result SoftDelete(DateTimeOffset utcNow)
        {
            if (Status == MediaStatus.Deleted)
            {
                return MediaErrors.AlreadyDeleted;
            }

            if (EntityType is not null)
            {
                return MediaErrors.StillAttached;
            }

            Status = MediaStatus.Deleted;
            DeletedAt = utcNow;
            ExpiresAt = null;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new MediaDeletedEvent(Id));
            return Result.Success();
        }

        /// <summary>
        /// Добавляет миниатюру к файлу. Проверяет уникальность пары
        /// (<paramref name="size"/>, <paramref name="format"/>), положительность
        /// размеров и допустимость текущего статуса.
        /// </summary>
        /// <param name="size">Номинальный размер миниатюры.</param>
        /// <param name="format">Формат миниатюры.</param>
        /// <param name="storageKey">Путь миниатюры в хранилище.</param>
        /// <param name="width">Фактическая ширина в пикселях.</param>
        /// <param name="height">Фактическая высота в пикселях.</param>
        /// <param name="sizeBytes">Размер файла миниатюры в байтах.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> либо <see cref="Result.Failure(Error)"/> с одной из:
        /// <see cref="MediaErrors.ThumbnailRequiresReadySource"/>,
        /// <see cref="MediaErrors.InvalidThumbnailDimensions"/>,
        /// <see cref="MediaErrors.DuplicateThumbnail"/>.
        /// </returns>
        public Result AddThumbnail(
            ThumbnailSize size,
            ThumbnailFormat format,
            string storageKey,
            int width,
            int height,
            long sizeBytes,
            DateTimeOffset utcNow)
        {
            if (Status is MediaStatus.Deleted or MediaStatus.Failed)
            {
                return MediaErrors.ThumbnailRequiresReadySource;
            }

            if (width <= 0 || height <= 0)
            {
                return MediaErrors.InvalidThumbnailDimensions;
            }

            foreach (var existing in _thumbnails)
            {
                if (existing.Size == size && existing.Format == format)
                {
                    return MediaErrors.DuplicateThumbnail;
                }
            }

            _thumbnails.Add(MediaThumbnail.CreateForFile(
                Id,
                size,
                format,
                storageKey,
                width,
                height,
                sizeBytes,
                utcNow));

            UpdatedAt = utcNow;
            return Result.Success();
        }

        #endregion
    }
}
