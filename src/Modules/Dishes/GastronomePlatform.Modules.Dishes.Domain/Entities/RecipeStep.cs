using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Errors;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Один шаг приготовления рецепта. Часть агрегата <see cref="Dish"/>,
    /// принадлежит <see cref="Recipe"/>.
    /// </summary>
    /// <remarks>
    /// Все методы изменения состояния — <see langword="internal"/>. Внешний код
    /// управляет шагами через wrapper-методы на корне агрегата <see cref="Dish"/>:
    /// <c>AddRecipeStep</c>, <c>UpdateRecipeStep</c>, <c>RemoveRecipeStep</c>,
    /// <c>ReorderRecipeSteps</c>.
    /// </remarks>
    public sealed class RecipeStep : Entity<Guid>
    {
        #region Limits

        /// <summary>Минимальная длина <see cref="Description"/>.</summary>
        public const int MIN_DESCRIPTION_LENGTH = 10;

        /// <summary>Максимальная длина <see cref="Description"/>.</summary>
        public const int MAX_DESCRIPTION_LENGTH = 4000;

        /// <summary>Максимальная длина <see cref="Title"/>.</summary>
        public const int MAX_TITLE_LENGTH = 200;

        /// <summary>Максимальная длина <see cref="VideoUrl"/>.</summary>
        public const int MAX_VIDEO_URL_LENGTH = 500;

        /// <summary>Минимальное значение <see cref="TemperatureCelsius"/> в °C.</summary>
        public const int MIN_TEMPERATURE_CELSIUS = -30;

        /// <summary>Максимальное значение <see cref="TemperatureCelsius"/> в °C.</summary>
        public const int MAX_TEMPERATURE_CELSIUS = 300;

        /// <summary>Минимальное значение <see cref="TimerMinutes"/>.</summary>
        public const int MIN_TIMER_MINUTES = 1;

        /// <summary>Максимальное значение <see cref="TimerMinutes"/> (24 часа).</summary>
        public const int MAX_TIMER_MINUTES = 1440;

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор рецепта-владельца. FK на <c>dishes.Recipes</c>.
        /// </summary>
        public Guid RecipeId { get; private set; }

        /// <summary>
        /// Порядковый номер шага в рамках одного <see cref="Recipe"/>. Уникален.
        /// Назначается автоматически при добавлении и пересчитывается при удалении
        /// и переупорядочивании.
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// Короткий заголовок шага: «Варим бульон», «Делаем подливу». Опционально.
        /// </summary>
        public string? Title { get; private set; }

        /// <summary>
        /// Основной текст шага. Длина 10–4000 символов
        /// (валидация — на уровне команды через FluentValidation).
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Идентификатор иллюстрации шага в модуле Media. Логическая ссылка
        /// на <c>media.MediaFiles</c> без FK-constraint (кросс-модульно). Опционально.
        /// </summary>
        public Guid? ImageMediaId { get; private set; }

        /// <summary>
        /// URL внешнего видео (YouTube, VK или другой плеер). Опционально.
        /// </summary>
        public string? VideoUrl { get; private set; }

        /// <summary>
        /// Температура приготовления в градусах Цельсия. Диапазон −30…300.
        /// Опционально.
        /// </summary>
        public int? TemperatureCelsius { get; private set; }

        /// <summary>
        /// Время для UI-таймера в минутах. Диапазон 1…1440 (до 24 часов).
        /// Опционально.
        /// </summary>
        public int? TimerMinutes { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// </summary>
        private RecipeStep() : base() { }

        /// <summary>
        /// Приватный конструктор, используется только из <see cref="CreateForRecipe"/>.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        /// <param name="order">Порядковый номер шага.</param>
        /// <param name="description">Основной текст шага.</param>
        /// <param name="title">Короткий заголовок. Опционально.</param>
        /// <param name="imageMediaId">Идентификатор иллюстрации в Media. Опционально.</param>
        /// <param name="videoUrl">URL внешнего видео. Опционально.</param>
        /// <param name="temperatureCelsius">Температура приготовления. Опционально.</param>
        /// <param name="timerMinutes">Время для таймера в минутах. Опционально.</param>
        private RecipeStep(
            Guid recipeId,
            int order,
            string description,
            string? title,
            Guid? imageMediaId,
            string? videoUrl,
            int? temperatureCelsius,
            int? timerMinutes)
            : base(Guid.NewGuid())
        {
            RecipeId = recipeId;
            Order = order;
            Description = description;
            Title = title;
            ImageMediaId = imageMediaId;
            VideoUrl = videoUrl;
            TemperatureCelsius = temperatureCelsius;
            TimerMinutes = timerMinutes;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новый шаг рецепта. Вызывается только из <c>Recipe.AddStep(...)</c>.
        /// Валидация длины <paramref name="description"/> и диапазонов температуры/таймера
        /// ожидается на уровне команды.
        /// </summary>
        /// <param name="recipeId">Идентификатор рецепта-владельца.</param>
        /// <param name="order">Порядковый номер шага в рамках рецепта.</param>
        /// <param name="description">Основной текст шага.</param>
        /// <param name="title">Короткий заголовок. Опционально.</param>
        /// <param name="imageMediaId">Идентификатор иллюстрации в Media. Опционально.</param>
        /// <param name="videoUrl">URL внешнего видео. Опционально.</param>
        /// <param name="temperatureCelsius">Температура приготовления. Опционально.</param>
        /// <param name="timerMinutes">Время для таймера в минутах. Опционально.</param>
        /// <returns>Новый <see cref="RecipeStep"/>.</returns>
        internal static RecipeStep CreateForRecipe(
            Guid recipeId,
            int order,
            string description,
            string? title,
            Guid? imageMediaId,
            string? videoUrl,
            int? temperatureCelsius,
            int? timerMinutes)
        {
            return new RecipeStep(
                recipeId,
                order,
                description,
                title,
                imageMediaId,
                videoUrl,
                temperatureCelsius,
                timerMinutes);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет все поля шага одним вызовом (соответствует UC-DSH-021).
        /// Валидирует диапазоны <paramref name="temperatureCelsius"/>
        /// и <paramref name="timerMinutes"/>. Валидация длины
        /// <paramref name="description"/> ожидается на уровне команды.
        /// </summary>
        /// <param name="description">Основной текст шага.</param>
        /// <param name="title">Короткий заголовок. <see langword="null"/> — очистить.</param>
        /// <param name="imageMediaId">Идентификатор иллюстрации в Media. <see langword="null"/> — очистить.</param>
        /// <param name="videoUrl">URL внешнего видео. <see langword="null"/> — очистить.</param>
        /// <param name="temperatureCelsius">Температура приготовления. <see langword="null"/> — очистить.</param>
        /// <param name="timerMinutes">Время для таймера в минутах. <see langword="null"/> — очистить.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, либо <see cref="Result.Failure(Error)"/> с
        /// <see cref="DishesErrors.InvalidTemperature"/> или
        /// <see cref="DishesErrors.InvalidTimerMinutes"/>, если значения вне диапазона.
        /// </returns>
        internal Result Update(
            string description,
            string? title,
            Guid? imageMediaId,
            string? videoUrl,
            int? temperatureCelsius,
            int? timerMinutes)
        {
            if (temperatureCelsius is < MIN_TEMPERATURE_CELSIUS or > MAX_TEMPERATURE_CELSIUS)
            {
                return Result.Failure(DishesErrors.InvalidTemperature);
            }

            if (timerMinutes is < MIN_TIMER_MINUTES or > MAX_TIMER_MINUTES)
            {
                return Result.Failure(DishesErrors.InvalidTimerMinutes);
            }

            Description = description;
            Title = title;
            ImageMediaId = imageMediaId;
            VideoUrl = videoUrl;
            TemperatureCelsius = temperatureCelsius;
            TimerMinutes = timerMinutes;

            return Result.Success();
        }

        /// <summary>
        /// Устанавливает порядковый номер шага. Вызывается только из
        /// <c>Recipe.RemoveStep</c> и <c>Recipe.ReorderSteps</c> при пересборке порядка.
        /// </summary>
        /// <param name="order">Новый порядковый номер.</param>
        internal void SetOrder(int order)
        {
            Order = order;
        }

        #endregion
    }
}
