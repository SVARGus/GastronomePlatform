using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetDietLabels
{
    /// <summary>
    /// Команда установки битовой маски диетических меток блюда (UC-DSH-009).
    /// Реализует ADR-0016: Reject-семантика при конфликте маски с составом рецепта.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.
    /// </para>
    /// <para>
    /// Если запрошенная маска содержит биты, конфликтующие с
    /// <c>Ingredient.DietConflictsMask</c> хотя бы одного catalog-ингредиента
    /// текущего рецепта, команда возвращает <c>DISHES.DIET_LABELS_CONFLICT_WITH_COMPOSITION</c>
    /// (HTTP 409). Автор может либо снять конфликтующие биты, либо изменить состав.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="DietLabelsMask">Новая битовая маска диетических меток.
    /// <see cref="DietLabels.None"/> — допустимое значение (снять все метки).</param>
    public sealed record SetDietLabelsCommand(
        Guid DishId,
        DietLabels DietLabelsMask) : ICommand;
}
