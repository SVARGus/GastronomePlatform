using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ReorderRecipeIngredients
{
    /// <summary>
    /// Валидатор команды <see cref="ReorderRecipeIngredientsCommand"/>.
    /// </summary>
    /// <remarks>
    /// Структурные проверки: непустой список, отсутствие <c>Guid.Empty</c>.
    /// Полноту покрытия и отсутствие дубликатов проверяет Domain через
    /// <c>Recipe.ReorderIngredients</c> — возвращает <c>DISHES.INVALID_INGREDIENT_ORDER</c>
    /// или <c>DISHES.RECIPE_INGREDIENT_NOT_FOUND</c>.
    /// </remarks>
    public sealed class ReorderRecipeIngredientsCommandValidator
        : AbstractValidator<ReorderRecipeIngredientsCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ReorderRecipeIngredientsCommandValidator"/>.
        /// </summary>
        public ReorderRecipeIngredientsCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.OrderedIngredientIds)
                .NotNull().WithMessage("Список идентификаторов позиций обязателен.")
                .Must(list => list is not null && list.Count > 0)
                .WithMessage("Список идентификаторов позиций не должен быть пустым.")
                .Must(list => list is null || list.All(id => id != Guid.Empty))
                .WithMessage("Список не должен содержать пустых GUID.");
        }
    }
}
