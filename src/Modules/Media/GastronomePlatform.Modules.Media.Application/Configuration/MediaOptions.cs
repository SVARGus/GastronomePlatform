namespace GastronomePlatform.Modules.Media.Application.Configuration
{
    /// <summary>
    /// Типизированные параметры конфигурации модуля Media.
    /// Привязываются к секции <c>Media</c> в <c>appsettings.json</c>.
    /// </summary>
    public sealed class MediaOptions
    {
        /// <summary>Имя секции в appsettings.json.</summary>
        public const string SECTION_NAME = "Media";

        /// <summary>Настройки хранилища файлов.</summary>
        public StorageOptions Storage { get; init; } = new();

        /// <summary>Ограничения для пользовательского upload (UC-MED-001).</summary>
        public UserUploadOptions UserUpload { get; init; } = new();

        /// <summary>Ограничения для системного upload (UC-MED-101, только Admin).</summary>
        public SystemUploadOptions SystemUpload { get; init; } = new();

        /// <summary>Настройки генерации миниатюр.</summary>
        public ThumbnailOptions Thumbnails { get; init; } = new();

        /// <summary>Настройки времени жизни файлов-сирот.</summary>
        public OrphanOptions Orphan { get; init; } = new();
    }

    /// <summary>
    /// Настройки хранилища файлов.
    /// </summary>
    public sealed class StorageOptions
    {
        /// <summary>
        /// Имя провайдера: <c>Local</c> (Этап 2) или <c>S3</c> (Этап 8+).
        /// </summary>
        public string Provider { get; init; } = "Local";

        /// <summary>
        /// Базовый путь локального хранилища.
        /// В Docker — volume mount <c>media-data:/data/media</c>.
        /// </summary>
        public string LocalBasePath { get; init; } = "/data/media";
    }

    /// <summary>
    /// Ограничения для пользовательского upload (UC-MED-001).
    /// </summary>
    public sealed class UserUploadOptions
    {
        /// <summary>Максимальный размер файла в байтах. По умолчанию 10 МБ.</summary>
        public long MaxSizeBytes { get; init; } = 10_485_760L;

        /// <summary>Минимальный размер файла в байтах. По умолчанию 5 КБ.</summary>
        public long MinSizeBytes { get; init; } = 5_120L;

        /// <summary>Разрешённые MIME-типы. На Этапе 2: JPEG и PNG.</summary>
        public string[] AllowedMimeTypes { get; init; } = ["image/jpeg", "image/png"];

        /// <summary>Максимальная сторона изображения в пикселях.</summary>
        public int MaxImageDimension { get; init; } = 4096;

        /// <summary>Минимальная сторона изображения в пикселях.</summary>
        public int MinImageDimension { get; init; } = 100;
    }

    /// <summary>
    /// Ограничения для системного upload (UC-MED-101, только Admin).
    /// </summary>
    public sealed class SystemUploadOptions
    {
        /// <summary>Максимальный размер системного файла в байтах. По умолчанию 2 МБ.</summary>
        public long MaxSizeBytes { get; init; } = 2_097_152L;

        /// <summary>Разрешённые MIME-типы: JPEG, PNG, SVG.</summary>
        public string[] AllowedMimeTypes { get; init; } =
            ["image/jpeg", "image/png", "image/svg+xml"];

        /// <summary>Максимальная сторона изображения в пикселях (только для JPEG/PNG).</summary>
        public int MaxImageDimension { get; init; } = 1024;
    }

    /// <summary>
    /// Настройки генерации миниатюр.
    /// </summary>
    public sealed class ThumbnailOptions
    {
        /// <summary>
        /// Целевая сторона Medium-миниатюры в пикселях (400×400 с сохранением aspect ratio).
        /// </summary>
        public int MediumSize { get; init; } = 400;

        /// <summary>Качество JPEG-миниатюры (0–100).</summary>
        public int JpegQuality { get; init; } = 85;
    }

    /// <summary>
    /// Настройки времени жизни файлов-сирот (orphan).
    /// </summary>
    public sealed class OrphanOptions
    {
        /// <summary>
        /// Через сколько часов orphan-файл истекает.
        /// Значение используется при расчёте <c>MediaFile.ExpiresAt = utcNow + orphanTimeout</c>.
        /// </summary>
        public int ExpirationHours { get; init; } = 24;
    }
}
