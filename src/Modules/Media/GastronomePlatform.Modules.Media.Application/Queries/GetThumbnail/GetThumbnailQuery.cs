using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetThumbnail
{
    /// <summary>
    /// Запрос для получения миниатюры медиафайла (UC-MED-003).
    /// На Этапе 2 поддерживается только <see cref="ThumbnailSize.Medium"/> / <see cref="ThumbnailFormat.Jpeg"/>.
    /// </summary>
    /// <param name="MediaId">Идентификатор медиафайла.</param>
    /// <param name="Size">Размер запрашиваемой миниатюры.</param>
    /// <param name="Format">Формат запрашиваемой миниатюры.</param>
    public sealed record GetThumbnailQuery(
        Guid MediaId,
        ThumbnailSize Size,
        ThumbnailFormat Format) : IQuery<GetThumbnailResult>;
}
