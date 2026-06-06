using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetFileMetadata
{
    /// <summary>
    /// Результат запроса <see cref="GetFileMetadataQuery"/> (UC-MED-004).
    /// </summary>
    /// <param name="Id">Идентификатор медиафайла.</param>
    /// <param name="ContentType">MIME-тип файла.</param>
    /// <param name="SizeBytes">Размер оригинального файла в байтах.</param>
    /// <param name="Width">Ширина изображения в пикселях. <see langword="null"/> для видео.</param>
    /// <param name="Height">Высота изображения в пикселях. <see langword="null"/> для видео.</param>
    /// <param name="Status">Текущий статус жизненного цикла.</param>
    /// <param name="EntityType">Тип привязанной сущности. <see langword="null"/> если файл orphan.</param>
    /// <param name="EntityId">Идентификатор привязанной сущности. <see langword="null"/> если файл orphan.</param>
    /// <param name="CreatedAt">Момент загрузки файла.</param>
    /// <param name="Thumbnails">Список доступных миниатюр.</param>
    public sealed record FileMetadataResult(
        Guid Id,
        string ContentType,
        long SizeBytes,
        int? Width,
        int? Height,
        MediaStatus Status,
        string? EntityType,
        Guid? EntityId,
        DateTimeOffset CreatedAt,
        IReadOnlyList<ThumbnailInfoResult> Thumbnails);

    /// <summary>
    /// Краткие метаданные миниатюры в составе <see cref="FileMetadataResult"/>.
    /// </summary>
    /// <param name="Size">Номинальный размер.</param>
    /// <param name="Format">Формат.</param>
    /// <param name="Width">Фактическая ширина в пикселях.</param>
    /// <param name="Height">Фактическая высота в пикселях.</param>
    /// <param name="SizeBytes">Размер файла миниатюры в байтах.</param>
    public sealed record ThumbnailInfoResult(
        ThumbnailSize Size,
        ThumbnailFormat Format,
        int Width,
        int Height,
        long SizeBytes);
}
