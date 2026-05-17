using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Errors;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Времена этапов приготовления рецепта. Часть агрегата <see cref="Dish"/>,
    /// 1:1 с <see cref="Recipe"/>.
    /// </summary>
    /// <remarks>
    /// Все методы изменения состояния — <see langword="internal"/>.
    /// Внешний код управляет временами через wrapper-метод <c>Dish.UpdateTiming(...)</c>.
    /// </remarks>
    public sealed class Timing : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Идентификатор рецепта-владельца. FK на <c>dishes.Recipes</c>.
        /// </summary>
        public Guid RecipeId { get; private set; }

        /// <summary>
        /// Время подготовки (порезать, замариновать, достать из холодильника). Опционально.
        /// </summary>
        public int? PrepTimeMinutes { get; private set; }

        /// <summary>
        /// Время основного приготовления (варка, жарка, запекание). Опционально.
        /// </summary>
        public int? CookTimeMinutes { get; private set; }

        /// <summary>
        /// Время отдыха (тесто подошло, мясо «отдохнуло» после жарки). Опционально.
        /// </summary>
        public int? RestTimeMinutes { get; private set; }

        /// <summary>
        /// Сколько повар активно участвует в процессе. Подмножество общего времени,
        /// не включается в сумму <see cref="TotalTimeMinutes"/>. Опционально.
        /// </summary>
        public int? ActiveTimeMinutes { get; private set; }

        /// <summary>
        /// Общее время. Единственное обязательное поле. По умолчанию — 0.
        /// </summary>
        public int TotalTimeMinutes { get; private set; }

        /// <summary>
        /// <see langword="true"/> — общее время заполнено автором вручную;
        /// <see langword="false"/> — рассчитано как сумма Prep + Cook + Rest.
        /// По умолчанию — <see langword="true"/>.
        /// </summary>
        public bool IsTotalManual { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private Timing() : base() { }

        /// <summary>
        /// Приватный конструктор, используется только из <see cref="CreateForRecipe"/>.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        private Timing(Guid recipeId) : base(Guid.NewGuid())
        {
            RecipeId = recipeId;
            TotalTimeMinutes = 0;
            IsTotalManual = true;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт пустой Timing с дефолтами. Вызывается только из <c>Recipe.CreateForDish</c>.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        /// <returns>Новый <see cref="Timing"/> с <see cref="TotalTimeMinutes"/> = 0 и <see cref="IsTotalManual"/> = <see langword="true"/>.</returns>
        internal static Timing CreateForRecipe(Guid recipeId)
        {
            return new Timing(recipeId);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет времена. Если <paramref name="isTotalManual"/> = <see langword="false"/>,
        /// общее время вычисляется как (<paramref name="prepTimeMinutes"/> ?? 0) +
        /// (<paramref name="cookTimeMinutes"/> ?? 0) + (<paramref name="restTimeMinutes"/> ?? 0),
        /// игнорируя <paramref name="totalTimeMinutes"/>.
        /// <para>
        /// <paramref name="activeTimeMinutes"/> в сумму не входит — это отдельная метрика
        /// «сколько повар активно занят».
        /// </para>
        /// </summary>
        /// <param name="prepTimeMinutes">Время подготовки в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="cookTimeMinutes">Время основного приготовления в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="restTimeMinutes">Время отдыха в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="activeTimeMinutes">Время активной работы повара в минутах. <see langword="null"/> — не задано.</param>
        /// <param name="totalTimeMinutes">Общее время в минутах. Используется, только если <paramref name="isTotalManual"/> = <see langword="true"/>.</param>
        /// <param name="isTotalManual">
        /// <see langword="true"/> — общее время задано вручную и сохраняется как есть;
        /// <see langword="false"/> — вычисляется автоматически из prep + cook + rest.
        /// </param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="Result.Failure(Error)"/> с <see cref="DishesErrors.InvalidTiming"/>,
        /// если хотя бы одно значение отрицательно.
        /// </returns>
        internal Result UpdateTimes(
            int? prepTimeMinutes,
            int? cookTimeMinutes,
            int? restTimeMinutes,
            int? activeTimeMinutes,
            int totalTimeMinutes,
            bool isTotalManual)
        {
            if (prepTimeMinutes is < 0
                || cookTimeMinutes is < 0
                || restTimeMinutes is < 0
                || activeTimeMinutes is < 0
                || totalTimeMinutes < 0)
            {
                return Result.Failure(DishesErrors.InvalidTiming);
            }

            PrepTimeMinutes = prepTimeMinutes;
            CookTimeMinutes = cookTimeMinutes;
            RestTimeMinutes = restTimeMinutes;
            ActiveTimeMinutes = activeTimeMinutes;
            IsTotalManual = isTotalManual;

            TotalTimeMinutes = isTotalManual
                ? totalTimeMinutes
                : (prepTimeMinutes ?? 0) + (cookTimeMinutes ?? 0) + (restTimeMinutes ?? 0);

            return Result.Success();
        }

        #endregion
    }
}
