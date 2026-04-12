using FluentValidation;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Login
{
    /// <summary>
    /// Валидатор команды входа в систему.
    /// </summary>
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Логин обязателен.")
                .MaximumLength(256).WithMessage("Логин не должен превышать 256 символов.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен.")
                .MaximumLength(100).WithMessage("Пароль не должен превышать 100 символов.");
        }
    }
}
