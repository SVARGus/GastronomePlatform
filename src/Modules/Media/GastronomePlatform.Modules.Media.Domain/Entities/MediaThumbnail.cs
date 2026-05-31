using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Domain.Entities
{
    /// <summary>
    /// Миниатюра / оптимизированная копия медиафайла. Часть агрегата <see cref="MediaFile"/>:
    /// принадлежит ему, но физически хранится в отдельной таблице.
    /// </summary>
    /// <remarks>
    /// Все методы изменения состояния — <see langword="internal"/>. Внешний код управляет
    /// миниатюрами только через корень агрегата <see cref="MediaFile"/> (метод
    /// <see cref="MediaFile.AddThumbnail"/>). Прямое создание из репозиториев или хендлеров
    /// запрещено.
    /// </remarks>
    public sealed class MediaThumbnail : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Идентификатор файла-владельца. FK на <c>media.MediaFiles</c> с <c>ON DELETE CASCADE</c>.
        /// </summary>
        public Guid MediaFileId { get; private set; }

        /// <summary>
        /// Номинальный размер миниатюры. На Этапе 2 — только <see cref="Enums.ThumbnailSize.Medium"/>.
        /// </summary>
        public ThumbnailSize Size { get; private set; }

        /// <summary>
        /// Формат миниатюры. На Этапе 2 — только <see cref="Enums.ThumbnailFormat.Jpeg"/>.
        /// </summary>
        public ThumbnailFormat Format { get; private set; }

        /// <summary>
        /// Путь миниатюры в хранилище (значение от <c>IStorageKeyGenerator</c>).
        /// Иммутабелен после создания.
        /// </summary>
        public string StorageKey { get; private set; } = string.Empty;

        /// <summary>
        /// Фактическая ширина миниатюры в пикселях. Может отличаться от номинального размера
        /// при сохранении aspect ratio.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Фактическая высота миниатюры в пикселях.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Размер файла миниатюры в байтах.
        /// </summary>
        public long SizeBytes { get; private set; }

        /// <summary>
        /// Момент создания миниатюры. Иммутабелен.
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private MediaThumbnail() : base() { }

        /// <summary>
        /// Приватный конструктор. Используется только из <see cref="CreateForFile"/>.
        /// </summary>
        /// <param name="mediaFileId">Идентификатор файла-владельца.</param>
        /// <param name="size">Номинальный размер.</param>
        /// <param name="format">Формат миниатюры.</param>
        /// <param name="storageKey">Путь в хранилище.</param>
        /// <param name="width">Фактическая ширина в пикселях.</param>
        /// <param name="height">Фактическая высота в пикселях.</param>
        /// <param name="sizeBytes">Размер файла миниатюры в байтах.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        private MediaThumbnail(
            Guid mediaFileId,
            ThumbnailSize size,
            ThumbnailFormat format,
            string storageKey,
            int width,
            int height,
            long sizeBytes,
            DateTimeOffset utcNow)
            : base(Guid.NewGuid())
        {
            MediaFileId = mediaFileId;
            Size = size;
            Format = format;
            StorageKey = storageKey;
            Width = width;
            Height = height;
            SizeBytes = sizeBytes;
            CreatedAt = utcNow;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт миниатюру в составе агрегата. Вызывается только из
        /// <see cref="MediaFile.AddThumbnail"/>; внешний код не имеет к фабрике доступа.
        /// Инварианты, относящиеся к комбинации полей (положительные размеры, непустой
        /// <paramref name="storageKey"/>), проверяются на стороне <see cref="MediaFile.AddThumbnail"/>
        /// перед вызовом.
        /// </summary>
        /// <param name="mediaFileId">Идентификатор файла-владельца.</param>
        /// <param name="size">Номинальный размер.</param>
        /// <param name="format">Формат миниатюры.</param>
        /// <param name="storageKey">Путь в хранилище.</param>
        /// <param name="width">Фактическая ширина в пикселях.</param>
        /// <param name="height">Фактическая высота в пикселях.</param>
        /// <param name="sizeBytes">Размер файла миниатюры в байтах.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>Новая миниатюра, привязанная к указанному файлу.</returns>
        internal static MediaThumbnail CreateForFile(
            Guid mediaFileId,
            ThumbnailSize size,
            ThumbnailFormat format,
            string storageKey,
            int width,
            int height,
            long sizeBytes,
            DateTimeOffset utcNow)
        {
            return new MediaThumbnail(
                mediaFileId,
                size,
                format,
                storageKey,
                width,
                height,
                sizeBytes,
                utcNow);
        }

        #endregion
    }
}
