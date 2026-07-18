namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Настройки фоновых задач модуля Subscriptions.
    /// Читаются из секции <c>Subscriptions:Scheduler</c> конфигурации приложения.
    /// </summary>
    public sealed class SubscriptionSchedulerOptions
    {
        /// <summary>Имя секции конфигурации.</summary>
        public const string SECTION_NAME = "Subscriptions:Scheduler";

        /// <summary>
        /// Включён ли фоновый сборщик. Позволяет отключить его локально,
        /// не убирая регистрацию из DI.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Интервал между итерациями в минутах.
        /// </summary>
        /// <remarks>
        /// Значение по умолчанию намеренно крупное: истечение подписки не влияет
        /// на доступ к платным возможностям — гранты отсекаются по
        /// <c>CurrentPeriodEnd</c> независимо от статуса. Сборщик приводит в порядок
        /// сам статус, и здесь минуты роли не играют.
        /// </remarks>
        public int IntervalMinutes { get; set; } = 60;

        /// <summary>
        /// Максимальное количество подписок, обрабатываемых за одну итерацию.
        /// Остаток переносится на следующую.
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}
