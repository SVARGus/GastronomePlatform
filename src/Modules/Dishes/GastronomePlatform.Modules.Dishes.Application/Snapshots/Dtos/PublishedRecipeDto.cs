namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
{
    /// <summary>
    /// Снепшот <c>Recipe</c> в составе jsonb-снепшота блюда — простые поля рецепта,
    /// 1:1-связки (<see cref="Timing"/>, <see cref="Yield"/>, <see cref="Nutrition"/>)
    /// и подколлекции (<see cref="Steps"/>, <see cref="Ingredients"/>) на момент публикации.
    /// </summary>
    /// <remarks>
    /// Идентификатор <c>Recipe.Id</c> в снепшот не включается: рецепт жёстко привязан
    /// к корню агрегата (1:1 с <c>Dish</c>), и при чтении снепшота он избыточен.
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
    public sealed record PublishedRecipeDto(
        string? IntroductionText,
        int ServingsDefault,
        bool IsAlcoholic,
        string? AuthorTips,
        string? ServingSuggestions,
        string? Notes,
        PublishedTimingDto Timing,
        PublishedYieldDto Yield,
        PublishedNutritionDto? Nutrition,
        IReadOnlyList<PublishedRecipeStepDto> Steps,
        IReadOnlyList<PublishedRecipeIngredientDto> Ingredients);
}
