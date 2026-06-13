using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddFreeformIngredientToRecipe
{
    /// <summary>
    /// Валидатор команды <see cref="AddFreeformIngredientToRecipeCommand"/>.
    /// </summary>
    /// <remarks>
    /// Лимиты длины полей — единый источник в <see cref="RecipeIngredient"/>.
    /// </remarks>
    public sealed class AddFreeformIngredientToRecipeCommandValidator
        : AbstractValidator<AddFreeformIngredientToRecipeCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AddFreeformIngredientToRecipeCommandValidator"/>.
        /// </summary>
        public AddFreeformIngredientToRecipeCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.FreeformText)
                .NotEmpty().WithMessage("Свободный текст ингредиента обязателен.")
                .Must(text => !string.IsNullOrWhiteSpace(text))
                    .WithMessage("Свободный текст ингредиента не должен состоять только из пробелов.")
                .Length(RecipeIngredient.MIN_FREEFORM_LENGTH, RecipeIngredient.MAX_FREEFORM_LENGTH)
                    .WithMessage(
                        $"Свободный текст ингредиента должен быть от {RecipeIngredient.MIN_FREEFORM_LENGTH} " +
                        $"до {RecipeIngredient.MAX_FREEFORM_LENGTH} символов.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0m).WithMessage("Количество должно быть строго положительным.");

            RuleFor(x => x.MeasureUnitId)
                .NotEmpty().WithMessage("Идентификатор единицы измерения обязателен.");

            RuleFor(x => x.PreparationNote)
                .MaximumLength(RecipeIngredient.MAX_PREPARATION_NOTE_LENGTH)
                    .WithMessage($"Заметка по подготовке не должна превышать {RecipeIngredient.MAX_PREPARATION_NOTE_LENGTH} символов.")
                .When(x => x.PreparationNote is not null);
        }
    }
}
