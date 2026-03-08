namespace GastronomePlatform.Common.Application.Abstractions
{
    /// <summary>
    /// Абстракция над системным временем.
    /// Позволяет подменять время в тестах.
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Текущее время в UTC с информацией о смещении.
        /// </summary>
        DateTimeOffset UtcNow { get; }

        /// <summary>
        /// Текущая дата (без времени).
        /// </summary>
        DateTime Today { get; }
    }
}
