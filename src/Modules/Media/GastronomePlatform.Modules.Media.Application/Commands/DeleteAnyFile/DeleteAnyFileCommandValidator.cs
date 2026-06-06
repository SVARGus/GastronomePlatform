using FluentValidation;

namespace GastronomePlatform.Modules.Media.Application.Commands.DeleteAnyFile
{
    /// <summary>
    /// Валидатор команды <see cref="DeleteAnyFileCommand"/> (UC-MED-102).
    /// </summary>
    public sealed class DeleteAnyFileCommandValidator : AbstractValidator<DeleteAnyFileCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteAnyFileCommandValidator"/>.
        /// </summary>
        public DeleteAnyFileCommandValidator()
        {
            RuleFor(x => x.MediaId)
                .NotEqual(Guid.Empty).WithMessage("Идентификатор медиафайла обязателен.");
        }
    }
}
