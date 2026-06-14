using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetScaledRecipeIngredients
{
    /// <summary>
    /// Валидатор запроса <see cref="GetScaledRecipeIngredientsQuery"/>.
    /// </summary>
    public sealed class GetScaledRecipeIngredientsQueryValidator
        : AbstractValidator<GetScaledRecipeIngredientsQuery>
    {
        // Защитный потолок — пересчёт на абсурдно большое число порций может дать
        // переполнение decimal или бессмысленный результат. Реалистичная верхняя граница
        // для бытового рецепта — порядка 1000 порций.
        private const int MAX_SERVINGS = 1000;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetScaledRecipeIngredientsQueryValidator"/>.
        /// </summary>
        public GetScaledRecipeIngredientsQueryValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.Servings)
                .InclusiveBetween(1, MAX_SERVINGS)
                    .WithMessage($"Число порций должно быть в диапазоне 1..{MAX_SERVINGS}.");
        }
    }
}
