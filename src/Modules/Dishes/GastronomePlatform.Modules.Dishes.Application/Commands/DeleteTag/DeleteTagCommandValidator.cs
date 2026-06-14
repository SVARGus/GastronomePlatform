using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeleteTag
{
    /// <summary>
    /// Валидатор команды <see cref="DeleteTagCommand"/>.
    /// </summary>
    public sealed class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeleteTagCommandValidator"/>.
        /// </summary>
        public DeleteTagCommandValidator()
        {
            RuleFor(x => x.TagId)
                .NotEmpty().WithMessage("Идентификатор тега обязателен.");
        }
    }
}
