using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetPopularTags
{
    /// <summary>
    /// Запрос топ-N верифицированных тегов по <c>UsageCount</c>
    /// (UC-DSH-061). Анонимный публичный эндпоинт.
    /// </summary>
    /// <param name="Limit">Максимальное число возвращаемых тегов (1..50, дефолт — 20).</param>
    public sealed record GetPopularTagsQuery(int Limit) : IQuery<IReadOnlyList<TagDto>>;
}
