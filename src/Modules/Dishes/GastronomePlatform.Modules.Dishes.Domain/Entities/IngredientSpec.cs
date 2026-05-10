using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Сорт/вид родительского ингредиента (например, «Молоко» → «3.2%», «2.5%», «Безлактозное»).
    /// На Этапе 2 — Stub-реализация: таблица создаётся, базовая Domain-модель есть,
    /// но в UI выбор сорта пока не предлагается.
    /// </summary>
    /// <remarks>
    /// Полноценное расширение (admin CRUD API, UI выбора сорта в рецепте,
    /// логика «КБЖУ из spec приоритетнее DefaultNutrition родителя») — Этап 8+.
    /// </remarks>
    public sealed class IngredientSpec : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Идентификатор родительского ингредиента.
        /// </summary>
        public Guid IngredientId { get; private set; }

        /// <summary>
        /// Название сорта/вида. Примеры: «Высший сорт», «3.2%», «Цельнозерновая».
        /// </summary>
        public string SpecName { get; private set; } = string.Empty;

        /// <summary>
        /// Идентификатор записи КБЖУ для этого сорта.
        /// Уникален в рамках всей таблицы (1:1 связь с <c>Nutrition</c>):
        /// у каждой spec свой собственный <c>Nutrition</c>-объект.
        /// </summary>
        public Guid NutritionId { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private IngredientSpec() : base() { }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="IngredientSpec"/>.
        /// Используется только из фабричного метода <see cref="Create"/>.
        /// </summary>
        /// <param name="ingredientId">Идентификатор родительского ингредиента.</param>
        /// <param name="specName">Название сорта/вида.</param>
        /// <param name="nutritionId">Идентификатор записи КБЖУ для этого сорта.</param>
        private IngredientSpec(Guid ingredientId, string specName, Guid nutritionId)
            : base(Guid.NewGuid())
        {
            IngredientId = ingredientId;
            SpecName = specName;
            NutritionId = nutritionId;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новый сорт ингредиента.
        /// Валидация наличия родительского <c>Ingredient</c> и связки с уникальной
        /// <c>Nutrition</c> ожидается на уровне команды.
        /// </summary>
        /// <param name="ingredientId">Идентификатор родительского ингредиента.</param>
        /// <param name="specName">Название сорта/вида.</param>
        /// <param name="nutritionId">Идентификатор записи КБЖУ для этого сорта.</param>
        /// <returns>Новый экземпляр <see cref="IngredientSpec"/>.</returns>
        public static IngredientSpec Create(Guid ingredientId, string specName, Guid nutritionId)
        {
            return new IngredientSpec(ingredientId, specName, nutritionId);
        }

        #endregion
    }
}
