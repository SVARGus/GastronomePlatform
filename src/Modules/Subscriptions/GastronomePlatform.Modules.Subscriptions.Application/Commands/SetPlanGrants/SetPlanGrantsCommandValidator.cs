using FluentValidation;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.SetPlanGrants
{
    /// <summary>
    /// Валидатор команды <see cref="SetPlanGrantsCommand"/>.
    /// </summary>
    /// <remarks>
    /// Проверяет форму входных данных: валидность значений enum, отсутствие дубликатов,
    /// неотрицательность квоты. Прикладное правило «квота применима только к
    /// <c>PromotionAdvanced</c>» — в хендлере, так как оно опирается на реестр
    /// квотовых грантов и может расшириться на Этапе 4+.
    /// </remarks>
    public sealed class SetPlanGrantsCommandValidator : AbstractValidator<SetPlanGrantsCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetPlanGrantsCommandValidator"/>.
        /// </summary>
        public SetPlanGrantsCommandValidator()
        {
            RuleFor(x => x.PlanId)
                .NotEmpty().WithMessage("Идентификатор плана обязателен.");

            RuleFor(x => x.Grants)
                .NotNull().WithMessage("Список грантов обязателен (пустой список = снять все гранты).");

            RuleForEach(x => x.Grants).ChildRules(item =>
            {
                item.RuleFor(g => g.Grant)
                    .IsInEnum().WithMessage("Указан недопустимый идентификатор гранта (FeatureGrant).");

                item.RuleFor(g => g.Quantity)
                    .GreaterThan(0).WithMessage("Квота гранта должна быть положительной.")
                    .When(g => g.Quantity.HasValue);
            });

            RuleFor(x => x.Grants)
                .Must(list => list.Select(item => item.Grant).Distinct().Count() == list.Count)
                .WithMessage("В списке грантов не должно быть повторов.")
                .When(x => x.Grants is not null);
        }
    }
}
