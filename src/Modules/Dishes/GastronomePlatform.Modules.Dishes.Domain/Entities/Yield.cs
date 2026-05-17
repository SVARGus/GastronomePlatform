using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Выход готового продукта рецепта и размер порции.
    /// Часть агрегата <see cref="Dish"/>, 1:1 с <see cref="Recipe"/>.
    /// </summary>
    /// <remarks>
    /// Все методы изменения состояния — <see langword="internal"/>.
    /// Внешний код управляет выходом через wrapper-метод <c>Dish.UpdateYield(...)</c>.
    /// </remarks>
    public sealed class Yield : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Идентификатор рецепта-владельца. FK на <c>dishes.Recipes</c>.
        /// </summary>
        public Guid RecipeId { get; private set; }

        /// <summary>
        /// Общее количество готового продукта в единицах <see cref="YieldUnit"/>.
        /// По умолчанию — 0.
        /// </summary>
        public decimal QuantityTotal { get; private set; }

        /// <summary>
        /// Единица выхода готового продукта. По умолчанию — <see cref="Enums.YieldUnit.Servings"/>.
        /// </summary>
        public YieldUnit YieldUnit { get; private set; }

        /// <summary>
        /// Количество порций. По умолчанию — 1.
        /// </summary>
        public int ServingsCount { get; private set; }

        /// <summary>
        /// Вес одной порции в граммах. Критично для расчёта КБЖУ на порцию.
        /// Опционально.
        /// </summary>
        public decimal? GramsPerServing { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private Yield() : base() { }

        /// <summary>
        /// Приватный конструктор, используется только из <see cref="CreateForRecipe"/>.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        private Yield(Guid recipeId) : base(Guid.NewGuid())
        {
            RecipeId = recipeId;
            QuantityTotal = 0m;
            YieldUnit = YieldUnit.Servings;
            ServingsCount = 1;
            GramsPerServing = null;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт Yield с дефолтами. Вызывается только из <c>Recipe.CreateForDish</c>.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        /// <returns>
        /// Новый <see cref="Yield"/> с <see cref="QuantityTotal"/> = 0,
        /// <see cref="YieldUnit"/> = <see cref="Enums.YieldUnit.Servings"/>,
        /// <see cref="ServingsCount"/> = 1, <see cref="GramsPerServing"/> = <see langword="null"/>.
        /// </returns>
        internal static Yield CreateForRecipe(Guid recipeId)
        {
            return new Yield(recipeId);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет значения выхода. <paramref name="servingsCount"/> должно быть не меньше 1,
        /// <paramref name="quantityTotal"/> — неотрицательным, <paramref name="gramsPerServing"/>
        /// (если задан) — неотрицательным.
        /// </summary>
        /// <param name="quantityTotal">Общее количество готового продукта.</param>
        /// <param name="yieldUnit">Единица выхода.</param>
        /// <param name="servingsCount">Количество порций.</param>
        /// <param name="gramsPerServing">Вес одной порции в граммах. <see langword="null"/> — не задано.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="Result.Failure(Error)"/> с <see cref="DishesErrors.InvalidYield"/>,
        /// если инварианты нарушены.
        /// </returns>
        internal Result Update(
            decimal quantityTotal,
            YieldUnit yieldUnit,
            int servingsCount,
            decimal? gramsPerServing)
        {
            if (quantityTotal < 0m || servingsCount < 1 || gramsPerServing is < 0m)
            {
                return Result.Failure(DishesErrors.InvalidYield);
            }

            QuantityTotal = quantityTotal;
            YieldUnit = yieldUnit;
            ServingsCount = servingsCount;
            GramsPerServing = gramsPerServing;

            return Result.Success();
        }

        #endregion
    }
}
