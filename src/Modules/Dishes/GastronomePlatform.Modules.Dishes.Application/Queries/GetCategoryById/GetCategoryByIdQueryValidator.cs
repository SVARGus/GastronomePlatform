using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryById
{
    /// <summary>
    /// Валидатор запроса <see cref="GetCategoryByIdQuery"/>.
    /// </summary>
    public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetCategoryByIdQueryValidator"/>.
        /// </summary>
        public GetCategoryByIdQueryValidator()
        {
            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Идентификатор категории обязателен.");
        }
    }
}
