using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishBySlug
{
    /// <summary>
    /// Валидатор запроса <see cref="GetDishBySlugQuery"/>.
    /// </summary>
    /// <remarks>
    /// Лимит длины — 220 символов, совпадает с EF Configuration колонки <c>Dish.Slug</c>.
    /// Защищает от заведомо невалидных запросов до похода в БД.
    /// </remarks>
    public sealed class GetDishBySlugQueryValidator : AbstractValidator<GetDishBySlugQuery>
    {
        // Лимит длины slug в БД. Должен совпадать с DishConfiguration:
        // builder.Property(x => x.Slug).HasMaxLength(220).
        private const int MAX_SLUG_LENGTH = 220;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishBySlugQueryValidator"/>.
        /// </summary>
        public GetDishBySlugQueryValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug блюда обязателен.")
                .MaximumLength(MAX_SLUG_LENGTH)
                    .WithMessage($"Slug не должен превышать {MAX_SLUG_LENGTH} символов.");
        }
    }
}
