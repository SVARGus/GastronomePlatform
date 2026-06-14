namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetScaledRecipeIngredients
{
    /// <summary>
    /// Позиция рецепта с пересчитанным на запрошенное число порций количеством
    /// (UC-DSH-056 RecalculateIngredients).
    /// </summary>
    /// <remarks>
    /// Плоский discriminated-DTO: поле <see cref="Type"/> = <c>"catalog"</c> или
    /// <c>"freeform"</c>; набор заполненных доп.полей зависит от значения. Это удобнее
    /// для одной коллекции на UI, чем nested-полиморфия.
    /// </remarks>
    /// <param name="Id">Идентификатор позиции (как в snapshot).</param>
    /// <param name="Order">Порядковый номер позиции в рецепте.</param>
    /// <param name="Type">Дискриминатор природы: <c>"catalog"</c> или <c>"freeform"</c>.</param>
    /// <param name="IngredientId">Идентификатор ингредиента из справочника. Заполнен для catalog.</param>
    /// <param name="IngredientSpecId">Идентификатор спецификации (сорта). Опционально для catalog.</param>
    /// <param name="FreeformText">Свободный текст позиции. Заполнен для freeform.</param>
    /// <param name="OriginalQuantity">Исходное количество (на <c>ServingsDefault</c> порций).</param>
    /// <param name="ScaledQuantity">Пересчитанное количество (на <c>ServingsRequested</c> порций).</param>
    /// <param name="MeasureUnitId">Идентификатор единицы измерения (не меняется при пересчёте).</param>
    /// <param name="IsOptional">Признак опциональности.</param>
    /// <param name="PreparationNote">Заметка по подготовке. Опционально.</param>
    public sealed record ScaledIngredientDto(
        Guid Id,
        int Order,
        string Type,
        Guid? IngredientId,
        Guid? IngredientSpecId,
        string? FreeformText,
        decimal OriginalQuantity,
        decimal ScaledQuantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote);
}
