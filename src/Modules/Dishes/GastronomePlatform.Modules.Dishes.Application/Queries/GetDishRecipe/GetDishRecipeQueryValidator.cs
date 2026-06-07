using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Валидатор запроса <see cref="GetDishRecipeQuery"/>.
    /// </summary>
    public sealed class GetDishRecipeQueryValidator : AbstractValidator<GetDishRecipeQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishRecipeQueryValidator"/>.
        /// </summary>
        public GetDishRecipeQueryValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");
        }
    }
}
