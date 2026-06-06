namespace GastronomePlatform.Modules.Media.Application.Queries.GetThumbnail
{
    /// <summary>
    /// Результат запроса <see cref="GetThumbnailQuery"/> (UC-MED-003).
    /// Содержит открытый поток — контроллер обязан закрыть его после записи в ответ.
    /// </summary>
    /// <param name="Content">Поток содержимого миниатюры.</param>
    /// <param name="ContentType">MIME-тип миниатюры.</param>
    public sealed record GetThumbnailResult(Stream Content, string ContentType);
}
