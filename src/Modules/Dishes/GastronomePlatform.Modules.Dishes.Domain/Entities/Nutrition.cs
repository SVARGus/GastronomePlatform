using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Пищевая ценность (КБЖУ) — вспомогательная сущность.
    /// Используется в <c>Recipe</c> (КБЖУ блюда),
    /// <c>Ingredient</c> (базовые КБЖУ продукта) и <c>IngredientSpec</c> (КБЖУ сорта).
    /// </summary>
    /// <remarks>
    /// Инварианты «значения &gt;= 0», «<see cref="SaturatedFats"/> &lt;= <see cref="Fats"/>»
    /// и «<see cref="Sugar"/> &lt;= <see cref="Carbs"/>» проверяются на уровне команды
    /// (FluentValidation), не в Domain.
    /// </remarks>
    public sealed class Nutrition : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Способ расчёта КБЖУ: на 100 г или на порцию.
        /// </summary>
        public NutritionCalcMethod CalcMethod { get; private set; }

        /// <summary>Калорийность, ккал.</summary>
        public decimal Calories { get; private set; }

        /// <summary>Белки, г.</summary>
        public decimal Proteins { get; private set; }

        /// <summary>Жиры, г.</summary>
        public decimal Fats { get; private set; }

        /// <summary>
        /// Насыщенные жиры, г. Опционально.
        /// При заполнении должно соблюдаться условие
        /// <see cref="SaturatedFats"/> &lt;= <see cref="Fats"/>.
        /// </summary>
        public decimal? SaturatedFats { get; private set; }

        /// <summary>Углеводы, г.</summary>
        public decimal Carbs { get; private set; }

        /// <summary>
        /// Сахара, г. Опционально.
        /// При заполнении должно соблюдаться условие
        /// <see cref="Sugar"/> &lt;= <see cref="Carbs"/>.
        /// </summary>
        public decimal? Sugar { get; private set; }

        /// <summary>Клетчатка, г. Опционально.</summary>
        public decimal? Fiber { get; private set; }

        /// <summary>Соль, г. Опционально. Важно для определённых диет.</summary>
        public decimal? Salt { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private Nutrition() : base() { }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="Nutrition"/>.
        /// Используется только из фабричного метода <see cref="Create"/>.
        /// </summary>
        /// <param name="calcMethod">Способ расчёта КБЖУ.</param>
        /// <param name="calories">Калорийность, ккал.</param>
        /// <param name="proteins">Белки, г.</param>
        /// <param name="fats">Жиры, г.</param>
        /// <param name="saturatedFats">Насыщенные жиры, г. Опционально.</param>
        /// <param name="carbs">Углеводы, г.</param>
        /// <param name="sugar">Сахара, г. Опционально.</param>
        /// <param name="fiber">Клетчатка, г. Опционально.</param>
        /// <param name="salt">Соль, г. Опционально.</param>
        private Nutrition(
            NutritionCalcMethod calcMethod,
            decimal calories,
            decimal proteins,
            decimal fats,
            decimal? saturatedFats,
            decimal carbs,
            decimal? sugar,
            decimal? fiber,
            decimal? salt)
            : base(Guid.NewGuid())
        {
            CalcMethod = calcMethod;
            Calories = calories;
            Proteins = proteins;
            Fats = fats;
            SaturatedFats = saturatedFats;
            Carbs = carbs;
            Sugar = sugar;
            Fiber = fiber;
            Salt = salt;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новую запись пищевой ценности.
        /// Валидация (неотрицательность значений, согласованность
        /// <c>Sugar &lt;= Carbs</c> и <c>SaturatedFats &lt;= Fats</c>)
        /// ожидается на уровне команды.
        /// </summary>
        /// <param name="calcMethod">Способ расчёта КБЖУ.</param>
        /// <param name="calories">Калорийность, ккал.</param>
        /// <param name="proteins">Белки, г.</param>
        /// <param name="fats">Жиры, г.</param>
        /// <param name="saturatedFats">Насыщенные жиры, г. Опционально.</param>
        /// <param name="carbs">Углеводы, г.</param>
        /// <param name="sugar">Сахара, г. Опционально.</param>
        /// <param name="fiber">Клетчатка, г. Опционально.</param>
        /// <param name="salt">Соль, г. Опционально.</param>
        /// <returns>Новый экземпляр <see cref="Nutrition"/>.</returns>
        public static Nutrition Create(
            NutritionCalcMethod calcMethod,
            decimal calories,
            decimal proteins,
            decimal fats,
            decimal? saturatedFats,
            decimal carbs,
            decimal? sugar,
            decimal? fiber,
            decimal? salt)
        {
            return new Nutrition(calcMethod, calories, proteins, fats, saturatedFats, carbs, sugar, fiber, salt);
        }

        #endregion
    }
}
