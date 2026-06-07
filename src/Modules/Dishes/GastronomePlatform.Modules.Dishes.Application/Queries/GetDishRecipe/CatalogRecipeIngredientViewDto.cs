namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Позиция рецепта со ссылкой на справочник <c>Ingredient</c> (catalog-природа).
    /// Дискриминатор JSON: <c>"type": "catalog"</c>.
    /// </summary>
    /// <param name="Id">Идентификатор позиции в рамках агрегата.</param>
    /// <param name="Order">Порядковый номер позиции в рецепте.</param>
    /// <param name="Quantity">Количество.</param>
    /// <param name="MeasureUnitId">Идентификатор единицы измерения.</param>
    /// <param name="IsOptional">Признак опциональности.</param>
    /// <param name="PreparationNote">Заметка по подготовке. Опционально.</param>
    /// <param name="IngredientId">Идентификатор ингредиента из справочника.
    /// Карточка ингредиента — через UC-DSH-063.</param>
    /// <param name="IngredientSpecId">Идентификатор спецификации (сорта). Опционально.</param>
    public sealed record CatalogRecipeIngredientViewDto(
        Guid Id,
        int Order,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote,
        Guid IngredientId,
        Guid? IngredientSpecId)
        : RecipeIngredientViewDto(Id, Order, Quantity, MeasureUnitId, IsOptional, PreparationNote);
}
