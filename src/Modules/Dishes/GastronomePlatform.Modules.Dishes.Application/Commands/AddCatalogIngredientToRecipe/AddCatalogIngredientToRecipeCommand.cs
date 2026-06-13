using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddCatalogIngredientToRecipe
{
    /// <summary>
    /// Команда добавления ингредиента из справочника в рецепт блюда (UC-DSH-030, catalog-ветка).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.
    /// </para>
    /// <para>
    /// После успешного добавления Handler вызывает <c>Dish.RecalculateDishMarkers</c>,
    /// чтобы пересчитать <c>AllergensMask</c>, <c>HasUnverifiedAllergens</c>
    /// и автокорректировать <c>DietLabelsMask</c> (ADR-0016).
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="IngredientId">Идентификатор ингредиента из справочника.</param>
    /// <param name="IngredientSpecId">Идентификатор спецификации (сорта). Опционально.</param>
    /// <param name="Quantity">Количество. Строго положительное.</param>
    /// <param name="MeasureUnitId">Идентификатор единицы измерения.</param>
    /// <param name="IsOptional">Признак опциональности позиции.</param>
    /// <param name="PreparationNote">Заметка по подготовке. Опционально.</param>
    public sealed record AddCatalogIngredientToRecipeCommand(
        Guid DishId,
        Guid IngredientId,
        Guid? IngredientSpecId,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote) : ICommand<AddCatalogIngredientToRecipeResult>;
}
