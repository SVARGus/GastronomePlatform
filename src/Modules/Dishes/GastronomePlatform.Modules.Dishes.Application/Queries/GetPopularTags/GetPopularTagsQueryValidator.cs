using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetPopularTags
{
    /// <summary>
    /// Валидатор запроса <see cref="GetPopularTagsQuery"/>.
    /// </summary>
    public sealed class GetPopularTagsQueryValidator : AbstractValidator<GetPopularTagsQuery>
    {
        /// <summary>Максимально допустимый <c>Limit</c>.</summary>
        private const int MAX_LIMIT = 50;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetPopularTagsQueryValidator"/>.
        /// </summary>
        public GetPopularTagsQueryValidator()
        {
            RuleFor(x => x.Limit)
                .InclusiveBetween(1, MAX_LIMIT)
                    .WithMessage($"Лимит должен быть в диапазоне 1..{MAX_LIMIT}.");
        }
    }
}
