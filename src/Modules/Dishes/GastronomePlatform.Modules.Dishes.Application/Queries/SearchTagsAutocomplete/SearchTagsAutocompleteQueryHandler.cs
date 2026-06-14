using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Helpers;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchTagsAutocomplete
{
    /// <summary>
    /// Обработчик запроса <see cref="SearchTagsAutocompleteQuery"/> (UC-DSH-060).
    /// </summary>
    /// <remarks>
    /// Нормализует входной префикс тем же <see cref="TagNameNormalizer"/>, что используется
    /// в UC-DSH-008 SetTags — клиент и сервер живут в одной модели нормализации.
    /// Если префикс после нормализации пуст — возвращается пустой список.
    /// </remarks>
    public sealed class SearchTagsAutocompleteQueryHandler
        : IQueryHandler<SearchTagsAutocompleteQuery, IReadOnlyList<TagDto>>
    {
        private readonly ITagRepository _tagRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SearchTagsAutocompleteQueryHandler"/>.
        /// </summary>
        /// <param name="tagRepository">Репозиторий тегов.</param>
        public SearchTagsAutocompleteQueryHandler(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyList<TagDto>>> Handle(
            SearchTagsAutocompleteQuery request,
            CancellationToken cancellationToken)
        {
            string normalized = TagNameNormalizer.Normalize(request.Query);
            if (normalized.Length == 0)
            {
                return Result<IReadOnlyList<TagDto>>.Success(Array.Empty<TagDto>());
            }

            IReadOnlyList<Tag> tags = await _tagRepository.SearchByNormalizedNamePrefixAsync(
                normalized, request.Limit, cancellationToken);

            IReadOnlyList<TagDto> dtos = tags
                .Select(t => new TagDto(
                    Id: t.Id,
                    Name: t.Name,
                    Slug: t.Slug,
                    UsageCount: t.UsageCount,
                    IsVerified: t.IsVerified))
                .ToList();

            return Result<IReadOnlyList<TagDto>>.Success(dtos);
        }
    }
}
