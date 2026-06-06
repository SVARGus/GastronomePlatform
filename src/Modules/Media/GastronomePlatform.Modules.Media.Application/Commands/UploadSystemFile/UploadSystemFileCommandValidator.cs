using FluentValidation;
using GastronomePlatform.Modules.Media.Domain.Constants;

namespace GastronomePlatform.Modules.Media.Application.Commands.UploadSystemFile
{
    /// <summary>
    /// Валидатор команды <see cref="UploadSystemFileCommand"/> (UC-MED-101).
    /// </summary>
    /// <remarks>
    /// Статические проверки формата и типа сущности. Размер файла и ограничения
    /// на пиксели проверяются в <see cref="UploadSystemFileCommandHandler"/>
    /// с учётом конфигурации (<c>Media:SystemUpload</c>).
    /// </remarks>
    public sealed class UploadSystemFileCommandValidator : AbstractValidator<UploadSystemFileCommand>
    {
        private static readonly string[] _allowedMimeTypes =
            ["image/jpeg", "image/png", "image/svg+xml"];

        private static readonly HashSet<string> _systemEntityTypes =
            new(StringComparer.Ordinal)
            {
                MediaEntityTypes.CATEGORY_ICON,
                MediaEntityTypes.INGREDIENT_IMAGE
            };

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UploadSystemFileCommandValidator"/>.
        /// </summary>
        public UploadSystemFileCommandValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("Имя файла обязательно.")
                .MaximumLength(255).WithMessage("Имя файла не должно превышать 255 символов.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("Content-Type обязателен.")
                .Must(ct => _allowedMimeTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Допустимые типы файлов: image/jpeg, image/png, image/svg+xml.");

            RuleFor(x => x.FileContent)
                .NotEmpty().WithMessage("Содержимое файла обязательно.");

            RuleFor(x => x.IntendedEntityType)
                .NotEmpty().WithMessage("Тип целевой сущности обязателен.")
                .Must(et => _systemEntityTypes.Contains(et))
                .WithMessage(
                    $"Для системного upload допустимы только: " +
                    $"{MediaEntityTypes.CATEGORY_ICON}, {MediaEntityTypes.INGREDIENT_IMAGE}.");
        }
    }
}
