using FluentValidation;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangePhone
{
    /// <summary>
    /// Валидатор команды изменения телефона.
    /// </summary>
    public sealed class ChangePhoneCommandValidator : AbstractValidator<ChangePhoneCommand>
    {
        public ChangePhoneCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.NewPhone)
                .NotEmpty().WithMessage("Телефон обязателен.")
                .MaximumLength(50).WithMessage("Телефон не должен превышать 50 символов.")
                .Matches(@"^\+?[0-9\s\-\(\)]+$")
                .WithMessage("Некорректный формат номера телефона.");
        }
    }
}
