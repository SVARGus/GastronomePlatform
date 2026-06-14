using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchIngredients
{
    /// <summary>
    /// Валидатор запроса <see cref="SearchIngredientsQuery"/>.
    /// </summary>
    public sealed class SearchIngredientsQueryValidator : AbstractValidator<SearchIngredientsQuery>
    {
        /// <summary>Лимит длины префикса поиска (синхронизирован с длиной <c>Ingredient.Name</c> в БД).</summary>
        private const int MAX_QUERY_LENGTH = 200;

        /// <summary>Максимально допустимый <c>Limit</c>.</summary>
        private const int MAX_LIMIT = 50;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SearchIngredientsQueryValidator"/>.
        /// </summary>
        public SearchIngredientsQueryValidator()
        {
            RuleFor(x => x.Query)
                .NotEmpty().WithMessage("Префикс поиска обязателен.")
                .MaximumLength(MAX_QUERY_LENGTH)
                    .WithMessage($"Префикс поиска не должен превышать {MAX_QUERY_LENGTH} символов.");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, MAX_LIMIT)
                    .WithMessage($"Лимит должен быть в диапазоне 1..{MAX_LIMIT}.");
        }
    }
}
