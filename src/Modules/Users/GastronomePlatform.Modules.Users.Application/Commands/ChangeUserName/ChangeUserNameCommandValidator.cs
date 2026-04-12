using FluentValidation;

namespace GastronomePlatform.Modules.Users.Application.Commands.ChangeUserName
{
    /// <summary>
    /// Валидатор команды изменения никнейма.
    /// </summary>
    public sealed class ChangeUserNameCommandValidator : AbstractValidator<ChangeUserNameCommand>
    {
        public ChangeUserNameCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.NewUserName)
                .NotEmpty().WithMessage("Никнейм обязателен.")
                .MinimumLength(3).WithMessage("Никнейм должен содержать минимум 3 символа.")
                .MaximumLength(100).WithMessage("Никнейм не должен превышать 100 символов.")
                .Matches(@"^[a-zA-Z0-9_]+$")
                .WithMessage("Никнейм может содержать только латинские буквы, цифры и '_'.");
        }
    }
}
