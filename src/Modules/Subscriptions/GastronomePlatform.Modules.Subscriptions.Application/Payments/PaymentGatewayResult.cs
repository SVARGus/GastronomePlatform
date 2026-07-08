namespace GastronomePlatform.Modules.Subscriptions.Application.Payments
{
    /// <summary>
    /// Успешный результат авторизации списания у платёжного шлюза.
    /// </summary>
    /// <remarks>
    /// Возвращается из методов <see cref="IPaymentGateway"/> при подтверждённом списании.
    /// Ошибка потока фиксируется через <see cref="Common.Domain.Results.Result{TValue}.Failure(Common.Domain.Results.Error)"/>
    /// — синтетические карты с ветками отказа появятся в Phase B (сейчас Phase A mock всегда success).
    /// </remarks>
    /// <param name="TransactionId">
    /// Идентификатор транзакции у шлюза. Используется как <c>gatewayTransactionId</c>
    /// в <see cref="Domain.Entities.SubscriptionPayment"/>.
    /// </param>
    /// <param name="RawPayload">
    /// Сырой JSON-ответ шлюза для диагностики. Опционально; сохраняется в
    /// <see cref="Domain.Entities.SubscriptionPayment.GatewayPayload"/>.
    /// </param>
    public sealed record PaymentGatewayResult(string TransactionId, string? RawPayload);
}
