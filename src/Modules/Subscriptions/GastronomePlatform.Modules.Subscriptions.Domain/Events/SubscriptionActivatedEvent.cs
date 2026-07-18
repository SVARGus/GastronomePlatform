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
    /// <para>
    /// Через integration-события (RabbitMQ, Этап 8+) сигнал уходит в модуль Users
    /// для повышения роли пользователя. Сейчас — in-process через MediatR.
    /// </para>
    /// <para>
    /// Событие поднимается для подписки <b>любого</b> рода, включая
    /// <see cref="PlanKind.AddOn"/>. Отбор выполняет подписчик: обработчик роли
    /// в Users реагирует только на <see cref="PlanKind.Base"/>, потому что AddOn
    /// роль не меняет. Фильтр стоит на стороне потребителя намеренно — событие
    /// описывает произошедший факт, а не намерение отправителя, и отсев AddOn
    /// в эмиттере лишил бы будущих подписчиков (уведомления, аналитика) части событий.
    /// </para>
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
