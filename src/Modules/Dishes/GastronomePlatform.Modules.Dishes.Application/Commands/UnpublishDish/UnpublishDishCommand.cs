using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UnpublishDish
{
    /// <summary>
    /// Команда снятия блюда с публикации (UC-DSH-005).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Переводит блюдо из <c>Published</c> в <c>Unpublished</c>: обнуляет
    /// <c>PublishedVersionData</c>, <c>PublishedAt</c>, <c>PublishedVersionUpdatedAt</c>
    /// и очищает связки <c>*Published</c>-таблиц (<c>DishCategoryPublished</c>,
    /// <c>DishTagPublished</c>). Основные таблицы агрегата (<c>Dish</c>, <c>Recipe</c>
    /// и подколлекции) не затрагиваются — автор может вернуться к редактированию
    /// или повторно опубликовать через <c>UC-DSH-004</c>.
    /// </para>
    /// <para>
    /// Тело запроса не содержит входных полей: снятие — переход состояния без
    /// дополнительных параметров. Возвращает <see cref="Common.Domain.Results.Result"/>
    /// без значения — при успехе эндпоинт отдаёт <c>204 No Content</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда, снимаемого с публикации.</param>
    public sealed record UnpublishDishCommand(Guid DishId) : ICommand;
}
