using System.Text.Json.Serialization;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Базовый тип позиции рецепта в публичном представлении (UC-DSH-052).
    /// Конкретный вариант определяется природой ингредиента: ссылка на справочник
    /// (<see cref="CatalogRecipeIngredientViewDto"/>) либо свободный текст
    /// (<see cref="FreeformRecipeIngredientViewDto"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Discriminated union по ADR-0012 и ADR-0014. На уровне JSON природа
    /// определяется полем-дискриминатором <c>type</c>: <c>"catalog"</c> или
    /// <c>"freeform"</c>. Поле подставляется автоматически
    /// <see cref="System.Text.Json.JsonSerializer"/> при сериализации значения,
    /// объявленного как <see cref="RecipeIngredientViewDto"/>.
    /// </para>
    /// <para>
    /// При добавлении новой природы (Этап 8+, например пользовательский справочник)
    /// нужно: добавить наследник, зарегистрировать его атрибутом
    /// <see cref="JsonDerivedTypeAttribute"/> здесь, дополнить write-side
    /// (Domain-фабрика, Add-команда) и сериализационный round-trip-тест.
    /// </para>
    /// </remarks>
    /// <param name="Id">Идентификатор позиции в рамках агрегата.</param>
    /// <param name="Order">Порядковый номер позиции в списке ингредиентов рецепта (1..N).</param>
    /// <param name="Quantity">Количество (строго положительное).</param>
    /// <param name="MeasureUnitId">Идентификатор единицы измерения. Имя резолвится клиентом
    /// через справочник единиц (UC-DSH-064).</param>
    /// <param name="IsOptional"><see langword="true"/>, если ингредиент опционален («по желанию»).</param>
    /// <param name="PreparationNote">Заметка по подготовке: «мелко нарезанный», «комнатной температуры». Опционально.</param>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(CatalogRecipeIngredientViewDto), "catalog")]
    [JsonDerivedType(typeof(FreeformRecipeIngredientViewDto), "freeform")]
    public abstract record RecipeIngredientViewDto(
        Guid Id,
        int Order,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote);
}
