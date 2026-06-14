using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeleteOrDeactivateCategory
{
    /// <summary>
    /// Команда удаления или деактивации категории (UC-DSH-103). Сервер сам решает,
    /// можно ли удалить запись физически (нет детей и нет связей <c>DishCategory</c>);
    /// иначе — мягкое <c>IsActive = false</c>.
    /// </summary>
    /// <param name="CategoryId">Идентификатор категории.</param>
    public sealed record DeleteOrDeactivateCategoryCommand(Guid CategoryId)
        : ICommand<DeleteOrDeactivateCategoryResult>;
}
