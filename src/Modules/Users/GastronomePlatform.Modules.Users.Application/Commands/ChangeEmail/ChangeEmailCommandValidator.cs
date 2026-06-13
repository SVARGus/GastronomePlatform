using FluentValidation;
using GastronomePlatform.Modules.Users.Domain.Entities;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangeEmail
{
    /// <summary>
    /// Валидатор команды изменения email.
    /// </summary>
    /// <remarks>
    /// Лимит длины — единый источник в <see cref="UserProfile"/>
    /// (зеркало <c>Auth.Domain.Constants.AuthLimits.MAX_EMAIL_LENGTH</c>).
    /// </remarks>
    public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
    {
        public ChangeEmailCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.NewEmail)
                .NotEmpty().WithMessage("Email обязателен.")
                .EmailAddress().WithMessage("Некорректный формат email.")
                .MaximumLength(UserProfile.MAX_EMAIL_LENGTH)
                    .WithMessage($"Email не должен превышать {UserProfile.MAX_EMAIL_LENGTH} символов.");
        }
    }
}
