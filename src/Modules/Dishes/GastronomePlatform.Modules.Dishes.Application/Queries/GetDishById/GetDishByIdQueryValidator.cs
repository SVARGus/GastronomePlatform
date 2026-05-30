using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById
{
    /// <summary>
    /// Валидатор запроса <see cref="GetDishByIdQuery"/>.
    /// </summary>
    public sealed class GetDishByIdQueryValidator : AbstractValidator<GetDishByIdQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishByIdQueryValidator"/>.
        /// </summary>
        public GetDishByIdQueryValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");
        }
    }
}
