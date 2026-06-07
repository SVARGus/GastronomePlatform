namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Публичное представление рецепта блюда (UC-DSH-052). Содержит простые поля
    /// рецепта, 1:1-связки (<see cref="TimingViewDto"/>, <see cref="YieldViewDto"/>,
    /// <see cref="NutritionViewDto"/>) и подколлекции (<see cref="Steps"/>,
    /// <see cref="Ingredients"/>).
    /// </summary>
    /// <remarks>
    /// Идентификатор <c>Recipe.Id</c> в DTO не передаётся: рецепт жёстко привязан
    /// к корню агрегата (1:1 с <c>Dish</c>), и в публичном представлении он избыточен.
    /// Идентификатор блюда передаётся в обёртке <see cref="DishRecipeDto"/>.
    /// </remarks>
    /// <param name="IntroductionText">Вводный текст рецепта. <see langword="null"/>, если не задан.</param>
    /// <param name="ServingsDefault">Количество порций по умолчанию (≥ 1).</param>
    /// <param name="IsAlcoholic">Признак содержания алкоголя.</param>
    /// <param name="AuthorTips">Советы автора по приготовлению. Опционально.</param>
    /// <param name="ServingSuggestions">Рекомендации по сервировке. Опционально.</param>
    /// <param name="Notes">Дополнительные заметки. Опционально.</param>
    /// <param name="Timing">Времена этапов приготовления.</param>
    /// <param name="Yield">Выход готового продукта и размер порции.</param>
    /// <param name="Nutrition">Пищевая ценность. <see langword="null"/>, если автор не задал.</param>
    /// <param name="Steps">Шаги приготовления, упорядоченные по <c>Order</c>.</param>
    /// <param name="Ingredients">Ингредиенты рецепта (полиморфно по природе catalog/freeform),
    /// упорядоченные по <c>Order</c>.</param>
    public sealed record RecipeViewDto(
        string? IntroductionText,
        int ServingsDefault,
        bool IsAlcoholic,
        string? AuthorTips,
        string? ServingSuggestions,
        string? Notes,
        TimingViewDto Timing,
        YieldViewDto Yield,
        NutritionViewDto? Nutrition,
        IReadOnlyList<RecipeStepViewDto> Steps,
        IReadOnlyList<RecipeIngredientViewDto> Ingredients);
}
