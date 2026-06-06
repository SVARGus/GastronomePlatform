namespace GastronomePlatform.Modules.Media.Application.Commands.UploadSystemFile
{
    /// <summary>
    /// Результат команды <see cref="UploadSystemFileCommand"/> (UC-MED-101).
    /// </summary>
    /// <param name="MediaId">Идентификатор созданного медиафайла.</param>
    /// <param name="Width">Ширина изображения в пикселях. <see langword="null"/> для SVG.</param>
    /// <param name="Height">Высота изображения в пикселях. <see langword="null"/> для SVG.</param>
    /// <param name="SizeBytes">Фактический размер сохранённого файла в байтах.</param>
    public sealed record UploadSystemFileResult(Guid MediaId, int? Width, int? Height, long SizeBytes);
}
