namespace GastronomePlatform.Modules.Media.Application.Commands.UploadFile
{
    /// <summary>
    /// Результат команды <see cref="UploadFileCommand"/> (UC-MED-001).
    /// </summary>
    /// <param name="MediaId">
    /// Идентификатор созданного медиафайла. Клиент использует его при привязке к сущности
    /// через <c>IMediaService.AttachToEntityAsync</c>.
    /// </param>
    /// <param name="Width">Ширина изображения в пикселях.</param>
    /// <param name="Height">Высота изображения в пикселях.</param>
    /// <param name="SizeBytes">Размер исходного файла в байтах.</param>
    public sealed record UploadFileResult(
        Guid MediaId,
        int? Width,
        int? Height,
        long SizeBytes);
}
