namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetScaledRecipeIngredients
{
    /// <summary>
    /// Результат запроса <see cref="GetScaledRecipeIngredientsQuery"/> — список ингредиентов
    /// рецепта с количествами, пересчитанными на запрошенное число порций.
    /// </summary>
    /// <param name="ServingsDefault">Исходное число порций рецепта (из snapshot).</param>
    /// <param name="ServingsRequested">Запрошенное число порций.</param>
    /// <param name="Multiplier">Коэффициент пересчёта: <c>ServingsRequested / ServingsDefault</c>.</param>
    /// <param name="Ingredients">Список позиций с исходным и пересчитанным количеством.</param>
    public sealed record GetScaledRecipeIngredientsResult(
        int ServingsDefault,
        int ServingsRequested,
        decimal Multiplier,
        IReadOnlyList<ScaledIngredientDto> Ingredients);
}
