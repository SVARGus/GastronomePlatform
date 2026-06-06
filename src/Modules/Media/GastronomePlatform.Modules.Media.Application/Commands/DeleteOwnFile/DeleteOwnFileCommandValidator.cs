using FluentValidation;

namespace GastronomePlatform.Modules.Media.Application.Commands.DeleteOwnFile
{
    /// <summary>
    /// Валидатор команды <see cref="DeleteOwnFileCommand"/> (UC-MED-005).
    /// </summary>
    public sealed class DeleteOwnFileCommandValidator : AbstractValidator<DeleteOwnFileCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteOwnFileCommandValidator"/>.
        /// </summary>
        public DeleteOwnFileCommandValidator()
        {
            RuleFor(x => x.MediaId)
                .NotEqual(Guid.Empty).WithMessage("Идентификатор медиафайла обязателен.");
        }
    }
}
