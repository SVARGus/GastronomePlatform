namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMyDrafts
{
    /// <summary>
    /// Результат запроса <see cref="GetMyDraftsQuery"/> — постраничный список черновиков
    /// текущего пользователя с метаданными пагинации.
    /// </summary>
    /// <param name="Items">Элементы текущей страницы. Может быть пустым списком.</param>
    /// <param name="TotalCount">Общее количество черновиков пользователя (без учёта пагинации).</param>
    /// <param name="Page">Номер текущей страницы.</param>
    /// <param name="PageSize">Размер страницы, использованный при выборке.</param>
    public sealed record GetMyDraftsResult(
        IReadOnlyList<DishDraftListItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
