using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateIngredient
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateIngredientCommand"/>. Дублирует правила
    /// <c>CreateIngredientCommandValidator</c> для редактируемых полей.
    /// </summary>
    public sealed class UpdateIngredientCommandValidator : AbstractValidator<UpdateIngredientCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateIngredientCommandValidator"/>.
        /// </summary>
        public UpdateIngredientCommandValidator()
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty().WithMessage("Идентификатор ингредиента обязателен.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Название ингредиента обязательно.")
                .MinimumLength(Ingredient.MIN_NAME_LENGTH)
                    .WithMessage($"Название должно содержать не менее {Ingredient.MIN_NAME_LENGTH} символов.")
                .MaximumLength(Ingredient.MAX_NAME_LENGTH)
                    .WithMessage($"Название не должно превышать {Ingredient.MAX_NAME_LENGTH} символов.");

            RuleFor(x => x.PluralName)
                .MaximumLength(Ingredient.MAX_PLURAL_NAME_LENGTH)
                    .WithMessage($"Форма родительного падежа не должна превышать {Ingredient.MAX_PLURAL_NAME_LENGTH} символов.")
                .When(x => x.PluralName is not null);

            RuleFor(x => x.Description)
                .MaximumLength(Ingredient.MAX_DESCRIPTION_LENGTH)
                    .WithMessage($"Описание не должно превышать {Ingredient.MAX_DESCRIPTION_LENGTH} символов.")
                .When(x => x.Description is not null);

            RuleFor(x => x.BaseMeasureUnitId)
                .NotEmpty().WithMessage("Идентификатор базовой единицы измерения обязателен.");

            RuleFor(x => x.DensityApprox)
                .NotNull().WithMessage("Для жидкого продукта обязательно указать плотность.")
                .GreaterThan(0m).WithMessage("Плотность должна быть положительной.")
                .When(x => x.IsLiquid);

            RuleFor(x => x.DensityApprox)
                .GreaterThan(0m).WithMessage("Плотность должна быть положительной.")
                .When(x => !x.IsLiquid && x.DensityApprox.HasValue);

            RuleFor(x => x.AllergenType)
                .NotNull().WithMessage("Для аллергена обязательно указать тип.")
                .When(x => x.IsAllergen);

            RuleFor(x => x.AllergenType)
                .IsInEnum().WithMessage("Тип аллергена должен быть допустимым значением.")
                .When(x => x.AllergenType.HasValue);
        }
    }
}
