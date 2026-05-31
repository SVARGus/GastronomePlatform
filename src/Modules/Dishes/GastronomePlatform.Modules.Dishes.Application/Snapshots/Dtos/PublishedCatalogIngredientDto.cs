namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
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
    /// <param name="IngredientId">Идентификатор ингредиента из справочника.</param>
    /// <param name="IngredientSpecId">Идентификатор спецификации (сорта). Опционально.</param>
    public sealed record PublishedCatalogIngredientDto(
        Guid Id,
        int Order,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote,
        Guid IngredientId,
        Guid? IngredientSpecId)
        : PublishedRecipeIngredientDto(Id, Order, Quantity, MeasureUnitId, IsOptional, PreparationNote);
}
