using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishesByAuthor
{
    /// <summary>
    /// Запрос постраничного списка опубликованных блюд указанного автора (UC-DSH-055).
    /// Анонимный публичный эндпоинт. Сортировка — по <c>PublishedAt</c> убыванию.
    /// </summary>
    /// <remarks>
    /// Возвращает только публичные блюда (<c>PublishedVersionData IS NOT NULL</c>).
    /// Черновики, снятые и архивированные блюда в выдачу не попадают, даже если
    /// запрос делает сам автор — для этого есть <c>UC-DSH-053 GetMyDrafts</c>.
    /// </remarks>
    /// <param name="AuthorUserId">Идентификатор автора.</param>
    /// <param name="Page">Номер страницы, начиная с 1.</param>
    /// <param name="PageSize">Количество элементов на странице (1..25).</param>
    public sealed record GetDishesByAuthorQuery(
        Guid AuthorUserId,
        int Page,
        int PageSize) : IQuery<GetDishesByAuthorResult>;
}
