using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.Subscribe
{
    /// <summary>
    /// Команда оформления новой подписки пользователем (UC-SUB-020).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Идентификатор пользователя берётся из <c>ICurrentUserService</c> в хендлере,
    /// а не из тела запроса, чтобы исключить актор-спуфинг (клиент не может оформить
    /// подписку от чужого имени).
    /// </para>
    /// <para>
    /// Идемпотентность повторного нажатия «Subscribe» в Phase A не решается —
    /// в TECH-DEBT (нужен <c>Idempotency-Key</c>-заголовок + хранилище ключей,
    /// инфраструктура Phase C).
    /// </para>
    /// </remarks>
    /// <param name="PriceId">Идентификатор оффера (<c>PlanPrice</c>) для покупки.</param>
    /// <param name="PaymentMethodId">Токен способа оплаты, полученный от шлюза на клиенте.</param>
    /// <param name="AcceptedTermsAt">Момент явного акта согласия пользователя с офертой.</param>
    public sealed record SubscribeCommand(
        Guid PriceId,
        string PaymentMethodId,
        DateTimeOffset AcceptedTermsAt) : ICommand<SubscribeResult>;
}
