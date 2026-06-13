using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipeIngredient
{
    /// <summary>
    /// Валидатор команды <see cref="UpdateRecipeIngredientCommand"/>.
    /// </summary>
    /// <remarks>
    /// XOR <c>IngredientId</c> ↔ <c>FreeformText</c> и requirement
    /// «<c>IngredientSpecId</c> только при заполненном <c>IngredientId</c>»
    /// дублируются в Domain (<c>RecipeIngredient.Update</c>). Здесь — ранний 400-ответ
    /// до загрузки агрегата.
    /// </remarks>
    public sealed class UpdateRecipeIngredientCommandValidator
        : AbstractValidator<UpdateRecipeIngredientCommand>
    {
        private const int MIN_FREEFORM_LENGTH = 1;
        private const int MAX_FREEFORM_LENGTH = 200;
        private const int MAX_PREPARATION_NOTE_LENGTH = 200;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeIngredientCommandValidator"/>.
        /// </summary>
        public UpdateRecipeIngredientCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.RecipeIngredientId)
                .NotEmpty().WithMessage("Идентификатор позиции рецепта обязателен.");

            RuleFor(x => x.IngredientId)
                .NotEqual(Guid.Empty)
                .WithMessage("Идентификатор ингредиента не может быть пустым GUID.")
                .When(x => x.IngredientId.HasValue);

            RuleFor(x => x.IngredientSpecId)
                .NotEqual(Guid.Empty)
                .WithMessage("Идентификатор спецификации не может быть пустым GUID.")
                .When(x => x.IngredientSpecId.HasValue);

            RuleFor(x => x.Quantity)
                .GreaterThan(0m).WithMessage("Количество должно быть строго положительным.");

            RuleFor(x => x.MeasureUnitId)
                .NotEmpty().WithMessage("Идентификатор единицы измерения обязателен.");

            RuleFor(x => x.FreeformText)
                .Length(MIN_FREEFORM_LENGTH, MAX_FREEFORM_LENGTH)
                .WithMessage($"Свободный текст ингредиента должен быть от {MIN_FREEFORM_LENGTH} до {MAX_FREEFORM_LENGTH} символов.")
                .Must(text => !string.IsNullOrWhiteSpace(text))
                .WithMessage("Свободный текст ингредиента не должен состоять только из пробелов.")
                .When(x => x.FreeformText is not null);

            RuleFor(x => x.PreparationNote)
                .MaximumLength(MAX_PREPARATION_NOTE_LENGTH)
                .WithMessage($"Заметка по подготовке не должна превышать {MAX_PREPARATION_NOTE_LENGTH} символов.")
                .When(x => x.PreparationNote is not null);

            RuleFor(x => x)
                .Must(x => x.IngredientId.HasValue ^ !string.IsNullOrWhiteSpace(x.FreeformText))
                .WithMessage("Позиция должна задаваться ровно одним способом: либо IngredientId, либо FreeformText.");

            RuleFor(x => x)
                .Must(x => !x.IngredientSpecId.HasValue || x.IngredientId.HasValue)
                .WithMessage("Идентификатор спецификации допустим только при заполненном IngredientId.");
        }
    }
}
