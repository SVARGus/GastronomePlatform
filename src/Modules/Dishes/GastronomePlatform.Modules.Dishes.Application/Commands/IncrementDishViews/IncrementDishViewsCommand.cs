using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.IncrementDishViews
{
    /// <summary>
    /// Команда инкремента счётчика просмотров блюда (UC-DSH-070).
    /// Внутренний UC, вызывается клиентом после успешного рендера публичной карточки
    /// (UC-DSH-050 / UC-DSH-051).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Команда атомарно увеличивает <c>Dish.ViewsCount</c> на 1 без загрузки агрегата
    /// (репозиторный <c>UPDATE</c> через EF Core <c>ExecuteUpdateAsync</c>). Условие
    /// «блюдо опубликовано» зашито в WHERE: для черновика / снятого / архивного блюда
    /// команда возвращает 404, не раскрывая существование чужой записи.
    /// </para>
    /// <para>
    /// На текущем этапе команда не требует аутентификации (эндпоинт
    /// <c>POST /api/dishes/{id}/views</c> публичный, гости считаются). Фильтр
    /// автора блюда (исключить «самопросмотры») — будущая задача.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    public sealed record IncrementDishViewsCommand(Guid DishId) : ICommand;
}
