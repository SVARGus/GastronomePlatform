using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryBySlug
{
    /// <summary>
    /// Запрос карточки категории по slug с непосредственными детьми
    /// (UC-DSH-059). Анонимный публичный эндпоинт.
    /// </summary>
    /// <param name="Slug">URL-friendly идентификатор категории.</param>
    public sealed record GetCategoryBySlugQuery(string Slug) : IQuery<CategoryDetailDto>;
}
