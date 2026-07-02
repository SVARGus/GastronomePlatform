using GastronomePlatform.Common.Domain.Events;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Events
{
    /// <summary>
    /// Доменное событие — подписка перешла в <see cref="SubscriptionStatus.Expired"/>
    /// (период закончился без продления). Поднимается в
    /// <c>UserSubscription.Expire</c>.
    /// </summary>
    /// <remarks>
    /// Через integration-события (RabbitMQ, Этап 8+) сигнал уходит в модуль Users
    /// для отзыва роли. Users перед отзывом проверяет, нет ли у пользователя другой
    /// активной <see cref="PlanKind.Base"/>-подписки, дающей ту же роль (мульти-слот).
    /// AddOn роль не меняет и это событие в контексте роли игнорируется.
    /// </remarks>
    /// <param name="SubscriptionId">Идентификатор подписки.</param>
    /// <param name="UserId">Идентификатор владельца подписки.</param>
    /// <param name="PlanId">Идентификатор плана.</param>
    /// <param name="PlanKind">Род плана.</param>
    public sealed record SubscriptionExpiredEvent(
        Guid SubscriptionId,
        Guid UserId,
        Guid PlanId,
        PlanKind PlanKind) : IDomainEvent
    {
        /// <inheritdoc/>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <inheritdoc/>
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
