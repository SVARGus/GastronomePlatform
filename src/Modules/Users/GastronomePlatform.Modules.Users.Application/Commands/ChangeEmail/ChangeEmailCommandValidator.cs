using FluentValidation;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangeEmail
{
    /// <summary>
    /// Валидатор команды изменения email.
    /// </summary>
    public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
    {
        public ChangeEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.NewEmail)
                .NotEmpty().WithMessage("Email обязателен.")
                .EmailAddress().WithMessage("Некорректный формат email.")
                .MaximumLength(256).WithMessage("Email не должен превышать 256 символов.");
        }
    }
}
