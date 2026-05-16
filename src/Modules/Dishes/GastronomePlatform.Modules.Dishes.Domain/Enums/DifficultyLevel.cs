namespace GastronomePlatform.Modules.Dishes.Domain.Enums
{
    /// <summary>
    /// Уровень сложности приготовления блюда.
    /// Используется в фильтрах каталога и в карточке блюда.
    /// Хранится как <c>int</c> в БД.
    /// </summary>
    public enum DifficultyLevel
    {
        /// <summary>Легко.</summary>
        Easy = 0,

        /// <summary>Средне.</summary>
        Medium = 1,

        /// <summary>Сложно.</summary>
        Hard = 2,

        /// <summary>Профессиональный уровень.</summary>
        Pro = 3
    }
}
