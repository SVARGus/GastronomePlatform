using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetHistory
{
    /// <summary>
    /// Команда установки историко-культурного описания блюда (UC-DSH-010).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Замена поля <c>Dish.HistoryText</c>. <see langword="null"/> — очистить.
    /// Это длинный текст до 4000 символов, обычно редактируется на отдельном экране UI,
    /// поэтому UC выделен в отдельный эндпоинт, а не в составе UC-DSH-002 UpdateDishCard.
    /// </para>
    /// <para>Правка не трогает <c>PublishedVersionData</c>.</para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="HistoryText">Новый текст истории. <see langword="null"/> — очистить.</param>
    public sealed record SetHistoryCommand(Guid DishId, string? HistoryText) : ICommand;
}
