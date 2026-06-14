using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryTree
{
    /// <summary>
    /// Запрос полного дерева активных категорий каталога (UC-DSH-057).
    /// Иерархия собирается на стороне Application по полю <c>Category.ParentId</c>.
    /// </summary>
    /// <remarks>
    /// Анонимный публичный эндпоинт. Возвращается список корневых узлов; внутри
    /// каждого узла — рекурсивный список детей. Категории с <c>IsActive = false</c>
    /// в дерево не попадают.
    /// </remarks>
    public sealed record GetCategoryTreeQuery() : IQuery<IReadOnlyList<CategoryNodeDto>>;
}
