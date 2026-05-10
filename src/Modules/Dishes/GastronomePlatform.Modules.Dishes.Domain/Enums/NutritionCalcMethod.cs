namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Способ расчёта пищевой ценности (КБЖУ).
    /// Определяет, к какому количеству продукта отнесены значения в записи Nutrition.
    /// </summary>
    public enum NutritionCalcMethod
    {
        /// <summary>КБЖУ указаны на 100 граммов готового продукта.</summary>
        Per100g = 0,

        /// <summary>КБЖУ указаны на одну порцию.</summary>
        PerServing = 1
    }
}
