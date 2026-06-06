using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetFile
{
    /// <summary>
    /// Запрос для получения содержимого медиафайла (UC-MED-002).
    /// Возвращает поток для стриминга контроллером.
    /// </summary>
    /// <param name="MediaId">Идентификатор медиафайла.</param>
    public sealed record GetFileQuery(Guid MediaId) : IQuery<GetFileResult>;
}
