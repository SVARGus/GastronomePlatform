using FluentValidation;
using GastronomePlatform.Modules.Users.Domain.Entities;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdateLocation
{
    /// <summary>
    /// Валидатор команды обновления местоположения.
    /// </summary>
    /// <remarks>
    /// Лимиты длины полей — единый источник в <see cref="UserProfile"/>.
    /// </remarks>
    public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
    {
        public UpdateLocationCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.Country)
                .MaximumLength(UserProfile.MAX_COUNTRY_LENGTH)
                    .WithMessage($"Страна не должна превышать {UserProfile.MAX_COUNTRY_LENGTH} символов.")
                .When(x => x.Country is not null);

            RuleFor(x => x.Region)
                .MaximumLength(UserProfile.MAX_REGION_LENGTH)
                    .WithMessage($"Регион не должен превышать {UserProfile.MAX_REGION_LENGTH} символов.")
                .When(x => x.Region is not null);

            RuleFor(x => x.City)
                .MaximumLength(UserProfile.MAX_CITY_LENGTH)
                    .WithMessage($"Город не должен превышать {UserProfile.MAX_CITY_LENGTH} символов.")
                .When(x => x.City is not null);
        }
    }
}
