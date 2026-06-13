using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.RemoveRecipeIngredient
{
    /// <summary>
    /// Команда удаления позиции из рецепта блюда (UC-DSH-032).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.
    /// </para>
    /// <para>
    /// Domain после удаления переупорядочивает оставшиеся позиции
    /// (<c>Order = 1..n</c>). После успешного <c>Dish.RemoveRecipeIngredient</c>
    /// Handler вызывает <c>Dish.RecalculateDishMarkers</c> — состав изменился,
    /// маркеры могут отличаться (ADR-0016).
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="RecipeIngredientId">Идентификатор удаляемой позиции рецепта.</param>
    public sealed record RemoveRecipeIngredientCommand(
        Guid DishId,
        Guid RecipeIngredientId) : ICommand;
}
