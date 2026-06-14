using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateCategory
{
    /// <summary>
    /// Команда обновления категории (UC-DSH-102). Меняет <see cref="Name"/>,
    /// <see cref="Order"/>, <see cref="IconMediaId"/> и <see cref="IsActive"/>.
    /// Slug и <c>ParentId</c> — отдельные операции (UC-DSH-105 / UC-DSH-104).
    /// </summary>
    /// <param name="CategoryId">Идентификатор существующей категории.</param>
    /// <param name="Name">Новое имя.</param>
    /// <param name="Order">Новый порядок отображения.</param>
    /// <param name="IconMediaId">Идентификатор иконки. Опционально.</param>
    /// <param name="IsActive">Признак активности. Можно одновременно с правкой остальных полей.</param>
    public sealed record UpdateCategoryCommand(
        Guid CategoryId,
        string Name,
        int Order,
        Guid? IconMediaId,
        bool IsActive) : ICommand;
}
