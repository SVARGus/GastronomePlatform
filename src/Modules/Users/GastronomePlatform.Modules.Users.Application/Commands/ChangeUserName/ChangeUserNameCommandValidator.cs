using FluentValidation;
using GastronomePlatform.Modules.Users.Domain.Entities;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangeUserName
{
    /// <summary>
    /// Валидатор команды изменения никнейма.
    /// </summary>
    /// <remarks>
    /// Лимиты длины — единый источник в <see cref="UserProfile"/>
    /// (зеркало <c>Auth.Domain.Constants.AuthLimits.MIN/MAX_USER_NAME_LENGTH</c>).
    /// </remarks>
    public sealed class ChangeUserNameCommandValidator : AbstractValidator<ChangeUserNameCommand>
    {
        public ChangeUserNameCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.NewUserName)
                .NotEmpty().WithMessage("Никнейм обязателен.")
                .MinimumLength(UserProfile.MIN_USER_NAME_LENGTH)
                    .WithMessage($"Никнейм должен содержать минимум {UserProfile.MIN_USER_NAME_LENGTH} символа.")
                .MaximumLength(UserProfile.MAX_USER_NAME_LENGTH)
                    .WithMessage($"Никнейм не должен превышать {UserProfile.MAX_USER_NAME_LENGTH} символов.")
                .Matches(@"^[a-zA-Z0-9_]+$")
                    .WithMessage("Никнейм может содержать только латинские буквы, цифры и '_'.");
        }
    }
}
