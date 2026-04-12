using FluentValidation;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdateLocation
{
    /// <summary>
    /// Валидатор команды обновления местоположения.
    /// </summary>
    public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
    {
        public UpdateLocationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.Country)
                .MaximumLength(100).WithMessage("Страна не должна превышать 100 символов.")
                .When(x => x.Country is not null);

            RuleFor(x => x.Region)
                .MaximumLength(100).WithMessage("Регион не должен превышать 100 символов.")
                .When(x => x.Region is not null);

            RuleFor(x => x.City)
                .MaximumLength(100).WithMessage("Город не должен превышать 100 символов.")
                .When(x => x.City is not null);
        }
    }
}
