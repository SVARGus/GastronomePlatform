using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.ExpireDueSubscriptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Фоновый сборщик истёкших подписок (UC-SUB-203). С периодичностью
    /// <see cref="SubscriptionSchedulerOptions.IntervalMinutes"/> отправляет
    /// <see cref="ExpireDueSubscriptionsCommand"/>, переводящую подписки
    /// с законченным оплаченным периодом в статус <c>Expired</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Сам сервис — Singleton, а репозитории и <c>DbContext</c> зарегистрированы
    /// Scoped, поэтому на каждую итерацию создаётся отдельный scope. Держать
    /// <c>DbContext</c> на всё время жизни приложения нельзя: он накапливал бы
    /// отслеживаемые сущности и не переживал бы обрыв соединения.
    /// </para>
    /// <para>
    /// Первая итерация выполняется сразу при старте, дальше — по таймеру.
    /// Ошибка внутри итерации логируется и не гасит сервис: одна неудачная попытка
    /// (недоступная БД, неприменённая миграция) не должна лишать приложение
    /// сборщика до перезапуска.
    /// </para>
    /// </remarks>
    public sealed class SubscriptionExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SubscriptionSchedulerOptions _options;
        private readonly ILogger<SubscriptionExpirationBackgroundService> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionExpirationBackgroundService"/>.
        /// </summary>
        /// <param name="scopeFactory">Фабрика scope-ов для резолва Scoped-зависимостей на итерацию.</param>
        /// <param name="options">Настройки фоновых задач модуля.</param>
        /// <param name="logger">Логгер.</param>
        public SubscriptionExpirationBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<SubscriptionSchedulerOptions> options,
            ILogger<SubscriptionExpirationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _options      = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger       = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Фоновый сборщик истёкших подписок отключён настройкой Enabled.");
                return;
            }

            if (_options.IntervalMinutes <= 0)
            {
                _logger.LogError(
                    "Некорректный интервал фонового сборщика истёкших подписок: {IntervalMinutes} мин. " +
                    "Сборщик не запущен.",
                    _options.IntervalMinutes);

                return;
            }

            var interval = TimeSpan.FromMinutes(_options.IntervalMinutes);

            _logger.LogInformation(
                "Фоновый сборщик истёкших подписок запущен. Интервал: {IntervalMinutes} мин, размер батча: {BatchSize}.",
                _options.IntervalMinutes, _options.BatchSize);

            using var timer = new PeriodicTimer(interval);

            try
            {
                do
                {
                    await RunIterationAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Штатная остановка приложения — не ошибка.
                _logger.LogInformation("Фоновый сборщик истёкших подписок остановлен.");
            }
        }

        /// <summary>
        /// Выполняет одну итерацию: создаёт scope и отправляет команду истечения.
        /// Исключения логируются и не выпускаются наружу, кроме отмены при остановке.
        /// </summary>
        /// <param name="cancellationToken">Токен остановки сервиса.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        private async Task RunIterationAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                Result<ExpireDueSubscriptionsResult> result = await sender.Send(
                    new ExpireDueSubscriptionsCommand(_options.BatchSize),
                    cancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "Итерация фонового сборщика истёкших подписок завершилась ошибкой. " +
                        "Код: {ErrorCode}. Сообщение: {ErrorMessage}.",
                        result.Error.Code, result.Error.Message);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Пробрасываем — остановку обрабатывает ExecuteAsync.
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Необработанная ошибка в итерации фонового сборщика истёкших подписок. " +
                    "Сервис продолжит работу и повторит попытку на следующем тике.");
            }
        }
    }
}
