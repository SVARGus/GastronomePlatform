using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetMySubscriptions
{
    /// <summary>
    /// Обработчик запроса <see cref="GetMySubscriptionsQuery"/> (UC-SUB-026).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Возвращает все подписки актора (историю целиком, от новых к старым):
    /// кабинет сам выбирает «текущую» по статусу и границе периода, а истёкшие
    /// показывает как историю. Пустой список — валидный ответ (подписок не было).
    /// </para>
    /// <para>
    /// Авторизация тривиальна: выборка выполняется строго по
    /// <c>ICurrentUserService.UserId</c>, чужие данные недостижимы по построению —
    /// поэтому <c>ISubscriptionAccessPolicy</c> (POL-004) не задействуется.
    /// Маппинг в <see cref="SubscriptionResponse"/> зеркалит UC-SUB-021.
    /// </para>
    /// </remarks>
    public sealed class GetMySubscriptionsQueryHandler
        : IQueryHandler<GetMySubscriptionsQuery, IReadOnlyList<SubscriptionResponse>>
    {
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetMySubscriptionsQueryHandler"/>.
        /// </summary>
        /// <param name="userSubscriptionRepository">Репозиторий подписок пользователей.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        public GetMySubscriptionsQueryHandler(
            IUserSubscriptionRepository userSubscriptionRepository,
            ICurrentUserService currentUser)
        {
            _userSubscriptionRepository = userSubscriptionRepository ?? throw new ArgumentNullException(nameof(userSubscriptionRepository));
            _currentUser                = currentUser                ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyList<SubscriptionResponse>>> Handle(
            GetMySubscriptionsQuery request,
            CancellationToken cancellationToken)
        {
            var actorUserId = _currentUser.UserId!.Value;

            IReadOnlyList<UserSubscription> subscriptions =
                await _userSubscriptionRepository.ListByUserAsync(actorUserId, cancellationToken);

            // Конкретный List, не IReadOnlyList: неявный оператор Result<T> не
            // применяется к выражениям интерфейсного типа (ограничение C# для
            // user-defined conversions) — эталон см. GetSubscriptionCatalogQueryHandler.
            List<SubscriptionResponse> response = subscriptions.Select(Map).ToList();
            return response;
        }

        private static SubscriptionResponse Map(UserSubscription subscription) => new(
            Id:                      subscription.Id,
            UserId:                  subscription.UserId,
            PlanId:                  subscription.PlanId,
            CurrentPriceId:          subscription.CurrentPriceId,
            Status:                  subscription.Status,
            SnapshotAmount:          subscription.SnapshotAmount,
            SnapshotCurrency:        subscription.SnapshotCurrency,
            StartsAt:                subscription.StartsAt,
            CurrentPeriodStart:      subscription.CurrentPeriodStart,
            CurrentPeriodEnd:        subscription.CurrentPeriodEnd,
            TrialEnd:                subscription.TrialEnd,
            NextBillingAt:           subscription.NextBillingAt,
            AutoRenew:               subscription.AutoRenew,
            CancelAtPeriodEnd:       subscription.CancelAtPeriodEnd,
            RecurringDisabledReason: subscription.RecurringDisabledReason,
            CanceledAt:              subscription.CanceledAt,
            EndedAt:                 subscription.EndedAt,
            CreatedAt:               subscription.CreatedAt,
            UpdatedAt:               subscription.UpdatedAt);
    }
}
