using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishesByAuthor
{
    /// <summary>
    /// Результат запроса <see cref="GetDishesByAuthorQuery"/> — постраничный список
    /// публичных блюд автора с метаданными пагинации.
    /// </summary>
    /// <param name="Items">Элементы текущей страницы. Может быть пустым списком.</param>
    /// <param name="TotalCount">Общее количество публичных блюд автора (без учёта пагинации).</param>
    /// <param name="Page">Номер текущей страницы.</param>
    /// <param name="PageSize">Размер страницы, использованный при выборке.</param>
    public sealed record GetDishesByAuthorResult(
        IReadOnlyList<DishCardListItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
