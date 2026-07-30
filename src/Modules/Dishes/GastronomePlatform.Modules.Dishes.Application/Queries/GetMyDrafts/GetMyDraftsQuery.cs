using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMyDrafts
{
    /// <summary>
    /// Запрос постраничного списка непубличных блюд текущего пользователя
    /// по статусу: черновики (<c>Draft</c>) либо снятые с публикации
    /// (<c>Unpublished</c>) — вкладки раздела «Мои блюда».
    /// Сортировка фиксирована: <c>UpdatedAt DESC</c> (свежие правки сверху — «продолжить работу»).
    /// </summary>
    /// <param name="Page">Номер страницы, начиная с 1.</param>
    /// <param name="PageSize">Количество элементов на странице (1–25).</param>
    /// <param name="Status">
    /// Статус выборки: <see cref="DishStatus.Draft"/> (по умолчанию) или
    /// <see cref="DishStatus.Unpublished"/>. Остальные статусы отклоняются валидатором:
    /// опубликованные отдаёт UC-DSH-055, архив недоступен автору на этом этапе.
    /// </param>
    public sealed record GetMyDraftsQuery(
        int Page,
        int PageSize,
        DishStatus Status = DishStatus.Draft) : IQuery<GetMyDraftsResult>;
}
