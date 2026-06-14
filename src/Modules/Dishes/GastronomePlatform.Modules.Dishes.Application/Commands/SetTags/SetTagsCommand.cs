using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetTags
{
    /// <summary>
    /// Команда установки набора тегов блюда (UC-DSH-008). Replace-семантика:
    /// присланный список имён полностью заменяет текущий набор связок
    /// <c>DishTag</c>. Лимит — 20 тегов (Domain).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Клиент передаёт <b>имена</b>, не идентификаторы: сервер выполняет find-or-create
    /// по нормализованной форме (<c>Trim + lower + collapse-whitespace</c>). Существующие
    /// теги переиспользуются, отсутствующие — создаются и сохраняются в справочник.
    /// Дубликаты в пределах команды (например, «Веган» и «веган»), различающиеся
    /// только регистром или пробелами, схлопываются в один тег без ошибки.
    /// </para>
    /// <para>
    /// Пустой список — снять все теги. <c>Tag.UsageCount</c> атомарно
    /// пересчитывается через дельту старых/новых тегов.
    /// </para>
    /// <para>
    /// Правка не трогает <c>PublishedVersionData</c> и связку <c>DishTagPublished</c>:
    /// каталог продолжает показывать прежний набор до явной перепубликации (UC-DSH-004).
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="TagNames">Имена тегов (0..20 после дедупликации по нормализованной форме).</param>
    public sealed record SetTagsCommand(
        Guid DishId,
        IReadOnlyList<string> TagNames) : ICommand;
}
