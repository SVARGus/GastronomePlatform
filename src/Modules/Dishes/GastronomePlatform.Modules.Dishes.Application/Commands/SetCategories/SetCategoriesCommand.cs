using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetCategories
{
    /// <summary>
    /// Команда установки набора категорий блюда (UC-DSH-007). Replace-семантика:
    /// присланный список полностью заменяет текущий набор связок
    /// <c>DishCategory</c> блюда (0–3 категории).
    /// </summary>
    /// <remarks>
    /// <para>Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.</para>
    /// <para>
    /// Пустой список — допустимое значение (снять все категории). Domain проверяет
    /// лимит и отсутствие дубликатов; существование каждого <c>CategoryId</c> и
    /// активность категории — забота Handler-а (запрос
    /// <c>ICategoryRepository.ListByIdsAsync</c>).
    /// </para>
    /// <para>
    /// Правка не трогает <c>PublishedVersionData</c> и связку
    /// <c>DishCategoryPublished</c>: посетители каталога видят прежний набор категорий
    /// до явной перепубликации (UC-DSH-004).
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="CategoryIds">Новый набор идентификаторов категорий (0–3, без дубликатов).</param>
    public sealed record SetCategoriesCommand(
        Guid DishId,
        IReadOnlyList<Guid> CategoryIds) : ICommand;
}
