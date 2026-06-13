using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddCatalogIngredientToRecipe
{
    /// <summary>
    /// Валидатор команды <see cref="AddCatalogIngredientToRecipeCommand"/>.
    /// </summary>
    /// <remarks>
    /// Существование <c>Ingredient</c>, <c>IngredientSpec</c> и <c>MeasureUnit</c>
    /// валидируется на уровне Handler-а (требуется обращение к БД) и возвращает
    /// доменные ошибки <c>DISHES.*_NOT_FOUND</c> / <c>DISHES.INGREDIENT_INACTIVE</c>
    /// / <c>DISHES.INGREDIENT_SPEC_MISMATCH</c>. Здесь — только структурные проверки.
    /// Лимиты длины полей — единый источник в <see cref="RecipeIngredient"/>.
    /// </remarks>
    public sealed class AddCatalogIngredientToRecipeCommandValidator
        : AbstractValidator<AddCatalogIngredientToRecipeCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AddCatalogIngredientToRecipeCommandValidator"/>.
        /// </summary>
        public AddCatalogIngredientToRecipeCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.IngredientId)
                .NotEmpty().WithMessage("Идентификатор ингредиента обязателен.");

            RuleFor(x => x.IngredientSpecId)
                .NotEqual(Guid.Empty)
                .WithMessage("Идентификатор спецификации не может быть пустым GUID.")
                .When(x => x.IngredientSpecId.HasValue);

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
