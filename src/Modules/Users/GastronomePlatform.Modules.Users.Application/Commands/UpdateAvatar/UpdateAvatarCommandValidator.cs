using FluentValidation;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdateAvatar
{
    /// <summary>
    /// Валидатор команды обновления аватара.
    /// </summary>
    public sealed class UpdateAvatarCommandValidator : AbstractValidator<UpdateAvatarCommand>
    {
        public UpdateAvatarCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");
        }
    }
}
