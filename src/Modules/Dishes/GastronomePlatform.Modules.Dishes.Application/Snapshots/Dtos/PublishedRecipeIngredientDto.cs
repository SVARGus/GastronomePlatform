namespace GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos
{
    /// <summary>
    /// Базовый тип позиции рецепта в jsonb-снепшоте. Конкретный вариант
    /// определяется природой ингредиента: ссылка на справочник
    /// (<see cref="PublishedCatalogIngredientDto"/>) либо свободный текст
    /// (<see cref="PublishedFreeformIngredientDto"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Discriminated union по ADR-0012 и ADR-0014. На уровне JSON природа
    /// определяется полем-дискриминатором <c>type</c>: <c>"catalog"</c> или
    /// <c>"freeform"</c>. Полиморфизм реализован через
    /// <see cref="PublishedRecipeIngredientDtoConverter"/>, а не атрибутами
    /// <c>JsonPolymorphic</c>/<c>JsonDerivedType</c>: PostgreSQL канонизирует
    /// <c>jsonb</c> и пересортировывает ключи, из-за чего <c>type</c> перестаёт
    /// быть первым свойством, а атрибутный полиморфизм .NET 8 требует его
    /// строго первым при чтении.
    /// </para>
    /// <para>
    /// При добавлении новой природы (Этап 8+, например пользовательский справочник)
    /// нужно: добавить наследник, дополнить обе ветки
    /// <see cref="PublishedRecipeIngredientDtoConverter"/> (Read и Write),
    /// write-side (Domain-фабрика, Add-команда) и сериализационный round-trip-тест
    /// с эмуляцией канонизации ключей.
    /// </para>
    /// </remarks>
    /// <param name="Id">Идентификатор позиции в рамках агрегата.</param>
    /// <param name="Order">Порядковый номер позиции в списке ингредиентов рецепта (1..N).</param>
    /// <param name="Quantity">Количество (строго положительное).</param>
    /// <param name="MeasureUnitId">Идентификатор единицы измерения. Имя единицы в MVP-формате
    /// не денормализуется — резолвится потребителем при чтении снепшота.</param>
    /// <param name="IsOptional"><see langword="true"/>, если ингредиент опционален («по желанию»).</param>
    /// <param name="PreparationNote">Заметка по подготовке: «мелко нарезанный», «комнатной температуры». Опционально.</param>
    public abstract record PublishedRecipeIngredientDto(
        Guid Id,
        int Order,
        decimal Quantity,
        Guid MeasureUnitId,
        bool IsOptional,
        string? PreparationNote);
}
