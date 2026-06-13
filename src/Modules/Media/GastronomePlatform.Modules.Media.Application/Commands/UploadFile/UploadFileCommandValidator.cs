using FluentValidation;
using GastronomePlatform.Modules.Media.Application.Configuration;
using GastronomePlatform.Modules.Media.Domain.Constants;
using GastronomePlatform.Modules.Media.Domain.Entities;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Modules.Media.Application.Commands.UploadFile
{
    /// <summary>
    /// Валидатор команды <see cref="UploadFileCommand"/> (UC-MED-001).
    /// </summary>
    /// <remarks>
    /// Проверяет только то, что не требует декодирования изображения: MIME-whitelist,
    /// размер файла, имя файла и тип целевой сущности. Проверка magic bytes и размеров
    /// изображения (width/height) выполняется в <see cref="UploadFileCommandHandler"/>.
    /// <para>
    /// Лимит длины имени файла — единый источник в <see cref="MediaFile"/>.
    /// Минимальный и максимальный размер файла читаются из <see cref="MediaOptions.UserUpload"/>
    /// (значения настраиваются через <c>appsettings.json</c>).
    /// </para>
    /// </remarks>
    public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
    {
        private static readonly string[] _allowedMimeTypes = ["image/jpeg", "image/png"];

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UploadFileCommandValidator"/>.
        /// </summary>
        /// <param name="options">Конфигурация модуля Media.</param>
        public UploadFileCommandValidator(IOptions<MediaOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            UserUploadOptions userUpload = options.Value.UserUpload;

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("Имя файла обязательно.")
                .MaximumLength(MediaFile.MAX_FILE_NAME_LENGTH)
                    .WithMessage($"Имя файла не должно превышать {MediaFile.MAX_FILE_NAME_LENGTH} символов.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("MIME-тип файла обязателен.")
                .Must(ct => _allowedMimeTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
                    .WithMessage("Тип файла не поддерживается. Разрешены: image/jpeg, image/png.");

            RuleFor(x => x.FileContent)
                .NotEmpty().WithMessage("Содержимое файла обязательно.")
                .Must(c => c.LongLength >= userUpload.MinSizeBytes)
                    .WithMessage($"Размер файла должен быть не менее {userUpload.MinSizeBytes / 1024} КБ.")
                .Must(c => c.LongLength <= userUpload.MaxSizeBytes)
                    .WithMessage($"Размер файла не должен превышать {userUpload.MaxSizeBytes / 1_048_576} МБ.");

            RuleFor(x => x.IntendedEntityType)
                .NotEmpty().WithMessage("Тип целевой сущности обязателен.")
                .Must(t => MediaEntityTypes.KNOWN_TYPES.Contains(t))
                    .WithMessage("Указан неизвестный тип целевой сущности.");
        }
    }
}
