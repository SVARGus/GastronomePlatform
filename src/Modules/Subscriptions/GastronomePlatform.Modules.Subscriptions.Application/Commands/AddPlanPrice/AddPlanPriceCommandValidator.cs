using FluentValidation;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.AddPlanPrice
{
    /// <summary>
    /// Валидатор команды <see cref="AddPlanPriceCommand"/>.
    /// </summary>
    /// <remarks>
    /// Валидатор проверяет только форму входных данных: длины строк, диапазоны,
    /// формат кода валюты. Доменные инварианты оффера (Amount ≥ 0, Trial-правила,
    /// переходы у не-recurring) проверяются в <c>PlanPrice.Create</c> и приходят
    /// как ошибки через <c>Result</c>-поток. Cross-price проверки (существование
    /// цели перехода, тот же <c>PlanId</c>) — в хендлере.
    /// </remarks>
    public sealed class AddPlanPriceCommandValidator : AbstractValidator<AddPlanPriceCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AddPlanPriceCommandValidator"/>.
        /// </summary>
        public AddPlanPriceCommandValidator()
        {
            RuleFor(x => x.PlanId)
                .NotEmpty().WithMessage("Идентификатор плана обязателен.");

            RuleFor(x => x.Kind)
                .IsInEnum().WithMessage("Указан недопустимый тип оффера (Kind).");

            RuleFor(x => x.PublicName)
                .MaximumLength(PlanPrice.MAX_PUBLIC_NAME_LENGTH)
                    .WithMessage($"Витринное имя оффера не должно превышать {PlanPrice.MAX_PUBLIC_NAME_LENGTH} символов.")
                .When(x => x.PublicName is not null);

            RuleFor(x => x.DurationDays)
                .GreaterThan(0).WithMessage("Длительность периода должна быть положительной.")
                .When(x => x.DurationDays.HasValue);

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Код валюты обязателен.")
                .Length(PlanPrice.CURRENCY_LENGTH)
                    .WithMessage($"Код валюты должен состоять из {PlanPrice.CURRENCY_LENGTH} символов (ISO 4217).")
                .Matches("^[A-Z]{3}$")
                    .WithMessage("Код валюты должен содержать три заглавные латинские буквы.");

            RuleFor(x => x.CompareAtAmount)
                .GreaterThan(x => x.Amount)
                    .WithMessage("«Старая цена» должна быть строго больше текущей суммы.")
                .When(x => x.CompareAtAmount.HasValue);

            RuleFor(x => x.DiscountPercent)
                .InclusiveBetween(0, 100).WithMessage("Скидка должна быть в диапазоне [0, 100] %.")
                .When(x => x.DiscountPercent.HasValue);

            RuleFor(x => x.TrialDays)
                .GreaterThan(0).WithMessage("Количество дней триала должно быть положительным.")
                .When(x => x.TrialDays.HasValue);

            RuleFor(x => x.InternalNotes)
                .MaximumLength(PlanPrice.MAX_INTERNAL_NOTES_LENGTH)
                    .WithMessage($"Служебные заметки не должны превышать {PlanPrice.MAX_INTERNAL_NOTES_LENGTH} символов.")
                .When(x => x.InternalNotes is not null);

            RuleFor(x => x)
                .Must(cmd => cmd.AvailableFrom < cmd.AvailableUntil)
                .When(cmd => cmd.AvailableFrom.HasValue && cmd.AvailableUntil.HasValue)
                .WithMessage("Начало окна доступности должно быть строго раньше конца окна.");
        }
    }
}
