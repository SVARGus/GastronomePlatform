using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ReorderRecipeIngredients
{
    /// <summary>
    /// Команда массового переупорядочивания позиций рецепта (UC-DSH-033).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.
    /// </para>
    /// <para>
    /// Состав ингредиентов не меняется — пересчёт маркеров не требуется.
    /// Domain проверяет, что переданный список содержит все позиции рецепта
    /// без дубликатов; иначе — <c>DISHES.INVALID_INGREDIENT_ORDER</c>
    /// или <c>DISHES.RECIPE_INGREDIENT_NOT_FOUND</c>.
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="OrderedIngredientIds">Идентификаторы позиций рецепта в желаемом порядке.</param>
    public sealed record ReorderRecipeIngredientsCommand(
        Guid DishId,
        IReadOnlyList<Guid> OrderedIngredientIds) : ICommand;
}
