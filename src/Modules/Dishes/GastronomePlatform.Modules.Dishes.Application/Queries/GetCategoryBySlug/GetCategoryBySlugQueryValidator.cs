using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryBySlug
{
    /// <summary>
    /// Валидатор запроса <see cref="GetCategoryBySlugQuery"/>.
    /// </summary>
    public sealed class GetCategoryBySlugQueryValidator : AbstractValidator<GetCategoryBySlugQuery>
    {
        // Лимит длины slug категории. Должен совпадать с CategoryConfiguration.
        private const int MAX_SLUG_LENGTH = 220;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetCategoryBySlugQueryValidator"/>.
        /// </summary>
        public GetCategoryBySlugQueryValidator()
        {
            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug категории обязателен.")
                .MaximumLength(MAX_SLUG_LENGTH)
                    .WithMessage($"Slug не должен превышать {MAX_SLUG_LENGTH} символов.");
        }
    }
}
