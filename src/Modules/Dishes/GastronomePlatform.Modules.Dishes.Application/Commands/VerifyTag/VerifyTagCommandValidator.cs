using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.VerifyTag
{
    /// <summary>
    /// Валидатор команды <see cref="VerifyTagCommand"/>.
    /// </summary>
    public sealed class VerifyTagCommandValidator : AbstractValidator<VerifyTagCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="VerifyTagCommandValidator"/>.
        /// </summary>
        public VerifyTagCommandValidator()
        {
            RuleFor(x => x.TagId)
                .NotEmpty().WithMessage("Идентификатор тега обязателен.");
        }
    }
}
