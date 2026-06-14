using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ChangeMainImage
{
    /// <summary>
    /// Команда изменения главного фото блюда (UC-DSH-011).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>Поле выделено в отдельный UC из-за межмодульной семантики attach/detach
    /// через <c>IMediaService</c>: смена фото — это не просто запись поля, но и
    /// синхронизация состояния медиафайла в модуле Media.</para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="MainImageId">
    /// Новый идентификатор главного фото из модуля Media.
    /// <see langword="null"/> — удалить ссылку (detach предыдущего, если был).
    /// </param>
    public sealed record ChangeMainImageCommand(
        Guid DishId,
        Guid? MainImageId) : ICommand;
}
