using FluentValidation;

namespace GastronomePlatform.Modules.Auth.Application.Commands.Logout
{
    /// <summary>
    /// Валидатор команды выхода из системы.
    /// </summary>
    public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token обязателен.");
        }
    }
}
