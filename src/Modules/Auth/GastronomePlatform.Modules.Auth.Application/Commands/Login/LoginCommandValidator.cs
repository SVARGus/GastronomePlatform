using FluentValidation;
using GastronomePlatform.Modules.Auth.Domain.Constants;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Login
{
    /// <summary>
    /// Валидатор команды входа в систему.
    /// </summary>
    /// <remarks>
    /// Лимиты длины полей — единый источник в <see cref="AuthLimits"/>.
    /// </remarks>
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Login)
                .NotEmpty().WithMessage("Логин обязателен.")
                .MaximumLength(AuthLimits.MAX_LOGIN_LENGTH)
                    .WithMessage($"Логин не должен превышать {AuthLimits.MAX_LOGIN_LENGTH} символов.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен.")
                .MaximumLength(AuthLimits.MAX_PASSWORD_LENGTH)
                    .WithMessage($"Пароль не должен превышать {AuthLimits.MAX_PASSWORD_LENGTH} символов.");
        }
    }
}
