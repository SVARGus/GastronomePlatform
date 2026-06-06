using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetFileMetadata
{
    /// <summary>
    /// Запрос метаданных медиафайла без его содержимого (UC-MED-004).
    /// </summary>
    /// <param name="MediaId">Идентификатор медиафайла.</param>
    public sealed record GetFileMetadataQuery(Guid MediaId) : IQuery<FileMetadataResult>;
}
