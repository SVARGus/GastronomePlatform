using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetTiming
{
    /// <summary>
    /// Валидатор команды <see cref="SetTimingCommand"/>.
    /// </summary>
    /// <remarks>
    /// Проверяет неотрицательность всех временных значений. Дополнительная проверка
    /// (<c>IsTotalManual = false</c> ⇒ <c>TotalTimeMinutes</c> вычисляется автоматически
    /// сервером — клиентское значение не используется) обеспечивается Domain-методом
    /// <c>Timing.UpdateTimes</c>.
    /// </remarks>
    public sealed class SetTimingCommandValidator : AbstractValidator<SetTimingCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetTimingCommandValidator"/>.
        /// </summary>
        public SetTimingCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.PrepTimeMinutes)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Время подготовки не может быть отрицательным.")
                .When(x => x.PrepTimeMinutes.HasValue);

            RuleFor(x => x.CookTimeMinutes)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Время основного приготовления не может быть отрицательным.")
                .When(x => x.CookTimeMinutes.HasValue);

            RuleFor(x => x.RestTimeMinutes)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Время отдыха не может быть отрицательным.")
                .When(x => x.RestTimeMinutes.HasValue);

            RuleFor(x => x.ActiveTimeMinutes)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Время активной работы повара не может быть отрицательным.")
                .When(x => x.ActiveTimeMinutes.HasValue);

            RuleFor(x => x.TotalTimeMinutes)
                .GreaterThanOrEqualTo(0)
                    .WithMessage("Общее время не может быть отрицательным.");
        }
    }
}
