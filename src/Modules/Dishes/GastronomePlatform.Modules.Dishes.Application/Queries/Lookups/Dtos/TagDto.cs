namespace GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos
{
    /// <summary>
    /// DTO тега для автокомплита и облака популярных тегов
    /// (UC-DSH-060 SearchTagsAutocomplete, UC-DSH-061 GetPopularTags).
    /// </summary>
    /// <param name="Id">Идентификатор тега.</param>
    /// <param name="Name">Оригинальное написание (как ввёл пользователь).</param>
    /// <param name="Slug">URL-friendly идентификатор тега (страницы вида <c>/tags/vegan</c>).</param>
    /// <param name="UsageCount">Количество блюд, к которым прикреплён тег.</param>
    /// <param name="IsVerified">Признак admin-верификации.</param>
    public sealed record TagDto(
        Guid Id,
        string Name,
        string Slug,
        int UsageCount,
        bool IsVerified);
}
