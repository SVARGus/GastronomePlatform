using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddFreeformIngredientToRecipe
{
    /// <summary>
    /// Команда добавления ингредиента свободным текстом в рецепт блюда (UC-DSH-030, freeform-ветка).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Авторизация — POL-001 (автор или Admin). Проверка в Handler-е.
    /// </para>
    /// <para>
    /// Freeform-позиции не имеют справочной маски аллергенов и диет-конфликтов:
    /// после добавления Handler вызывает <c>Dish.RecalculateDishMarkers</c>, которое
    /// поднимет флаг <c>HasUnverifiedAllergens</c> (наличие непроверенной позиции).
    /// Сами диет-метки блюда из-за freeform-позиции не автокорректируются (ADR-0016).
    /// </para>
    /// </remarks>
    /// <param name="DishId">Идентификатор блюда.</param>
    /// <param name="FreeformText">Свободный текст ингредиента (1..200 символов).</param>
    /// <param name="Quantity">Количество. Строго положительное.</param>
    /// <param name="MeasureUnitId">Идентификатор единицы измерения.</param>
    /// <param name="IsOptional">Признак опциональности позиции.</param>
    /// <param name="PreparationNote">Заметка по подготовке. Опционально.</param>
    public sealed record AddFreeformIngredientToRecipeCommand(
        Guid DishId,
        string FreeformText,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote) : ICommand<AddFreeformIngredientToRecipeResult>;
}
