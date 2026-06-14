using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateCategory
{
    /// <summary>
    /// Команда создания категории каталога (UC-DSH-101). Авторизация — роль <c>Admin</c>.
    /// Slug генерируется сервером из <paramref name="Name"/> через <c>ISlugGenerator</c>;
    /// при коллизии добавляется суффикс <c>-N</c>.
    /// </summary>
    /// <param name="Name">Отображаемое имя категории (2..100 символов).</param>
    /// <param name="ParentId">Идентификатор родителя. <see langword="null"/> — корневая категория.</param>
    /// <param name="Order">Порядок отображения внутри уровня иерархии.</param>
    /// <param name="IconMediaId">Идентификатор иконки в Media. Опционально.</param>
    public sealed record CreateCategoryCommand(
        string Name,
        Guid? ParentId,
        int Order,
        Guid? IconMediaId) : ICommand<CreateCategoryResult>;
}
