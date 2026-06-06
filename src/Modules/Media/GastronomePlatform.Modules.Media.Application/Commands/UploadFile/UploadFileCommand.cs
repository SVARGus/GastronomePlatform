using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Media.Application.Commands.UploadFile
{
    /// <summary>
    /// Команда загрузки файла пользователем (UC-MED-001).
    /// </summary>
    /// <param name="FileName">Исходное имя файла, как его прислал клиент.</param>
    /// <param name="ContentType">MIME-тип файла (<c>image/jpeg</c> или <c>image/png</c>).</param>
    /// <param name="FileContent">Содержимое файла. Контроллер копирует <c>IFormFile</c> в байтовый массив.</param>
    /// <param name="IntendedEntityType">
    /// Тип сущности, к которой будет привязан файл (константа из <c>MediaEntityTypes</c>).
    /// Обязателен даже для orphan-файлов: определяет <c>DataCategory</c> и структуру пути в хранилище.
    /// </param>
    public sealed record UploadFileCommand(
        string FileName,
        string ContentType,
        byte[] FileContent,
        string IntendedEntityType) : ICommand<UploadFileResult>;
}
