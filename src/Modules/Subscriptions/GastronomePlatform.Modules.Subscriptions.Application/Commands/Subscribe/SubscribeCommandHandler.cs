using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Application.Payments;
using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.Subscribe
{
    /// <summary>
    /// Обработчик команды <see cref="SubscribeCommand"/> (UC-SUB-020).
    /// </summary>
    /// <remarks>
    /// <para>Поток выполнения:</para>
    /// <list type="number">
    ///   <item>Существование оффера (<c>SUBS.PRICE_NOT_FOUND</c>).</item>
    ///   <item>Покупаемость оффера — флаги <c>IsPurchasable</c>/<c>IsActive</c> + окно
    ///         <c>AvailableFrom</c>/<c>AvailableUntil</c> (<c>SUBS.OFFER_NOT_PURCHASABLE</c>).</item>
    ///   <item>Существование плана (<c>SUBS.PLAN_NOT_FOUND</c>).</item>
    ///   <item>Покупочный роль-гейт POL-004 §4.2 через <see cref="IRoleEligibilityService"/>
    ///         (<c>SUBS.FORBIDDEN_ROLE_REQUIRED</c>) — только если <c>plan.RequiredRole</c> задан.</item>
    ///   <item>Инвариант «≤1 активной Base» POL-004 §4.2 (<c>SUBS.ALREADY_HAS_BASE</c>) —
    ///         только для Base-планов.</item>
    ///   <item>Авторизация списания через <see cref="IPaymentGateway"/>: Trial →
    ///         <see cref="IPaymentGateway.AuthorizeVerificationChargeAsync"/>;
    ///         иначе → <see cref="IPaymentGateway.AuthorizeInitialChargeAsync"/>.</item>
    ///   <item>Доменная фабрика <see cref="UserSubscription.Activate"/> —
    ///         создаёт агрегат, поднимает <c>SubscriptionActivatedEvent</c>.</item>
    ///   <item>Persist + <c>SaveChangesAsync</c>.</item>
    ///   <item>Публикация доменных событий через <see cref="IDomainEventDispatcher"/>
    ///         (только после успешного <c>SaveChangesAsync</c>).</item>
    /// </list>
    /// <para>
    /// Terms snapshot — в Phase A заглушка (<c>"{}"</c>): полноценный snapshot-builder
    /// с версионированием оферты — отдельный слайс на Этапе 4+.
    /// </para>
    /// </remarks>
    public sealed class SubscribeCommandHandler : ICommandHandler<SubscribeCommand, SubscribeResult>
    {
        private const string PHASE_A_TERMS_SNAPSHOT = "{}";

        private readonly IPlanPriceRepository _priceRepository;
        private readonly ISubscriptionPlanRepository _planRepository;
        private readonly IUserSubscriptionRepository _userSubscriptionRepository;
        private readonly IRoleEligibilityService _roleEligibility;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscribeCommandHandler"/>.
        /// </summary>
        /// <param name="priceRepository">Репозиторий офферов.</param>
        /// <param name="planRepository">Репозиторий планов.</param>
        /// <param name="userSubscriptionRepository">Репозиторий подписок пользователей.</param>
        /// <param name="roleEligibility">Сервис проверки покупочного роль-гейта.</param>
        /// <param name="paymentGateway">Порт платёжного шлюза.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public SubscribeCommandHandler(
            IPlanPriceRepository priceRepository,
            ISubscriptionPlanRepository planRepository,
            IUserSubscriptionRepository userSubscriptionRepository,
            IRoleEligibilityService roleEligibility,
            IPaymentGateway paymentGateway,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IDomainEventDispatcher eventDispatcher)
        {
            _priceRepository            = priceRepository            ?? throw new ArgumentNullException(nameof(priceRepository));
            _planRepository             = planRepository             ?? throw new ArgumentNullException(nameof(planRepository));
            _userSubscriptionRepository = userSubscriptionRepository ?? throw new ArgumentNullException(nameof(userSubscriptionRepository));
            _roleEligibility            = roleEligibility            ?? throw new ArgumentNullException(nameof(roleEligibility));
            _paymentGateway             = paymentGateway             ?? throw new ArgumentNullException(nameof(paymentGateway));
            _currentUser                = currentUser                ?? throw new ArgumentNullException(nameof(currentUser));
            _clock                      = clock                      ?? throw new ArgumentNullException(nameof(clock));
            _eventDispatcher            = eventDispatcher            ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        /// <inheritdoc/>
        public async Task<Result<SubscribeResult>> Handle(
            SubscribeCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId!.Value;
            var utcNow = _clock.UtcNow;

            var price = await _priceRepository.GetByIdAsync(request.PriceId, cancellationToken);
            if (price is null)
            {
                return SubscriptionsErrors.PriceNotFound;
            }

            if (!IsPurchasable(price, utcNow))
            {
                return SubscriptionsErrors.OfferNotPurchasable;
            }

            var plan = await _planRepository.GetByIdAsync(price.PlanId, cancellationToken);
            if (plan is null)
            {
                return SubscriptionsErrors.PlanNotFound;
            }

            if (plan.RequiredRole is not null)
            {
                bool eligible = await _roleEligibility.IsEligibleForRoleAsync(
                    userId,
                    plan.RequiredRole,
                    cancellationToken);
                if (!eligible)
                {
                    return SubscriptionsErrors.ForbiddenRoleRequired;
                }
            }

            if (plan.PlanKind == PlanKind.Base)
            {
                bool hasBase = await _userSubscriptionRepository.HasActiveBaseAsync(
                    userId,
                    utcNow,
                    cancellationToken);
                if (hasBase)
                {
                    return SubscriptionsErrors.AlreadyHasBase;
                }
            }

            Result<PaymentGatewayResult> paymentResult = price.Kind == OfferKind.Trial
                ? await _paymentGateway.AuthorizeVerificationChargeAsync(
                    request.PaymentMethodId,
                    price.Currency,
                    cancellationToken)
                : await _paymentGateway.AuthorizeInitialChargeAsync(
                    request.PaymentMethodId,
                    price.Amount,
                    price.Currency,
                    cancellationToken);

            if (paymentResult.IsFailure)
            {
                return paymentResult.Error;
            }

            var payment = paymentResult.Value;

            Result<UserSubscription> activateResult = UserSubscription.Activate(
                userId:                 userId,
                planId:                 plan.Id,
                planKind:               plan.PlanKind,
                priceId:                price.Id,
                priceKind:              price.Kind,
                amount:                 price.Amount,
                currency:               price.Currency,
                durationDays:           price.DurationDays,
                trialDays:              price.TrialDays,
                gatewayPaymentMethodId: request.PaymentMethodId,
                gatewayTransactionId:   payment.TransactionId,
                gatewayPayload:         payment.RawPayload,
                termsSnapshot:          PHASE_A_TERMS_SNAPSHOT,
                documentNumber:         null,
                contentHash:            null,
                acceptedAt:             request.AcceptedTermsAt,
                utcNow:                 utcNow);

            if (activateResult.IsFailure)
            {
                return activateResult.Error;
            }

            var subscription = activateResult.Value;

            await _userSubscriptionRepository.AddAsync(subscription, cancellationToken);
            await _userSubscriptionRepository.SaveChangesAsync(cancellationToken);

            await _eventDispatcher.DispatchAsync(subscription, cancellationToken);

            return new SubscribeResult(subscription.Id);
        }

        /// <summary>
        /// Проверяет покупаемость оффера: активные флаги и попадание в окно доступности.
        /// </summary>
        private static bool IsPurchasable(PlanPrice price, DateTimeOffset utcNow)
        {
            if (!price.IsPurchasable || !price.IsActive)
            {
                return false;
            }

            if (price.AvailableFrom.HasValue && price.AvailableFrom.Value > utcNow)
            {
                return false;
            }

            if (price.AvailableUntil.HasValue && price.AvailableUntil.Value <= utcNow)
            {
                return false;
            }

            return true;
        }
    }
}
