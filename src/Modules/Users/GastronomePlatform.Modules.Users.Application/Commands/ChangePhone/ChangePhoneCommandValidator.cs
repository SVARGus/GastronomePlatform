using FluentValidation;
using GastronomePlatform.Modules.Users.Domain.Entities;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangePhone
{
    /// <summary>
    /// Валидатор команды изменения телефона.
    /// </summary>
    /// <remarks>
    /// Лимит длины — единый источник в <see cref="UserProfile"/>
    /// (зеркало <c>Auth.Domain.Constants.AuthLimits.MAX_PHONE_LENGTH</c>).
    /// </remarks>
    public sealed class ChangePhoneCommandValidator : AbstractValidator<ChangePhoneCommand>
    {
        public ChangePhoneCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.NewPhone)
                .NotEmpty().WithMessage("Телефон обязателен.")
                .MaximumLength(UserProfile.MAX_PHONE_LENGTH)
                    .WithMessage($"Телефон не должен превышать {UserProfile.MAX_PHONE_LENGTH} символов.")
                .Matches(@"^\+?[0-9\s\-\(\)]+$")
                    .WithMessage("Некорректный формат номера телефона.");
        }
    }
}
