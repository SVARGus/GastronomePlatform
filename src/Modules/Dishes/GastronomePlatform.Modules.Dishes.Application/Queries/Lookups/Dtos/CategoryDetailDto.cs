namespace GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos
{
    /// <summary>
    /// Карточка категории каталога с непосредственными дочерними категориями
    /// (UC-DSH-058 GetCategoryById, UC-DSH-059 GetCategoryBySlug).
    /// </summary>
    /// <remarks>
    /// Возвращается только один уровень вниз — внуки не разворачиваются.
    /// Полное дерево — UC-DSH-057 GetCategoryTree.
    /// </remarks>
    /// <param name="Id">Идентификатор категории.</param>
    /// <param name="Name">Отображаемое имя.</param>
    /// <param name="Slug">URL-friendly идентификатор.</param>
    /// <param name="ParentId">Идентификатор родителя. <see langword="null"/> для корневых категорий.</param>
    /// <param name="Order">Порядок отображения внутри уровня иерархии.</param>
    /// <param name="IconMediaId">Идентификатор иконки в Media. Опционально.</param>
    /// <param name="Children">Непосредственные дочерние категории (отсортированы по <c>Order</c>).</param>
    public sealed record CategoryDetailDto(
        Guid Id,
        string Name,
        string Slug,
        Guid? ParentId,
        int Order,
        Guid? IconMediaId,
        IReadOnlyList<CategoryDto> Children);
}
