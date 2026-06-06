using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Media.Application.Commands.UploadSystemFile
{
    /// <summary>
    /// Команда загрузки системного файла администратором (UC-MED-101).
    /// Создаёт <see cref="GastronomePlatform.Modules.Media.Domain.Entities.MediaFile"/>
    /// без владельца (<c>OwnerUserId = NULL</c>) в категории <c>Public</c>.
    /// Разрешены JPEG, PNG и SVG; SVG обязательно санируется перед сохранением.
    /// </summary>
    /// <param name="FileName">Оригинальное имя файла (напр. <c>borsch-icon.svg</c>).</param>
    /// <param name="ContentType">MIME-тип: <c>image/jpeg</c>, <c>image/png</c> или <c>image/svg+xml</c>.</param>
    /// <param name="FileContent">Байтовое содержимое файла.</param>
    /// <param name="IntendedEntityType">
    /// Тип сущности-цели: <c>CategoryIcon</c> или <c>IngredientImage</c>.
    /// Определяет путь в хранилище и будущую семантику привязки.
    /// </param>
    public sealed record UploadSystemFileCommand(
        string FileName,
        string ContentType,
        byte[] FileContent,
        string IntendedEntityType) : ICommand<UploadSystemFileResult>;
}
