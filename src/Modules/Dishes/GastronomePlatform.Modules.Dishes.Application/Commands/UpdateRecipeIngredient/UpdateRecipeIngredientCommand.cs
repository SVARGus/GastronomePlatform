using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipeIngredient
{
    /// <summary>
    /// Команда обновления существующей позиции рецепта (UC-DSH-031). Допускает
    /// смену источника catalog↔freeform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.
    /// </para>
    /// <para>
    /// Domain валидирует XOR <c>(IngredientId.HasValue) XOR (!string.IsNullOrWhiteSpace(FreeformText))</c>
    /// и требование «<c>IngredientSpecId</c> только при заполненном <c>IngredientId</c>».
    /// После успешного <c>Dish.UpdateRecipeIngredient</c> Handler вызывает
    /// <c>Dish.RecalculateDishMarkers</c> — состав мог измениться (смена ингредиента
    /// или переход catalog↔freeform), требуется пересчёт маркеров и автокоррекция
    /// диет-меток (ADR-0016).
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="RecipeIngredientId">Идентификатор обновляемой позиции рецепта.</param>
    /// <param name="IngredientId">Новый идентификатор ингредиента из справочника или <see langword="null"/>.</param>
    /// <param name="IngredientSpecId">Новый идентификатор спецификации или <see langword="null"/>.</param>
    /// <param name="FreeformText">Новый свободный текст или <see langword="null"/>.</param>
    /// <param name="Quantity">Новое количество. Строго положительное.</param>
    /// <param name="MeasureUnitId">Новый идентификатор единицы измерения.</param>
    /// <param name="IsOptional">Признак опциональности позиции.</param>
    /// <param name="PreparationNote">Новая заметка по подготовке. <see langword="null"/> — очистить.</param>
    public sealed record UpdateRecipeIngredientCommand(
        Guid DishId,
        Guid RecipeIngredientId,
        Guid? IngredientId,
        Guid? IngredientSpecId,
        string? FreeformText,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote) : ICommand;
}
