namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Тип пищевого аллергена.
    /// Битовая маска: используется в <c>Dish.AllergensMask</c> как комбинация
    /// (сумма аллергенов блюда), и в <c>Ingredient.AllergenType</c> как одиночное значение.
    /// </summary>
    [Flags]
    public enum AllergenType
    {
        /// <summary>Нет аллергенов.</summary>
        None = 0,

        /// <summary>Глютен (пшеница, рожь, ячмень, овёс).</summary>
        Gluten = 1 << 0,

        /// <summary>Молочные продукты (лактоза, казеин).</summary>
        Dairy = 1 << 1,

        /// <summary>Яйца.</summary>
        Eggs = 1 << 2,

        /// <summary>Орехи (миндаль, грецкий, фундук, кешью).</summary>
        Nuts = 1 << 3,

        /// <summary>Арахис.</summary>
        Peanuts = 1 << 4,

        /// <summary>Рыба.</summary>
        Fish = 1 << 5,

        /// <summary>Моллюски и ракообразные.</summary>
        Shellfish = 1 << 6,

        /// <summary>Соя.</summary>
        Soy = 1 << 7,

        /// <summary>Кунжут.</summary>
        Sesame = 1 << 8,

        /// <summary>Горчица.</summary>
        Mustard = 1 << 9,

        /// <summary>Сельдерей.</summary>
        Celery = 1 << 10,

        /// <summary>Сульфиты (консерванты в вине, сухофруктах).</summary>
        Sulphites = 1 << 11
    }
}
