namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Единица выхода готового продукта (используется в <c>Yield.YieldUnit</c>).
    /// Хранится как <c>int</c> в БД.
    /// </summary>
    public enum YieldUnit
    {
        /// <summary>Граммы.</summary>
        Grams = 0,

        /// <summary>Килограммы.</summary>
        Kilograms = 1,

        /// <summary>Миллилитры.</summary>
        Milliliters = 2,

        /// <summary>Литры.</summary>
        Liters = 3,

        /// <summary>Штуки.</summary>
        Pieces = 4,

        /// <summary>Порции.</summary>
        Servings = 5
    }
}
