using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddFreeformIngredientToRecipe
{
    /// <summary>
    /// Валидатор команды <see cref="AddFreeformIngredientToRecipeCommand"/>.
    /// </summary>
    public sealed class AddFreeformIngredientToRecipeCommandValidator
        : AbstractValidator<AddFreeformIngredientToRecipeCommand>
    {
        private const int MIN_FREEFORM_LENGTH = 1;
        private const int MAX_FREEFORM_LENGTH = 200;
        private const int MAX_PREPARATION_NOTE_LENGTH = 200;

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
                .Length(MIN_FREEFORM_LENGTH, MAX_FREEFORM_LENGTH)
                .WithMessage($"Свободный текст ингредиента должен быть от {MIN_FREEFORM_LENGTH} до {MAX_FREEFORM_LENGTH} символов.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0m).WithMessage("Количество должно быть строго положительным.");

            RuleFor(x => x.MeasureUnitId)
                .NotEmpty().WithMessage("Идентификатор единицы измерения обязателен.");

            RuleFor(x => x.PreparationNote)
                .MaximumLength(MAX_PREPARATION_NOTE_LENGTH)
                .WithMessage($"Заметка по подготовке не должна превышать {MAX_PREPARATION_NOTE_LENGTH} символов.")
                .When(x => x.PreparationNote is not null);
        }
    }
}
