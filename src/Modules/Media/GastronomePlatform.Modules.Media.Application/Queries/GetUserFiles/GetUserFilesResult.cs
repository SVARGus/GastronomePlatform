using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetUserFiles
{
    /// <summary>
    /// Результат запроса <see cref="GetUserFilesQuery"/> (UC-MED-103).
    /// </summary>
    /// <param name="Items">Файлы текущей страницы.</param>
    public sealed record GetUserFilesResult(IReadOnlyList<UserFileItemResult> Items);

    /// <summary>
    /// Краткие метаданные одного файла в списке (UC-MED-103).
    /// </summary>
    /// <param name="MediaId">Идентификатор медиафайла.</param>
    /// <param name="ContentType">MIME-тип файла.</param>
    /// <param name="SizeBytes">Размер файла в байтах.</param>
    /// <param name="Width">Ширина изображения в пикселях. <see langword="null"/> для SVG или видео.</param>
    /// <param name="Height">Высота изображения в пикселях. <see langword="null"/> для SVG или видео.</param>
    /// <param name="Status">Текущий статус жизненного цикла.</param>
    /// <param name="EntityType">Тип сущности-владельца. <see langword="null"/> для orphan-файлов.</param>
    /// <param name="EntityId">Идентификатор сущности-владельца. <see langword="null"/> для orphan-файлов.</param>
    /// <param name="CreatedAt">Момент создания файла.</param>
    /// <param name="AttachedAt">Момент привязки к сущности. <see langword="null"/> для orphan-файлов.</param>
    /// <param name="DeletedAt">Момент мягкого удаления. <see langword="null"/> для активных файлов.</param>
    public sealed record UserFileItemResult(
        Guid MediaId,
        string ContentType,
        long SizeBytes,
        int? Width,
        int? Height,
        MediaStatus Status,
        string? EntityType,
        Guid? EntityId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? AttachedAt,
        DateTimeOffset? DeletedAt);
}
