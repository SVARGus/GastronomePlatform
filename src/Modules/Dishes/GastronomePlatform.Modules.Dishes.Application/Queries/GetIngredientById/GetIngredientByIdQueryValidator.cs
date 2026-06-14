using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetIngredientById
{
    /// <summary>
    /// Валидатор запроса <see cref="GetIngredientByIdQuery"/>.
    /// </summary>
    public sealed class GetIngredientByIdQueryValidator : AbstractValidator<GetIngredientByIdQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetIngredientByIdQueryValidator"/>.
        /// </summary>
        public GetIngredientByIdQueryValidator()
        {
            RuleFor(x => x.IngredientId)
                .NotEmpty().WithMessage("Идентификатор ингредиента обязателен.");
        }
    }
}
