namespace GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos
{
    /// <summary>
    /// Узел дерева категорий — категория + рекурсивный список дочерних узлов
    /// (UC-DSH-057 GetCategoryTree).
    /// </summary>
    /// <param name="Id">Идентификатор категории.</param>
    /// <param name="Name">Отображаемое имя.</param>
    /// <param name="Slug">URL-friendly идентификатор.</param>
    /// <param name="Order">Порядок отображения внутри уровня иерархии.</param>
    /// <param name="IconMediaId">Идентификатор иконки в Media. Опционально.</param>
    /// <param name="Children">Дочерние узлы (могут быть пустыми).</param>
    public sealed record CategoryNodeDto(
        Guid Id,
        string Name,
        string Slug,
        int Order,
        Guid? IconMediaId,
        IReadOnlyList<CategoryNodeDto> Children);
}
