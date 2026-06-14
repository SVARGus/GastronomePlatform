using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeleteTag
{
    /// <summary>
    /// Команда удаления тега администратором (UC-DSH-131). Hard delete: тег и все его
    /// связки <c>DishTag</c>/<c>DishTagPublished</c> исчезают из БД. У всех затронутых блюд
    /// обновляется <c>Dish.UpdatedAt</c> — индикатор «есть несохранённые правки» сработает.
    /// </summary>
    /// <remarks>
    /// Используется для очистки спама/мата. Авторизация — роль <c>Admin</c>; проверка
    /// на эндпоинте через <c>[Authorize(Roles = PlatformRoles.ADMIN)]</c>.
    /// </remarks>
    /// <param name="TagId">Идентификатор удаляемого тега.</param>
    public sealed record DeleteTagCommand(Guid TagId) : ICommand;
}
