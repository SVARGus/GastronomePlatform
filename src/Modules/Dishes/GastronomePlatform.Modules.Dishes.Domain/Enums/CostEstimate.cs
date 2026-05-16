namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Грубая оценка стоимости блюда — для фильтрации в каталоге.
    /// Не привязано к точной цене ингредиентов; задаётся автором.
    /// Хранится как <c>int</c> в БД.
    /// </summary>
    public enum CostEstimate
    {
        /// <summary>Бюджетное.</summary>
        Budget = 0,

        /// <summary>Умеренное.</summary>
        Moderate = 1,

        /// <summary>Дорогое.</summary>
        Expensive = 2
    }
}
