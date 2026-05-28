using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMyDrafts
{
    /// <summary>
    /// Запрос постраничного списка черновиков (<c>Status = Draft</c>) текущего пользователя.
    /// Сортировка фиксирована: <c>UpdatedAt DESC</c> (свежие правки сверху — «продолжить работу»).
    /// </summary>
    /// <param name="Page">Номер страницы, начиная с 1.</param>
    /// <param name="PageSize">Количество элементов на странице (1–25).</param>
    public sealed record GetMyDraftsQuery(int Page, int PageSize) : IQuery<GetMyDraftsResult>;
}
