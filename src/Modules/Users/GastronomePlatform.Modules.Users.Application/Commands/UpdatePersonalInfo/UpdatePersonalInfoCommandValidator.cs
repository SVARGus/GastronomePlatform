using FluentValidation;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdatePersonalInfo
{
    /// <summary>
    /// Валидатор команды обновления персональных данных.
    /// </summary>
    public sealed class UpdatePersonalInfoCommandValidator : AbstractValidator<UpdatePersonalInfoCommand>
    {
        public UpdatePersonalInfoCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.FirstName)
                .MaximumLength(100).WithMessage("Имя не должно превышать 100 символов.")
                .When(x => x.FirstName is not null);

            RuleFor(x => x.LastName)
                .MaximumLength(100).WithMessage("Фамилия не должна превышать 100 символов.")
                .When(x => x.LastName is not null);

            RuleFor(x => x.MiddleName)
                .MaximumLength(100).WithMessage("Отчество не должно превышать 100 символов.")
                .When(x => x.MiddleName is not null);

            RuleFor(x => x.DisplayName)
                .MaximumLength(100).WithMessage("Отображаемое имя не должно превышать 100 символов.")
                .When(x => x.DisplayName is not null);

            RuleFor(x => x.Bio)
                .MaximumLength(2000).WithMessage("Описание не должно превышать 2000 символов.")
                .When(x => x.Bio is not null);

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Дата рождения не может быть в будущем.")
                .When(x => x.DateOfBirth is not null);
        }
    }
}
