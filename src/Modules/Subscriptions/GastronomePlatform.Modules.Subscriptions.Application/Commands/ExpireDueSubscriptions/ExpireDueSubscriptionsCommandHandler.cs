using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.ExpireDueSubscriptions
{
    /// <summary>
    /// Обработчик команды <see cref="ExpireDueSubscriptionsCommand"/> (UC-SUB-203).
    /// </summary>
    /// <remarks>
    /// <para>Поток выполнения:</para>
    /// <list type="number">
    ///   <item>Выборка проекций истёкших подписок через
    ///         <see cref="IUserSubscriptionRepository.ListExpirationCandidatesAsync"/>.
    ///         Проекция несёт <c>PlanKind</c>, который агрегат не хранит, но требует
    ///         доменный метод.</item>
    ///   <item>Загрузка агрегатов одним запросом через
    ///         <see cref="IUserSubscriptionRepository.ListByIdsAsync"/>.</item>
    ///   <item>Доменный переход <c>UserSubscription.Expire(planKind, utcNow)</c>
    ///         по каждому агрегату. Отказ перехода не прерывает батч: подписка
    ///         пропускается и попадает в счётчик отказов.</item>
    ///   <item>Один <c>SaveChangesAsync</c> на весь батч.</item>
    ///   <item>Публикация доменных событий — только после успешного сохранения.</item>
    /// </list>
    /// <para>
    /// Время фиксируется один раз в начале обработки и используется и для выборки,
    /// и для доменных переходов. Иначе подписка, отобранная по одной границе,
    /// проверялась бы доменом по другой.
    /// </para>
    /// <para>
    /// Команда всегда завершается успехом, даже если часть переходов отклонена:
    /// частичный отказ — это диагностический факт для логов, а не ошибка операции.
    /// Возвращать <c>Result.Failure</c> здесь нечему — вызывающий фоновый сервис
    /// не может ни повторить, ни откатить батч осмысленно.
    /// </para>
    /// </remarks>
    public sealed class ExpireDueSubscriptionsCommandHandler
        : ICommandHandler<ExpireDueSubscriptionsCommand, ExpireDueSubscriptionsResult>
    {
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly IDateTimeProvider _clock;
        private readonly ILogger<ExpireDueSubscriptionsCommandHandler> _logger;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ExpireDueSubscriptionsCommandHandler"/>.
        /// </summary>
        /// <param name="userSubscriptionRepository">Репозиторий подписок пользователей.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="logger">Логгер.</param>
        public ExpireDueSubscriptionsCommandHandler(
            IUserSubscriptionRepository userSubscriptionRepository,
            IDomainEventDispatcher eventDispatcher,
            IDateTimeProvider clock,
            ILogger<ExpireDueSubscriptionsCommandHandler> logger)
        {
            _userSubscriptionRepository = userSubscriptionRepository ?? throw new ArgumentNullException(nameof(userSubscriptionRepository));
            _eventDispatcher            = eventDispatcher            ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _clock                      = clock                      ?? throw new ArgumentNullException(nameof(clock));
            _logger                     = logger                     ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<Result<ExpireDueSubscriptionsResult>> Handle(
            ExpireDueSubscriptionsCommand request,
            CancellationToken cancellationToken)
        {
            var utcNow = _clock.UtcNow;

            IReadOnlyList<ExpirationCandidate> candidates =
                await _userSubscriptionRepository.ListExpirationCandidatesAsync(
                    utcNow,
                    request.BatchSize,
                    cancellationToken);

            int expiredCount = 0;
            int failedCount = 0;

            if (candidates.Count > 0)
            {
                Dictionary<Guid, PlanKind> planKindBySubscriptionId =
                    candidates.ToDictionary(c => c.SubscriptionId, c => c.PlanKind);

                IReadOnlyList<UserSubscription> subscriptions =
                    await _userSubscriptionRepository.ListByIdsAsync(
                        planKindBySubscriptionId.Keys.ToList(),
                        cancellationToken);

                var expired = new List<UserSubscription>(subscriptions.Count);

                foreach (var subscription in subscriptions)
                {
                    var planKind = planKindBySubscriptionId[subscription.Id];

                    Result expireResult = subscription.Expire(planKind, utcNow);
                    if (expireResult.IsFailure)
                    {
                        // Штатно недостижимо: выборка отбирает ровно те статусы, которые
                        // принимает доменный метод. Срабатывание означает, что фильтр
                        // выборки и доменный инвариант разошлись.
                        failedCount++;

                        _logger.LogWarning(
                            "Подписка {SubscriptionId} не переведена в Expired. Код ошибки: {ErrorCode}. Сообщение: {ErrorMessage}.",
                            subscription.Id, expireResult.Error.Code, expireResult.Error.Message);

                        continue;
                    }

                    expired.Add(subscription);
                }

                if (expired.Count > 0)
                {
                    await _userSubscriptionRepository.SaveChangesAsync(cancellationToken);

                    foreach (var subscription in expired)
                    {
                        await _eventDispatcher.DispatchAsync(subscription, cancellationToken);
                    }
                }

                expiredCount = expired.Count;
            }

            // Лог пишется на каждой итерации, включая пустую. Нулевая строка —
            // единственное свидетельство того, что таймер тикает и задача жива:
            // без неё «сборщик работает и делать нечего» неотличимо от
            // «сборщик не запускается».
            _logger.LogInformation(
                "Истечение подписок: обработано {ExpiredCount}, отклонено {FailedCount}, отобрано кандидатов {CandidateCount}.",
                expiredCount, failedCount, candidates.Count);

            return new ExpireDueSubscriptionsResult(expiredCount, failedCount);
        }
    }
}
