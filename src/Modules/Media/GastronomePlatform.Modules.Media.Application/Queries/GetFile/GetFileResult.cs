namespace GastronomePlatform.Modules.Media.Application.Queries.GetFile
{
    /// <summary>
    /// Результат запроса <see cref="GetFileQuery"/> (UC-MED-002).
    /// Содержит открытый поток — контроллер обязан закрыть его после записи в ответ.
    /// </summary>
    /// <param name="Content">Поток содержимого файла.</param>
    /// <param name="ContentType">MIME-тип файла.</param>
    /// <param name="OriginalFileName">Исходное имя файла (для заголовка <c>Content-Disposition</c>).</param>
    /// <param name="SizeBytes">Размер файла в байтах (для заголовка <c>Content-Length</c>).</param>
    public sealed record GetFileResult(
        Stream Content,
        string ContentType,
        string OriginalFileName,
        long SizeBytes);
}
