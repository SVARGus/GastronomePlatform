using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Events;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Подписка пользователя — единственный корень агрегата модуля Subscriptions.
    /// Инкапсулирует состояние жизненного цикла подписки, снепшот цены
    /// (grandfathering), счётчики dunning и композицию журнала платежей
    /// (<see cref="SubscriptionPayment"/>) и версий оферты (<see cref="SubscriptionAgreement"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Мульти-слот: у пользователя может быть несколько строк
    /// <see cref="UserSubscription"/>. Инвариант «≤1 активной Base-подписки»
    /// проверяется на уровне Application (это межагрегатная проверка).
    /// </para>
    /// <para>
    /// В Phase A реализованы только переходы, необходимые для UC-SUB-020
    /// (оформление), UC-SUB-022 (отмена) и UC-SUB-203 (истечение).
    /// Продление, ретраи, понижение по Fallback, антифрод, реактивация,
    /// scheduled-старт — Phase B/C, будут добавлены отдельными lifecycle-методами
    /// при появлении соответствующих UC. В enum-е <see cref="SubscriptionStatus"/>
    /// значения этих состояний уже присутствуют — enum-ы вводятся целиком.
    /// </para>
    /// </remarks>
    public sealed class UserSubscription : AggregateRoot<Guid>
    {
        #region Limits

        /// <summary>Длина кода валюты (ISO 4217).</summary>
        public const int CURRENCY_LENGTH = 3;

        /// <summary>Максимальная длина <see cref="GatewayPaymentMethodId"/>.</summary>
        public const int MAX_GATEWAY_PAYMENT_METHOD_ID_LENGTH = 200;

        #endregion

        /// <summary>
        /// Фиксированная сумма проверочного списания (1 у.е. в валюте оффера).
        /// Используется для верификации привязки способа оплаты у Trial-оффера
        /// (см. subscription-state-machine.md §3).
        /// </summary>
        private const decimal VERIFICATION_AMOUNT = 1m;

        // Backing fields для композитных коллекций. Настраиваются в UserSubscriptionConfiguration
        // через HasField(...) + PropertyAccessMode.Field.
        private readonly List<SubscriptionPayment> _payments = new();
        private readonly List<SubscriptionAgreement> _agreements = new();

        #region Properties

        /// <summary>Владелец подписки (кросс-модульная ссылка на <c>users.UserProfiles.UserId</c>, без FK).</summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Денормализованная ссылка на продукт. FK <see cref="SubscriptionPlan"/> с <c>Restrict</c>.
        /// Совпадает с <c>CurrentPrice.PlanId</c>, но хранится отдельно для быстрых запросов.
        /// </summary>
        public Guid PlanId { get; private set; }

        /// <summary>Текущий действующий оффер. FK <see cref="PlanPrice"/> с <c>Restrict</c> (защита grandfathering).</summary>
        public Guid CurrentPriceId { get; private set; }

        /// <summary>Состояние в машине состояний.</summary>
        public SubscriptionStatus Status { get; private set; }

        /// <summary>
        /// Зафиксированная сумма текущего периода (grandfathering).
        /// Для триала = 0, для платного оффера = <c>PlanPrice.Amount</c>.
        /// </summary>
        public decimal SnapshotAmount { get; private set; }

        /// <summary>Зафиксированная валюта текущего периода (ISO 4217).</summary>
        public string SnapshotCurrency { get; private set; } = string.Empty;

        /// <summary>Когда подписка начинает действовать (для retention/отложенных — в будущем).</summary>
        public DateTimeOffset StartsAt { get; private set; }

        /// <summary>Начало текущего оплаченного периода.</summary>
        public DateTimeOffset CurrentPeriodStart { get; private set; }

        /// <summary>Конец текущего оплаченного периода. Доступ сохраняется до этой даты даже при <see cref="SubscriptionStatus.Canceled"/>.</summary>
        public DateTimeOffset CurrentPeriodEnd { get; private set; }

        /// <summary>Конец триала. <see langword="null"/>, если подписка не в триале.</summary>
        public DateTimeOffset? TrialEnd { get; private set; }

        /// <summary>Настроено ли автопродление (выбор пользователя). <see langword="false"/> после отмены.</summary>
        public bool AutoRenew { get; private set; }

        /// <summary>«Отменить в конце периода, но не сейчас» — доступ до <see cref="CurrentPeriodEnd"/>.</summary>
        public bool CancelAtPeriodEnd { get; private set; }

        /// <summary>Счётчик неуспешных списаний на текущей dunning-ступени. В Phase A всегда 0.</summary>
        public int FailedAttempts { get; private set; }

        /// <summary>Когда планируется следующее списание. Индексируется для фонового сборщика.</summary>
        public DateTimeOffset? NextBillingAt { get; private set; }

        /// <summary>Токен привязки способа оплаты у шлюза (<c>payment_method_id</c> ЮKassa) для рекуррента.</summary>
        public string? GatewayPaymentMethodId { get; private set; }

        /// <summary>Причина отключения рекуррента. <see langword="null"/>, пока рекуррент активен.</summary>
        public RecurringDisabledReason? RecurringDisabledReason { get; private set; }

        /// <summary>Когда пользователь инициировал отмену.</summary>
        public DateTimeOffset? CanceledAt { get; private set; }

        /// <summary>Когда подписка фактически закончилась (переход в <see cref="SubscriptionStatus.Expired"/>).</summary>
        public DateTimeOffset? EndedAt { get; private set; }

        /// <summary>Дата создания. Иммутабельна.</summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>Дата последней правки.</summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        /// <summary>
        /// Журнал платежей подписки. Read-only коллекция; записи добавляются
        /// только через lifecycle-методы <see cref="UserSubscription"/>.
        /// </summary>
        public IReadOnlyList<SubscriptionPayment> Payments => _payments;

        /// <summary>
        /// Версии оферты подписки (append-only). Read-only коллекция; записи добавляются
        /// только через lifecycle-методы <see cref="UserSubscription"/>.
        /// </summary>
        public IReadOnlyList<SubscriptionAgreement> Agreements => _agreements;

        #endregion

        #region Constructors

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private UserSubscription() : base() { }

        /// <summary>Приватный конструктор — используется только из <see cref="Activate"/>.</summary>
        private UserSubscription(
            Guid userId,
            Guid planId,
            Guid currentPriceId,
            SubscriptionStatus status,
            decimal snapshotAmount,
            string snapshotCurrency,
            DateTimeOffset startsAt,
            DateTimeOffset currentPeriodStart,
            DateTimeOffset currentPeriodEnd,
            DateTimeOffset? trialEnd,
            DateTimeOffset? nextBillingAt,
            string gatewayPaymentMethodId,
            DateTimeOffset utcNow)
            : base(Guid.NewGuid())
        {
            UserId = userId;
            PlanId = planId;
            CurrentPriceId = currentPriceId;
            Status = status;
            SnapshotAmount = snapshotAmount;
            SnapshotCurrency = snapshotCurrency;
            StartsAt = startsAt;
            CurrentPeriodStart = currentPeriodStart;
            CurrentPeriodEnd = currentPeriodEnd;
            TrialEnd = trialEnd;
            AutoRenew = true;
            CancelAtPeriodEnd = false;
            FailedAttempts = 0;
            NextBillingAt = nextBillingAt;
            GatewayPaymentMethodId = gatewayPaymentMethodId;
            RecurringDisabledReason = null;
            CanceledAt = null;
            EndedAt = null;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Оформляет новую подписку по mock-шлюзу (Phase A, синхронный happy-path).
        /// Ветвится по <paramref name="priceKind"/>:
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Trial</b>: подписка входит в <see cref="SubscriptionStatus.Trialing"/>.
        /// Создаётся <see cref="SubscriptionPayment"/> с <see cref="PaymentPurpose.Verification"/>
        /// на сумму <see cref="VERIFICATION_AMOUNT"/> у.е.; сразу переводится в
        /// <see cref="PaymentStatus.Succeeded"/>, затем в <see cref="PaymentStatus.Refunded"/>
        /// (синхронный mock-refund, см. subscription-state-machine.md §3). Период =
        /// [<paramref name="utcNow"/>, <paramref name="utcNow"/> + <c>TrialDays</c>].
        /// <see cref="SnapshotAmount"/> = 0.
        /// </para>
        /// <para>
        /// <b>Intro/Standard/Retention/DunningFallback (платный)</b>: подписка входит
        /// в <see cref="SubscriptionStatus.Active"/>. Создаётся <see cref="SubscriptionPayment"/>
        /// с <see cref="PaymentPurpose.Initial"/> на сумму <paramref name="amount"/>;
        /// сразу переводится в <see cref="PaymentStatus.Succeeded"/>. Период =
        /// [<paramref name="utcNow"/>, <paramref name="utcNow"/> + <paramref name="durationDays"/>].
        /// <see cref="SnapshotAmount"/> = <paramref name="amount"/>.
        /// </para>
        /// <para>
        /// В обоих случаях создаётся первая <see cref="SubscriptionAgreement"/>
        /// с <see cref="AgreementChangeType.Initial"/> и версией = 1; поднимается
        /// <see cref="SubscriptionActivatedEvent"/>.
        /// </para>
        /// <para>
        /// В Phase B фабрика будет заменена/дополнена версиями, работающими через
        /// реальный webhook (асинхронный переход), — здесь сознательно вся логика
        /// внутри одной транзакции, как оговорено в Kickoff-логе Этапа 3.
        /// </para>
        /// </remarks>
        /// <param name="userId">Владелец подписки.</param>
        /// <param name="planId">Идентификатор плана.</param>
        /// <param name="planKind">Род плана (нужен для доменного события).</param>
        /// <param name="priceId">Идентификатор оффера.</param>
        /// <param name="priceKind">Природа оффера (определяет ветвление).</param>
        /// <param name="amount">Сумма оффера (для платного). Для триала должна быть 0.</param>
        /// <param name="currency">Код валюты (ISO 4217).</param>
        /// <param name="durationDays">Длительность периода в днях. Обязателен для платного оффера.</param>
        /// <param name="trialDays">Дней триала. Обязателен для <see cref="OfferKind.Trial"/>.</param>
        /// <param name="gatewayPaymentMethodId">Токен привязки способа оплаты у шлюза.</param>
        /// <param name="gatewayTransactionId">
        /// ID транзакции у шлюза (для Trial — ID verification-платежа;
        /// для платного — ID initial-платежа).
        /// </param>
        /// <param name="gatewayPayload">Сырой ответ шлюза для диагностики. Опционально.</param>
        /// <param name="termsSnapshot">JSON-снепшот условий для первичной оферты (собирается Application-слоем).</param>
        /// <param name="documentNumber">Человекочитаемый номер договора. Опционально.</param>
        /// <param name="contentHash">Хеш снепшота оферты. Опционально.</param>
        /// <param name="acceptedAt">Момент явного акта согласия пользователя.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result{TValue}.Success(TValue)"/> с новой подпиской и зарегистрированным
        /// <see cref="SubscriptionActivatedEvent"/>. Либо
        /// <see cref="SubscriptionsErrors.ActivateTrialRequiresTrialDays"/> /
        /// <see cref="SubscriptionsErrors.ActivatePaidRequiresDurationDays"/>, если параметры
        /// оффера не согласованы с ветвью активации.
        /// </returns>
        public static Result<UserSubscription> Activate(
            Guid userId,
            Guid planId,
            PlanKind planKind,
            Guid priceId,
            OfferKind priceKind,
            decimal amount,
            string currency,
            int? durationDays,
            int? trialDays,
            string gatewayPaymentMethodId,
            string gatewayTransactionId,
            string? gatewayPayload,
            string termsSnapshot,
            string? documentNumber,
            string? contentHash,
            DateTimeOffset acceptedAt,
            DateTimeOffset utcNow)
        {
            var isTrial = priceKind == OfferKind.Trial;

            if (isTrial && !trialDays.HasValue)
            {
                return SubscriptionsErrors.ActivateTrialRequiresTrialDays;
            }

            if (!isTrial && !durationDays.HasValue)
            {
                return SubscriptionsErrors.ActivatePaidRequiresDurationDays;
            }

            DateTimeOffset periodEnd = isTrial
                ? utcNow.AddDays(trialDays!.Value)
                : utcNow.AddDays(durationDays!.Value);

            SubscriptionStatus status = isTrial ? SubscriptionStatus.Trialing : SubscriptionStatus.Active;
            decimal snapshotAmount = isTrial ? 0m : amount;
            DateTimeOffset? trialEnd = isTrial ? periodEnd : null;

            var subscription = new UserSubscription(
                userId,
                planId,
                priceId,
                status,
                snapshotAmount,
                currency,
                startsAt: utcNow,
                currentPeriodStart: utcNow,
                currentPeriodEnd: periodEnd,
                trialEnd: trialEnd,
                nextBillingAt: periodEnd,
                gatewayPaymentMethodId,
                utcNow);

            // Первая версия оферты (Initial).
            var agreement = SubscriptionAgreement.Create(
                subscription.Id,
                version: 1,
                AgreementChangeType.Initial,
                termsSnapshot,
                documentNumber,
                contentHash,
                acceptedAt,
                effectiveAt: utcNow,
                utcNow);
            subscription._agreements.Add(agreement);

            // Первичный платёж: Trial → Verification (1) → Succeeded → Refunded;
            // Paid → Initial(amount) → Succeeded.
            if (isTrial)
            {
                var verificationPayment = SubscriptionPayment.Create(
                    subscription.Id,
                    priceId,
                    PaymentPurpose.Verification,
                    VERIFICATION_AMOUNT,
                    currency,
                    attemptNumber: 1,
                    occurredAt: utcNow,
                    utcNow);
                verificationPayment.MarkAsSucceeded(gatewayTransactionId, gatewayPayload, utcNow);
                verificationPayment.MarkAsRefunded(utcNow);
                subscription._payments.Add(verificationPayment);
            }
            else
            {
                var initialPayment = SubscriptionPayment.Create(
                    subscription.Id,
                    priceId,
                    PaymentPurpose.Initial,
                    amount,
                    currency,
                    attemptNumber: 1,
                    occurredAt: utcNow,
                    utcNow);
                initialPayment.MarkAsSucceeded(gatewayTransactionId, gatewayPayload, utcNow);
                subscription._payments.Add(initialPayment);
            }

            subscription.RaiseDomainEvent(new SubscriptionActivatedEvent(
                subscription.Id,
                subscription.UserId,
                subscription.PlanId,
                planKind));

            return subscription;
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Отменяет автопродление: переход в <see cref="SubscriptionStatus.Canceled"/>
        /// из <see cref="SubscriptionStatus.Trialing"/> или <see cref="SubscriptionStatus.Active"/>.
        /// Доступ сохраняется до <see cref="CurrentPeriodEnd"/>. Событие смены роли
        /// не порождает — оно пойдёт только при фактическом <see cref="Expire"/>.
        /// </summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="SubscriptionsErrors.CannotCancelInStatus"/>.
        /// </returns>
        public Result Cancel(DateTimeOffset utcNow)
        {
            if (Status is not (SubscriptionStatus.Trialing or SubscriptionStatus.Active))
            {
                return SubscriptionsErrors.CannotCancelInStatus;
            }

            Status = SubscriptionStatus.Canceled;
            AutoRenew = false;
            CancelAtPeriodEnd = true;
            RecurringDisabledReason = Enums.RecurringDisabledReason.UserCanceled;
            NextBillingAt = null;
            CanceledAt = utcNow;
            UpdatedAt = utcNow;

            return Result.Success();
        }

        /// <summary>
        /// Истечение подписки: переход <see cref="SubscriptionStatus.Canceled"/> →
        /// <see cref="SubscriptionStatus.Expired"/> при достижении
        /// <see cref="CurrentPeriodEnd"/> фоновым сборщиком (UC-SUB-203).
        /// Поднимает <see cref="SubscriptionExpiredEvent"/>.
        /// </summary>
        /// <param name="planKind">Род плана для доменного события (нужен подписчику Users для смены роли).</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="SubscriptionsErrors.CannotExpireInStatus"/> (в Phase A истекать
        /// может только отменённая подписка; для Phase B/C добавятся ветки из
        /// <c>PastDue</c> и <c>Scheduled</c>).
        /// </returns>
        public Result Expire(PlanKind planKind, DateTimeOffset utcNow)
        {
            if (Status != SubscriptionStatus.Canceled)
            {
                return SubscriptionsErrors.CannotExpireInStatus;
            }

            Status = SubscriptionStatus.Expired;
            EndedAt = utcNow;
            UpdatedAt = utcNow;

            RaiseDomainEvent(new SubscriptionExpiredEvent(Id, UserId, PlanId, planKind));

            return Result.Success();
        }

        #endregion
    }
}
