using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryById
{
    /// <summary>
    /// Запрос карточки категории по идентификатору с непосредственными детьми
    /// (UC-DSH-058). Анонимный публичный эндпоинт.
    /// </summary>
    /// <param name="CategoryId">Идентификатор категории.</param>
    public sealed record GetCategoryByIdQuery(Guid CategoryId) : IQuery<CategoryDetailDto>;
}
