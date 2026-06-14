using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetNutrition
{
    /// <summary>
    /// Валидатор команды <see cref="SetNutritionCommand"/>.
    /// </summary>
    /// <remarks>
    /// Domain (<c>Nutrition.Update</c>) валидацию не повторяет — единственный источник
    /// правды для значений КБЖУ — этот валидатор.
    /// </remarks>
    public sealed class SetNutritionCommandValidator : AbstractValidator<SetNutritionCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetNutritionCommandValidator"/>.
        /// </summary>
        public SetNutritionCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.CalcMethod)
                .IsInEnum().WithMessage("Способ расчёта КБЖУ должен быть допустимым значением.");

            RuleFor(x => x.Calories)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Калорийность не может быть отрицательной.");

            RuleFor(x => x.Proteins)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Белки не могут быть отрицательными.");

            RuleFor(x => x.Fats)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Жиры не могут быть отрицательными.");

            RuleFor(x => x.Carbs)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Углеводы не могут быть отрицательными.");

            RuleFor(x => x.SaturatedFats)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Насыщенные жиры не могут быть отрицательными.")
                .LessThanOrEqualTo(x => x.Fats)
                    .WithMessage("Насыщенные жиры не должны превышать общее количество жиров.")
                .When(x => x.SaturatedFats.HasValue);

            RuleFor(x => x.Sugar)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Сахара не могут быть отрицательными.")
                .LessThanOrEqualTo(x => x.Carbs)
                    .WithMessage("Сахара не должны превышать общее количество углеводов.")
                .When(x => x.Sugar.HasValue);

            RuleFor(x => x.Fiber)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Клетчатка не может быть отрицательной.")
                .When(x => x.Fiber.HasValue);

            RuleFor(x => x.Salt)
                .GreaterThanOrEqualTo(0m)
                    .WithMessage("Соль не может быть отрицательной.")
                .When(x => x.Salt.HasValue);
        }
    }
}
