namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Позиция рецепта со свободным текстом (freeform-природа) — для случаев,
    /// когда нужного ингредиента в справочнике нет на момент публикации.
    /// Дискриминатор JSON: <c>"type": "freeform"</c>.
    /// </summary>
    /// <param name="Id">Идентификатор позиции в рамках агрегата.</param>
    /// <param name="Order">Порядковый номер позиции в рецепте.</param>
    /// <param name="Quantity">Количество.</param>
    /// <param name="MeasureUnitId">Идентификатор единицы измерения.</param>
    /// <param name="IsOptional">Признак опциональности.</param>
    /// <param name="PreparationNote">Заметка по подготовке. Опционально.</param>
    /// <param name="FreeformText">Свободный текст ингредиента (например, «укроп от соседки»).</param>
    public sealed record FreeformRecipeIngredientViewDto(
        Guid Id,
        int Order,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote,
        string FreeformText)
        : RecipeIngredientViewDto(Id, Order, Quantity, MeasureUnitId, IsOptional, PreparationNote);
}
