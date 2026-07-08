using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Payments;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Payments
{
    /// <summary>
    /// Phase A mock-реализация <see cref="IPaymentGateway"/>: всегда возвращает
    /// синтетическую успешную транзакцию.
    /// </summary>
    /// <remarks>
    /// В Phase A нет цели покрыть отказные ветки шлюза — задача vertical slice
    /// UC-SUB-020 «Subscribe» — прогнать happy-path создания подписки end-to-end.
    /// Синтетические карты с ветками отказа (по образцу test-card-номеров ЮKassa),
    /// повторные попытки, dunning-логика — Phase B через реальный
    /// <c>YooKassaPaymentGateway</c> + webhook UC-SUB-201.
    /// </remarks>
    public sealed class MockPaymentGateway : IPaymentGateway
    {
        /// <inheritdoc/>
        public Task<Result<PaymentGatewayResult>> AuthorizeVerificationChargeAsync(
            string paymentMethodId,
            string currency,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Result<PaymentGatewayResult>>(BuildSuccess(paymentMethodId));

        /// <inheritdoc/>
        public Task<Result<PaymentGatewayResult>> AuthorizeInitialChargeAsync(
            string paymentMethodId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Result<PaymentGatewayResult>>(BuildSuccess(paymentMethodId));

        private static PaymentGatewayResult BuildSuccess(string paymentMethodId)
        {
            string transactionId = $"mock_tx_{Guid.NewGuid():N}";
            string rawPayload = $$"""{"status":"succeeded","payment_method_id":"{{paymentMethodId}}","transaction_id":"{{transactionId}}"}""";
            return new PaymentGatewayResult(transactionId, rawPayload);
        }
    }
}
