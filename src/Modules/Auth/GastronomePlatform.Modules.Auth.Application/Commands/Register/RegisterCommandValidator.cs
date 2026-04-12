using FluentValidation;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Register
{
    /// <summary>
    /// Валидатор команды регистрации пользователя.
    /// </summary>
    public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email обязателен.")
                .EmailAddress().WithMessage("Некорректный формат email.")
                .MaximumLength(256).WithMessage("Email не должен превышать 256 символов.");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Никнейм обязателен.")
                .MinimumLength(3).WithMessage("Никнейм должен содержать минимум 3 символа.")
                .MaximumLength(100).WithMessage("Никнейм не должен превышать 100 символов.")
                .Matches(@"^[a-zA-Z0-9_]+$")
                .WithMessage("Никнейм может содержать только латинские буквы, цифры и символ '_'.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Пароль обязателен.")
                .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов.")
                .MaximumLength(100).WithMessage("Пароль не должен превышать 100 символов.")
                .Matches(@"[A-Z]").WithMessage("Пароль должен содержать минимум одну заглавную букву.")
                .Matches(@"[a-z]").WithMessage("Пароль должен содержать минимум одну строчную букву.")
                .Matches(@"[0-9]").WithMessage("Пароль должен содержать минимум одну цифру.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Пароль должен содержать минимум один спецсимвол.");

            RuleFor(x => x.Phone)
                .MaximumLength(50).WithMessage("Телефон не должен превышать 50 символов.")
                .When(x => x.Phone is not null);
        }
    }
}
