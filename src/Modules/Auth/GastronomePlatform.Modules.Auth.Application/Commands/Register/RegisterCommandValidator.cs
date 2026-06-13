using FluentValidation;
using GastronomePlatform.Modules.Auth.Domain.Constants;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Register
{
    /// <summary>
    /// Валидатор команды регистрации пользователя.
    /// </summary>
    /// <remarks>
    /// Лимиты длины полей — единый источник в <see cref="AuthLimits"/>.
    /// </remarks>
    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен.")
                .EmailAddress().WithMessage("Некорректный формат email.")
                .MaximumLength(AuthLimits.MAX_EMAIL_LENGTH)
                    .WithMessage($"Email не должен превышать {AuthLimits.MAX_EMAIL_LENGTH} символов.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Никнейм обязателен.")
                .MinimumLength(AuthLimits.MIN_USER_NAME_LENGTH)
                    .WithMessage($"Никнейм должен содержать минимум {AuthLimits.MIN_USER_NAME_LENGTH} символа.")
                .MaximumLength(AuthLimits.MAX_USER_NAME_LENGTH)
                    .WithMessage($"Никнейм не должен превышать {AuthLimits.MAX_USER_NAME_LENGTH} символов.")
                .Matches(@"^[a-zA-Z0-9_]+$")
                    .WithMessage("Никнейм может содержать только латинские буквы, цифры и символ '_'.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен.")
                .MinimumLength(AuthLimits.MIN_PASSWORD_LENGTH)
                    .WithMessage($"Пароль должен содержать минимум {AuthLimits.MIN_PASSWORD_LENGTH} символов.")
                .MaximumLength(AuthLimits.MAX_PASSWORD_LENGTH)
                    .WithMessage($"Пароль не должен превышать {AuthLimits.MAX_PASSWORD_LENGTH} символов.")
                .Matches(@"[A-Z]").WithMessage("Пароль должен содержать минимум одну заглавную букву.")
                .Matches(@"[a-z]").WithMessage("Пароль должен содержать минимум одну строчную букву.")
                .Matches(@"[0-9]").WithMessage("Пароль должен содержать минимум одну цифру.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Пароль должен содержать минимум один спецсимвол.");

            RuleFor(x => x.Phone)
                .MaximumLength(AuthLimits.MAX_PHONE_LENGTH)
                    .WithMessage($"Телефон не должен превышать {AuthLimits.MAX_PHONE_LENGTH} символов.")
                .When(x => x.Phone is not null);
        }
    }
}
