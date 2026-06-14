using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.MoveCategory
{
    /// <summary>
    /// Команда перемещения категории в иерархии (UC-DSH-104). Меняет <c>ParentId</c>
    /// с проверкой циклов и соблюдения <c>Category.MAX_DEPTH</c>.
    /// </summary>
    /// <param name="CategoryId">Идентификатор перемещаемой категории.</param>
    /// <param name="NewParentId">Новый родитель или <see langword="null"/> для перемещения в корень.</param>
    public sealed record MoveCategoryCommand(Guid CategoryId, Guid? NewParentId) : ICommand;
}
