using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ArchiveDish
{
    /// <summary>
    /// Команда архивирования блюда (UC-DSH-006). Мягкое удаление: блюдо не отображается
    /// в каталоге и по прямой ссылке возвращает <c>404</c>, но данные остаются в БД.
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Переводит блюдо из <c>Draft</c> / <c>Published</c> / <c>Unpublished</c>
    /// в <c>Archived</c>: обнуляет <c>PublishedVersionData</c>, <c>PublishedAt</c>,
    /// <c>PublishedVersionUpdatedAt</c> и очищает связки <c>*Published</c>-таблиц.
    /// Основные таблицы агрегата (<c>Dish</c>, <c>Recipe</c> и подколлекции),
    /// привязанные медиафайлы и записи в <c>OrderItem.DishSnapshot</c> (модуль Orders,
    /// Этап 6+) — не затрагиваются.
    /// </para>
    /// <para>
    /// Тело запроса не содержит входных полей. Возвращает
    /// <see cref="Common.Domain.Results.Result"/> без значения — при успехе эндпоинт
    /// отдаёт <c>204 No Content</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор архивируемого блюда.</param>
    public sealed record ArchiveDishCommand(Guid DishId) : ICommand;
}
