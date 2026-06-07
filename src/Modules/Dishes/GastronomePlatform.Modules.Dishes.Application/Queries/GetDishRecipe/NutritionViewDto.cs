using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Пищевая ценность блюда в публичном представлении рецепта (UC-DSH-052).
    /// </summary>
    /// <param name="CalcMethod">Способ расчёта КБЖУ: на 100 г или на порцию.</param>
    /// <param name="Calories">Калорийность, ккал.</param>
    /// <param name="Proteins">Белки, г.</param>
    /// <param name="Fats">Жиры, г.</param>
    /// <param name="SaturatedFats">Насыщенные жиры, г. Опционально.</param>
    /// <param name="Carbs">Углеводы, г.</param>
    /// <param name="Sugar">Сахара, г. Опционально.</param>
    /// <param name="Fiber">Клетчатка, г. Опционально.</param>
    /// <param name="Salt">Соль, г. Опционально.</param>
    public sealed record NutritionViewDto(
        NutritionCalcMethod CalcMethod,
        decimal Calories,
        decimal Proteins,
        decimal Fats,
        decimal? SaturatedFats,
        decimal Carbs,
        decimal? Sugar,
        decimal? Fiber,
        decimal? Salt);
}
