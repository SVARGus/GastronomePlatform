using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchTagsAutocomplete
{
    /// <summary>
    /// Валидатор запроса <see cref="SearchTagsAutocompleteQuery"/>.
    /// </summary>
    public sealed class SearchTagsAutocompleteQueryValidator
        : AbstractValidator<SearchTagsAutocompleteQuery>
    {
        /// <summary>Максимально допустимый <c>Limit</c> для автокомплита.</summary>
        private const int MAX_LIMIT = 50;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SearchTagsAutocompleteQueryValidator"/>.
        /// </summary>
        public SearchTagsAutocompleteQueryValidator()
        {
            RuleFor(x => x.Query)
                .NotEmpty().WithMessage("Подстрока поиска обязательна.")
                .MaximumLength(Tag.MAX_NAME_LENGTH)
                    .WithMessage($"Подстрока поиска не должна превышать {Tag.MAX_NAME_LENGTH} символов.");

            RuleFor(x => x.Limit)
                .InclusiveBetween(1, MAX_LIMIT)
                    .WithMessage($"Лимит должен быть в диапазоне 1..{MAX_LIMIT}.");
        }
    }
}
