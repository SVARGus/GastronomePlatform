using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Media.Domain.Errors
{
    /// <summary>
    /// Доменные ошибки модуля Media.
    /// </summary>
    public static class MediaErrors
    {
        #region Доступ и поиск

        /// <summary>Медиафайл не найден (или soft-deleted / failed).</summary>
        public static readonly Error NotFound =
            Error.NotFound("MEDIA.NOT_FOUND", "Медиафайл не найден.");

        /// <summary>Файл ещё не готов к использованию (статус Uploaded / Processing).</summary>
        public static readonly Error NotReady =
            Error.Validation("MEDIA.NOT_READY", "Медиафайл ещё не готов к использованию.");

        /// <summary>Personal-файл запрошен без аутентификации (POL-002).</summary>
        public static readonly Error Unauthorized =
            Error.Forbidden("MEDIA.UNAUTHORIZED",
                "Для доступа к этому файлу необходима авторизация.");

        #endregion

        #region Владение и модификация (POL-003)

        /// <summary>Не владелец и не Admin (POL-003).</summary>
        public static readonly Error ForbiddenNotOwner =
            Error.Forbidden("MEDIA.FORBIDDEN_NOT_OWNER",
                "У вас нет прав на изменение этого файла.");

        /// <summary>Системный файл — пользовательское удаление недоступно (POL-003).</summary>
        public static readonly Error ForbiddenSystemFile =
            Error.Forbidden("MEDIA.FORBIDDEN_SYSTEM_FILE",
                "Системные файлы нельзя удалить через пользовательский сценарий.");

        /// <summary>Файл всё ещё привязан к сущности — сначала открепить (POL-003).</summary>
        public static readonly Error StillAttached =
            Error.Conflict("MEDIA.STILL_ATTACHED",
                "Файл привязан к сущности. Сначала открепите его или удалите сущность-владельца.");

        /// <summary>Повторное soft-delete — файл уже в статусе Deleted.</summary>
        public static readonly Error AlreadyDeleted =
            Error.Conflict("MEDIA.ALREADY_DELETED", "Медиафайл уже удалён.");

        #endregion

        #region Загрузка и валидация

        /// <summary>MIME-тип не входит в whitelist (jpeg/png для пользовательского upload).</summary>
        public static readonly Error InvalidFileType =
            Error.Validation("MEDIA.INVALID_FILE_TYPE",
                "Тип файла не поддерживается.");

        /// <summary>Размер файла превышает допустимый лимит.</summary>
        public static readonly Error FileTooLarge =
            Error.Validation("MEDIA.FILE_TOO_LARGE",
                "Размер файла превышает допустимый.");

        /// <summary>Размеры изображения вне допустимого диапазона.</summary>
        public static readonly Error InvalidImageDimensions =
            Error.Validation("MEDIA.INVALID_DIMENSIONS",
                "Размер изображения не соответствует требованиям.");

        /// <summary>Сбой при сохранении файла в хранилище.</summary>
        public static readonly Error UploadFailed =
            Error.Failure("MEDIA.UPLOAD_FAILED",
                "Не удалось сохранить файл.");

        #endregion

        #region Инварианты MediaFile (Domain factory + lifecycle)

        /// <summary>Парность <c>EntityType</c> и <c>EntityId</c>: оба заполнены либо оба NULL.</summary>
        public static readonly Error EntityRefsMustMatchNullity =
            Error.Validation("MEDIA.ENTITY_REFS_MUST_MATCH_NULLITY",
                "Поля EntityType и EntityId должны быть либо оба заполнены, либо оба пусты.");

        /// <summary>Personal-файлы должны иметь владельца — системные Personal-файлы запрещены.</summary>
        public static readonly Error PersonalRequiresOwner =
            Error.Validation("MEDIA.PERSONAL_REQUIRES_OWNER",
                "Файл с категорией Personal обязан иметь владельца — системные Personal-файлы запрещены.");

        /// <summary>Тип сущности не входит в известный список <c>MediaEntityTypes.KNOWN_TYPES</c>.</summary>
        public static readonly Error UnknownEntityType =
            Error.Validation("MEDIA.UNKNOWN_ENTITY_TYPE",
                "Неизвестный тип сущности-владельца.");

        /// <summary>Попытка привязать файл, который уже привязан — предотвращает «угон».</summary>
        public static readonly Error AlreadyAttached =
            Error.Conflict("MEDIA.ALREADY_ATTACHED",
                "Медиафайл уже привязан к сущности.");

        /// <summary>Owner-проверка при <c>AttachToEntity</c> (используется внутренним контрактом IMediaService).</summary>
        public static readonly Error NotOwned =
            Error.Forbidden("MEDIA.NOT_OWNED",
                "У вас нет прав на использование этого файла.");

        /// <summary>Попытка отвязать файл, который не привязан.</summary>
        public static readonly Error NotAttached =
            Error.Conflict("MEDIA.NOT_ATTACHED",
                "Медиафайл не привязан к сущности.");

        /// <summary>Запрошенный переход статуса не разрешён графом состояний.</summary>
        public static readonly Error InvalidStatusTransition =
            Error.Conflict("MEDIA.INVALID_STATUS_TRANSITION",
                "Текущий статус файла не допускает запрошенного перехода.");

        /// <summary>Размеры миниатюры должны быть строго положительными.</summary>
        public static readonly Error InvalidThumbnailDimensions =
            Error.Validation("MEDIA.INVALID_THUMBNAIL_DIMENSIONS",
                "Размеры миниатюры должны быть строго положительными.");

        /// <summary>Дубликат миниатюры — пара (Size, Format) уже есть в агрегате.</summary>
        public static readonly Error DuplicateThumbnail =
            Error.Conflict("MEDIA.DUPLICATE_THUMBNAIL",
                "Миниатюра такого размера и формата уже существует для этого файла.");

        /// <summary>Миниатюру нельзя добавить к файлу в не-готовом статусе.</summary>
        public static readonly Error ThumbnailRequiresReadySource =
            Error.Conflict("MEDIA.THUMBNAIL_REQUIRES_READY_SOURCE",
                "Миниатюру можно добавить только к файлу в статусах Uploaded, Processing или Ready.");

        #endregion
    }
}
