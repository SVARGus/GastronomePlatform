using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Subscriptions.Application.Payments
{
    /// <summary>
    /// Порт платёжного шлюза — абстрагирует авторизацию списаний при оформлении подписки
    /// (UC-SUB-020) от конкретной реализации (Phase A mock, Phase B ЮKassa).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Два семантических метода вместо одного универсального. Обоснование:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///   <b>Verification</b> — фиксированное списание 1 у.е. для проверки привязки способа оплаты
    ///   при активации триала. Константа <c>VERIFICATION_AMOUNT</c> хранится в
    ///   <see cref="Domain.Entities.UserSubscription"/>, поэтому call-site не считает сумму.
    ///   </item>
    ///   <item>
    ///   <b>Initial</b> — платёж по офферу на <see cref="Domain.Entities.PlanPrice.Amount"/>.
    ///   </item>
    /// </list>
    /// <para>
    /// Возврат <see cref="Task{TResult}"/> с <see cref="Result{TValue}"/> фиксирует
    /// поток управления хендлера с первой имплементации: в Phase B появятся синтетические
    /// карты с ветками отказа без переписывания <c>IsFailure</c>-веток вызывающего кода.
    /// </para>
    /// </remarks>
    public interface IPaymentGateway
    {
        /// <summary>
        /// Авторизует verification-списание (1 у.е.) для проверки привязки способа оплаты
        /// при активации триала.
        /// </summary>
        /// <param name="paymentMethodId">Токен способа оплаты, полученный от шлюза на клиенте.</param>
        /// <param name="currency">Код валюты (ISO 4217).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result{TValue}.Success(TValue)"/> с идентификатором транзакции
        /// либо ошибка списания.
        /// </returns>
        Task<Result<PaymentGatewayResult>> AuthorizeVerificationChargeAsync(
            string paymentMethodId,
            string currency,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Авторизует initial-списание по офферу.
        /// </summary>
        /// <param name="paymentMethodId">Токен способа оплаты.</param>
        /// <param name="amount">Сумма списания.</param>
        /// <param name="currency">Код валюты (ISO 4217).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result{TValue}.Success(TValue)"/> с идентификатором транзакции
        /// либо ошибка списания.
        /// </returns>
        Task<Result<PaymentGatewayResult>> AuthorizeInitialChargeAsync(
            string paymentMethodId,
            decimal amount,
            string currency,
            CancellationToken cancellationToken = default);
    }
}
