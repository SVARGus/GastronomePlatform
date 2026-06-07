using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Выход готового продукта и размер порции в публичном представлении рецепта (UC-DSH-052).
    /// </summary>
    /// <param name="QuantityTotal">Общее количество готового продукта в единицах <paramref name="YieldUnit"/>.</param>
    /// <param name="YieldUnit">Единица выхода.</param>
    /// <param name="ServingsCount">Количество порций (≥ 1).</param>
    /// <param name="GramsPerServing">Вес одной порции в граммах. <see langword="null"/>, если не задан.</param>
    public sealed record YieldViewDto(
        decimal QuantityTotal,
        YieldUnit YieldUnit,
        int ServingsCount,
        decimal? GramsPerServing);
}
