using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetPopularTags
{
    /// <summary>
    /// Обработчик запроса <see cref="GetPopularTagsQuery"/> (UC-DSH-061).
    /// </summary>
    public sealed class GetPopularTagsQueryHandler
        : IQueryHandler<GetPopularTagsQuery, IReadOnlyList<TagDto>>
    {
        private readonly ITagRepository _tagRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetPopularTagsQueryHandler"/>.
        /// </summary>
        /// <param name="tagRepository">Репозиторий тегов.</param>
        public GetPopularTagsQueryHandler(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyList<TagDto>>> Handle(
            GetPopularTagsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Tag> tags = await _tagRepository.ListTopVerifiedByUsageAsync(
                request.Limit, cancellationToken);

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
