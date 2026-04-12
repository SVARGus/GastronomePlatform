using FluentValidation;

namespace GastronomePlatform.Modules.Auth.Application.Commands.RefreshAccessToken
{
    /// <summary>
    /// Валидатор команды обновления токенов.
    /// </summary>
    public sealed class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
    {
        public RefreshAccessTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token обязателен.");
        }
    }
}
