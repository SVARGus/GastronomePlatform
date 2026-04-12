using FluentValidation;

namespace GastronomePlatform.Modules.Users.Application.Commands.SetVisibility
{
    /// <summary>
    /// Валидатор команды изменения видимости профиля.
    /// </summary>
    public sealed class SetVisibilityCommandValidator : AbstractValidator<SetVisibilityCommand>
    {
        public SetVisibilityCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");
        }
    }
}
