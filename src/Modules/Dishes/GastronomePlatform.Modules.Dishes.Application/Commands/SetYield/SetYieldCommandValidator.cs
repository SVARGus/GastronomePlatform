using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetYield
{
    /// <summary>
    /// Валидатор команды <see cref="SetYieldCommand"/>.
    /// </summary>
    /// <remarks>
    /// Проверяет валидность <c>YieldUnit</c> (член enum), неотрицательность
    /// <c>QuantityTotal</c> и <c>GramsPerServing</c>, <c>ServingsCount ≥ 1</c>.
    /// Дублирующая Domain-проверка остаётся для defense-in-depth.
    /// </remarks>
    public sealed class SetYieldCommandValidator : AbstractValidator<SetYieldCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetYieldCommandValidator"/>.
        /// </summary>
        public SetYieldCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.YieldUnit)
                .IsInEnum().WithMessage("Единица выхода должна быть допустимым значением.");

            RuleFor(x => x.QuantityTotal)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Общее количество готового продукта не может быть отрицательным.");

            RuleFor(x => x.ServingsCount)
                .GreaterThanOrEqualTo(1)
                    .WithMessage("Количество порций должно быть не меньше 1.");

            RuleFor(x => x.GramsPerServing)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Вес порции не может быть отрицательным.")
                .When(x => x.GramsPerServing.HasValue);
        }
    }
}
