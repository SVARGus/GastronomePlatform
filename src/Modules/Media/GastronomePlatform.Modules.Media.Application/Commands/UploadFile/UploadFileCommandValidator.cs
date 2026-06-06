using FluentValidation;
using GastronomePlatform.Modules.Media.Domain.Constants;

namespace GastronomePlatform.Modules.Media.Application.Commands.UploadFile
{
    /// <summary>
    /// Валидатор команды <see cref="UploadFileCommand"/> (UC-MED-001).
    /// </summary>
    /// <remarks>
    /// Проверяет только то, что не требует декодирования изображения: MIME-whitelist,
    /// размер файла, имя файла и тип целевой сущности. Проверка magic bytes и размеров
    /// изображения (width/height) выполняется в <see cref="UploadFileCommandHandler"/>.
    /// </remarks>
    public sealed class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
    {
        private const long MAX_FILE_SIZE = 10_485_760L;
        private const long MIN_FILE_SIZE = 5_120L;

        private static readonly string[] _allowedMimeTypes = ["image/jpeg", "image/png"];

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UploadFileCommandValidator"/>.
        /// </summary>
        public UploadFileCommandValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("Имя файла обязательно.")
                .MaximumLength(255).WithMessage("Имя файла не должно превышать 255 символов.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("MIME-тип файла обязателен.")
                .Must(ct => _allowedMimeTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Тип файла не поддерживается. Разрешены: image/jpeg, image/png.");

            RuleFor(x => x.FileContent)
                .NotEmpty().WithMessage("Содержимое файла обязательно.")
                .Must(c => c.LongLength >= MIN_FILE_SIZE)
                .WithMessage($"Размер файла должен быть не менее {MIN_FILE_SIZE / 1024} КБ.")
                .Must(c => c.LongLength <= MAX_FILE_SIZE)
                .WithMessage($"Размер файла не должен превышать {MAX_FILE_SIZE / 1_048_576} МБ.");

            RuleFor(x => x.IntendedEntityType)
                .NotEmpty().WithMessage("Тип целевой сущности обязателен.")
                .Must(t => MediaEntityTypes.KNOWN_TYPES.Contains(t))
                .WithMessage("Указан неизвестный тип целевой сущности.");
        }
    }
}
