using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchDishes
{
    /// <summary>
    /// Результат запроса <see cref="SearchDishesQuery"/> — постраничный список
    /// опубликованных блюд с метаданными пагинации.
    /// </summary>
    /// <param name="Items">Карточки блюд текущей страницы. Может быть пустым.</param>
    /// <param name="TotalCount">Общее количество подходящих блюд без учёта пагинации.</param>
    /// <param name="Page">Номер текущей страницы.</param>
    /// <param name="PageSize">Размер страницы, использованный при выборке.</param>
    public sealed record SearchDishesResult(
        IReadOnlyList<DishCardListItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
