using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById
{
    /// <summary>
    /// Обработчик запроса <see cref="GetSubscriptionByIdQuery"/> (UC-SUB-021).
    /// </summary>
    /// <remarks>
    /// <para>Поток выполнения (по эталону UC-SUB-022):</para>
    /// <list type="number">
    ///   <item>Авторизация актора через <see cref="ISubscriptionAccessPolicy"/>
    ///         (POL-004 §4.1 — Owner ‖ Admin). Policy сама загружает подписку и
    ///         возвращает <c>SUBS.NOT_FOUND</c> либо <c>SUBS.FORBIDDEN_NOT_OWNER</c>.</item>
    ///   <item>Повторная загрузка агрегата через <see cref="IUserSubscriptionRepository.GetByIdAsync"/>
    ///         для маппинга в DTO. Двойная загрузка (Policy + Handler) — сознательное
    ///         следование эталону POL-004 §6.2 (см. решение Q1 логa UC-SUB-022). Второй
    ///         null-check — защита от race-window с удалением подписки между
    ///         двумя <c>GetByIdAsync</c>.</item>
    ///   <item>Маппинг <see cref="UserSubscription"/> → <see cref="SubscriptionResponse"/>
    ///         (плоское DTO без обогащения именем плана/деталями оффера — клиент
    ///         вызывает витринные UC-SUB-040/041 при необходимости).</item>
    /// </list>
    /// </remarks>
    public sealed class GetSubscriptionByIdQueryHandler
        : IQueryHandler<GetSubscriptionByIdQuery, SubscriptionResponse>
    {
        private readonly ISubscriptionAccessPolicy _accessPolicy;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetSubscriptionByIdQueryHandler"/>.
        /// </summary>
        /// <param name="accessPolicy">Политика авторизации операций над подпиской (POL-004).</param>
        /// <param name="userSubscriptionRepository">Репозиторий подписок пользователей.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        public GetSubscriptionByIdQueryHandler(
            ISubscriptionAccessPolicy accessPolicy,
            IUserSubscriptionRepository userSubscriptionRepository,
            ICurrentUserService currentUser)
        {
            _accessPolicy               = accessPolicy               ?? throw new ArgumentNullException(nameof(accessPolicy));
            _userSubscriptionRepository = userSubscriptionRepository ?? throw new ArgumentNullException(nameof(userSubscriptionRepository));
            _currentUser                = currentUser                ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<SubscriptionResponse>> Handle(
            GetSubscriptionByIdQuery request,
            CancellationToken cancellationToken)
        {
            var actorUserId = _currentUser.UserId!.Value;
            var actorRoles = _currentUser.Roles;

            Result authResult = await _accessPolicy.AuthorizeOperationAsync(
                request.SubscriptionId,
                actorUserId,
                actorRoles,
                cancellationToken);
            if (authResult.IsFailure)
            {
                return authResult.Error;
            }

            var subscription = await _userSubscriptionRepository.GetByIdAsync(
                request.SubscriptionId,
                cancellationToken);
            if (subscription is null)
            {
                return SubscriptionsErrors.NotFound;
            }

            return Map(subscription);
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
