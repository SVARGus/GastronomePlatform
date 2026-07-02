using GastronomePlatform.Common.Domain.Events;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Events
{
    /// <summary>
    /// Доменное событие — подписка вошла в состояние с доступом
    /// (<see cref="SubscriptionStatus.Trialing"/> или <see cref="SubscriptionStatus.Active"/>)
    /// из состояния без доступа. Поднимается в фабрике активации подписки.
    /// </summary>
    /// <remarks>
    /// Через integration-события (RabbitMQ, Этап 8+) сигнал уходит в модуль Users
    /// для повышения роли пользователя. На Этапе 3 — in-process через MediatR.
    /// Событие эмитит только <see cref="PlanKind.Base"/>-подписка; AddOn роль не меняет.
    /// </remarks>
    /// <param name="SubscriptionId">Идентификатор подписки.</param>
    /// <param name="UserId">Идентификатор владельца подписки.</param>
    /// <param name="PlanId">Идентификатор плана.</param>
    /// <param name="PlanKind">Род плана (<see cref="Enums.PlanKind.Base"/> / <see cref="Enums.PlanKind.AddOn"/>).</param>
    public sealed record SubscriptionActivatedEvent(
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
