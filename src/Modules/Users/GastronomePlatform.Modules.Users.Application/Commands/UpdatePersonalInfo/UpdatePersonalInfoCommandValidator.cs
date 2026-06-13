using FluentValidation;
using GastronomePlatform.Modules.Users.Domain.Entities;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdatePersonalInfo
{
    /// <summary>
    /// Валидатор команды обновления персональных данных.
    /// </summary>
    /// <remarks>
    /// Лимиты длины полей — единый источник в <see cref="UserProfile"/>.
    /// </remarks>
    public sealed class UpdatePersonalInfoCommandValidator : AbstractValidator<UpdatePersonalInfoCommand>
    {
        public UpdatePersonalInfoCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.FirstName)
                .MaximumLength(UserProfile.MAX_FIRST_NAME_LENGTH)
                    .WithMessage($"Имя не должно превышать {UserProfile.MAX_FIRST_NAME_LENGTH} символов.")
                .When(x => x.FirstName is not null);

            RuleFor(x => x.LastName)
                .MaximumLength(UserProfile.MAX_LAST_NAME_LENGTH)
                    .WithMessage($"Фамилия не должна превышать {UserProfile.MAX_LAST_NAME_LENGTH} символов.")
                .When(x => x.LastName is not null);

            RuleFor(x => x.MiddleName)
                .MaximumLength(UserProfile.MAX_MIDDLE_NAME_LENGTH)
                    .WithMessage($"Отчество не должно превышать {UserProfile.MAX_MIDDLE_NAME_LENGTH} символов.")
                .When(x => x.MiddleName is not null);

            RuleFor(x => x.DisplayName)
                .MaximumLength(UserProfile.MAX_DISPLAY_NAME_LENGTH)
                    .WithMessage($"Отображаемое имя не должно превышать {UserProfile.MAX_DISPLAY_NAME_LENGTH} символов.")
                .When(x => x.DisplayName is not null);

            RuleFor(x => x.Bio)
                .MaximumLength(UserProfile.MAX_BIO_LENGTH)
                    .WithMessage($"Описание не должно превышать {UserProfile.MAX_BIO_LENGTH} символов.")
                .When(x => x.Bio is not null);

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Дата рождения не может быть в будущем.")
                .When(x => x.DateOfBirth is not null);
        }
    }
}
