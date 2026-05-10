namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Тип единицы измерения. Определяет правила конвертации.
    /// Конвертация возможна только внутри одного типа; масса ↔ объём — лишь
    /// через известную плотность ингредиента (<c>Ingredient.DensityApprox</c>).
    /// </summary>
    public enum MeasureUnitType
    {
        /// <summary>Масса: г, кг</summary>
        Mass = 0,

        /// <summary>Объём: мл, л, ст.л, ч.л, стакан</summary>
        Volume = 1,

        /// <summary>Количество: шт</summary>
        Count = 2,

        /// <summary>Щепотка — неконвертируемая единица.</summary>
        Pinch = 3
    }
}
