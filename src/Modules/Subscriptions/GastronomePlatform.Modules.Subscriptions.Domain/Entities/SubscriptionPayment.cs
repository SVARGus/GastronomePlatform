using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Журнальная запись попытки списания у платёжного шлюза — единица истории
    /// платежей подписки. Владеется агрегатом <see cref="UserSubscription"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Идемпотентность webhook обеспечивается уникальностью
    /// <see cref="GatewayTransactionId"/> (UNIQUE-индекс). Повторная доставка
    /// того же <c>webhook.payment.succeeded</c> не создаёт новую запись.
    /// </para>
    /// <para>
    /// Создание и все переходы статуса — <c>internal</c>: их инициирует только
    /// <see cref="UserSubscription"/> в рамках своих lifecycle-методов
    /// (например, <c>Activate</c> пишет <c>Verification</c>+<c>Refunded</c>
    /// или <c>Initial</c>+<c>Succeeded</c> внутри одной транзакции). Внешние
    /// консюмеры (Application, WebAPI) — только на чтение через геттеры.
    /// </para>
    /// </remarks>
    public sealed class SubscriptionPayment : Entity<Guid>
    {
        #region Limits

        /// <summary>Длина кода валюты (ISO 4217).</summary>
        public const int CURRENCY_LENGTH = 3;

        /// <summary>Максимальная длина <see cref="GatewayTransactionId"/>.</summary>
        public const int MAX_GATEWAY_TRANSACTION_ID_LENGTH = 200;

        /// <summary>Максимальная длина <see cref="FailureReason"/>.</summary>
        public const int MAX_FAILURE_REASON_LENGTH = 500;

        #endregion

        #region Properties

        /// <summary>FK на <see cref="UserSubscription"/>. <c>OnDelete: Cascade</c>.</summary>
        public Guid SubscriptionId { get; private set; }

        /// <summary>FK на <see cref="PlanPrice"/> — оффер, по которому шёл платёж. <c>OnDelete: Restrict</c>.</summary>
        public Guid PriceId { get; private set; }

        /// <summary>
        /// Назначение попытки (<see cref="PaymentPurpose.Verification"/>,
        /// <see cref="PaymentPurpose.Initial"/>, <see cref="PaymentPurpose.Recurring"/>).
        /// </summary>
        public PaymentPurpose Purpose { get; private set; }

        /// <summary>Сумма попытки списания.</summary>
        public decimal Amount { get; private set; }

        /// <summary>Валюта попытки (ISO 4217).</summary>
        public string Currency { get; private set; } = string.Empty;

        /// <summary>Текущий статус транзакции.</summary>
        public PaymentStatus Status { get; private set; }

        /// <summary>Номер попытки в текущей dunning-ступени (1 — первая, ≥2 — ретраи).</summary>
        public int AttemptNumber { get; private set; }

        /// <summary>
        /// ID транзакции у шлюза. <see langword="null"/> для <see cref="PaymentStatus.Pending"/>
        /// и для <see cref="PaymentStatus.Failed"/> без ответа шлюза. UNIQUE (идемпотентность webhook).
        /// </summary>
        public string? GatewayTransactionId { get; private set; }

        /// <summary>Сырой ответ шлюза (jsonb) — для диагностики. Опционально.</summary>
        public string? GatewayPayload { get; private set; }

        /// <summary>Код/причина отказа шлюза (например, <c>insufficient_funds</c>). Опционально.</summary>
        public string? FailureReason { get; private set; }

        /// <summary>Момент транзакции по версии шлюза.</summary>
        public DateTimeOffset OccurredAt { get; private set; }

        /// <summary>Момент создания записи у нас. Иммутабелен.</summary>
        public DateTimeOffset CreatedAt { get; private set; }

        #endregion

        #region Constructors

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private SubscriptionPayment() : base() { }

        /// <summary>Приватный конструктор — используется только из <see cref="Create"/>.</summary>
        private SubscriptionPayment(
            Guid subscriptionId,
            Guid priceId,
            PaymentPurpose purpose,
            decimal amount,
            string currency,
            int attemptNumber,
            DateTimeOffset occurredAt,
            DateTimeOffset utcNow)
            : base(Guid.NewGuid())
        {
            SubscriptionId = subscriptionId;
            PriceId = priceId;
            Purpose = purpose;
            Amount = amount;
            Currency = currency;
            AttemptNumber = attemptNumber;
            Status = PaymentStatus.Pending;
            OccurredAt = occurredAt;
            CreatedAt = utcNow;
        }

        #endregion

        /// <summary>
        /// Создаёт новую запись попытки списания в статусе <see cref="PaymentStatus.Pending"/>.
        /// Вызывается только из <see cref="UserSubscription"/>.
        /// </summary>
        /// <param name="subscriptionId">Идентификатор подписки-владельца.</param>
        /// <param name="priceId">Идентификатор оффера, по которому идёт платёж.</param>
        /// <param name="purpose">Назначение попытки.</param>
        /// <param name="amount">Сумма попытки.</param>
        /// <param name="currency">Валюта попытки.</param>
        /// <param name="attemptNumber">Номер попытки в текущей ступени.</param>
        /// <param name="occurredAt">Момент транзакции по шлюзу (может совпадать с <paramref name="utcNow"/> для mock).</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>Новая запись <see cref="SubscriptionPayment"/> в статусе <see cref="PaymentStatus.Pending"/>.</returns>
        internal static SubscriptionPayment Create(
            Guid subscriptionId,
            Guid priceId,
            PaymentPurpose purpose,
            decimal amount,
            string currency,
            int attemptNumber,
            DateTimeOffset occurredAt,
            DateTimeOffset utcNow)
        {
            return new SubscriptionPayment(
                subscriptionId,
                priceId,
                purpose,
                amount,
                currency,
                attemptNumber,
                occurredAt,
                utcNow);
        }

        /// <summary>
        /// Переводит платёж в <see cref="PaymentStatus.Succeeded"/>. Разрешён только
        /// из <see cref="PaymentStatus.Pending"/>.
        /// </summary>
        /// <param name="gatewayTransactionId">ID транзакции у шлюза.</param>
        /// <param name="gatewayPayload">Сырой ответ шлюза. Опционально.</param>
        /// <param name="occurredAt">Момент транзакции.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="SubscriptionsErrors.PaymentInvalidTransition"/>.
        /// </returns>
        internal Result MarkAsSucceeded(string gatewayTransactionId, string? gatewayPayload, DateTimeOffset occurredAt)
        {
            if (Status != PaymentStatus.Pending)
            {
                return SubscriptionsErrors.PaymentInvalidTransition;
            }

            Status = PaymentStatus.Succeeded;
            GatewayTransactionId = gatewayTransactionId;
            GatewayPayload = gatewayPayload;
            OccurredAt = occurredAt;

            return Result.Success();
        }

        /// <summary>
        /// Переводит платёж в <see cref="PaymentStatus.Failed"/>. Разрешён только
        /// из <see cref="PaymentStatus.Pending"/>.
        /// </summary>
        /// <param name="gatewayTransactionId">ID транзакции у шлюза. Опционально (может отсутствовать при сетевом сбое).</param>
        /// <param name="failureReason">Код/причина отказа.</param>
        /// <param name="gatewayPayload">Сырой ответ шлюза. Опционально.</param>
        /// <param name="occurredAt">Момент транзакции.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="SubscriptionsErrors.PaymentInvalidTransition"/>.
        /// </returns>
        internal Result MarkAsFailed(string? gatewayTransactionId, string failureReason, string? gatewayPayload, DateTimeOffset occurredAt)
        {
            if (Status != PaymentStatus.Pending)
            {
                return SubscriptionsErrors.PaymentInvalidTransition;
            }

            Status = PaymentStatus.Failed;
            GatewayTransactionId = gatewayTransactionId;
            FailureReason = failureReason;
            GatewayPayload = gatewayPayload;
            OccurredAt = occurredAt;

            return Result.Success();
        }

        /// <summary>
        /// Переводит платёж в <see cref="PaymentStatus.Refunded"/>. Разрешён только
        /// из <see cref="PaymentStatus.Succeeded"/> (нельзя вернуть неуспешный платёж).
        /// </summary>
        /// <param name="occurredAt">Момент возврата по шлюзу.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или
        /// <see cref="SubscriptionsErrors.PaymentInvalidTransition"/>.
        /// </returns>
        internal Result MarkAsRefunded(DateTimeOffset occurredAt)
        {
            if (Status != PaymentStatus.Succeeded)
            {
                return SubscriptionsErrors.PaymentInvalidTransition;
            }

            Status = PaymentStatus.Refunded;
            OccurredAt = occurredAt;

            return Result.Success();
        }
    }
}
