namespace GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos
{
    /// <summary>
    /// Плоский DTO категории каталога — без детей. Используется в карточках и списках.
    /// </summary>
    /// <param name="Id">Идентификатор категории.</param>
    /// <param name="Name">Отображаемое имя.</param>
    /// <param name="Slug">URL-friendly идентификатор.</param>
    /// <param name="ParentId">Идентификатор родителя. <see langword="null"/> для корневых категорий.</param>
    /// <param name="Order">Порядок отображения внутри уровня иерархии.</param>
    /// <param name="IconMediaId">Идентификатор иконки в Media. Опционально.</param>
    public sealed record CategoryDto(
        Guid Id,
        string Name,
        string Slug,
        Guid? ParentId,
        int Order,
        Guid? IconMediaId);
}
