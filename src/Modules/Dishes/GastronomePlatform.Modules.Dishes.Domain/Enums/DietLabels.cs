namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Диетические метки блюда. Битовая маска: одно блюдо может иметь несколько меток
    /// одновременно (например, <c>Vegan | GlutenFree</c>).
    /// Хранится как <c>int</c> в БД (поле <c>Dish.DietLabelsMask</c>).
    /// Используется для фильтрации в каталоге.
    /// </summary>
    [Flags]
    public enum DietLabels
    {
        /// <summary>Без диетических меток.</summary>
        None = 0,

        /// <summary>Вегетарианское (без мяса и рыбы).</summary>
        Vegetarian = 1 << 0,    // 1

        /// <summary>Веганское (без любых продуктов животного происхождения).</summary>
        Vegan = 1 << 1,    // 2

        /// <summary>Без глютена.</summary>
        GlutenFree = 1 << 2,    // 4

        /// <summary>Без лактозы.</summary>
        LactoseFree = 1 << 3,    // 8

        /// <summary>Халяль.</summary>
        Halal = 1 << 4,    // 16

        /// <summary>Кошер.</summary>
        Kosher = 1 << 5,    // 32

        /// <summary>Подходит для кето-диеты.</summary>
        KetoFriendly = 1 << 6,    // 64

        /// <summary>Низкоуглеводное.</summary>
        LowCarb = 1 << 7,    // 128

        /// <summary>Низкокалорийное.</summary>
        LowCalorie = 1 << 8,    // 256

        /// <summary>Без сахара.</summary>
        SugarFree = 1 << 9    // 512
    }
}
