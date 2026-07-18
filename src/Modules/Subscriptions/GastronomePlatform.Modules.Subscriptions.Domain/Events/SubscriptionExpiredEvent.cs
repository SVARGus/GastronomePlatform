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
    /// <para>
    /// Роль пользователя по этому событию <b>не отзывается</b>. Роль в модели —
    /// входной критерий: чтобы план стал доступен к покупке, пользователь должен
    /// заранее соответствовать требованиям подъёма по роли (подтверждённые данные
    /// в личном кабинете, подписанные соглашения). Это покупочный роль-гейт
    /// <c>SubscriptionPlan.RequiredRole</c> (POL-004 §4.2). Обратной зависимости
    /// «период закончился — роль снята» в модели нет, и доступ к платным
    /// возможностям она не определяет: он резолвится через гранты активных
    /// подписок, которые отсекаются по <c>CurrentPeriodEnd</c> сами.
    /// </para>
    /// <para>
    /// Ожидаемый потребитель — модуль Notifications (Этап 5): уведомление
    /// пользователю об окончании подписки. До его появления событие эмитится
    /// без подписчиков и фиксирует факт бизнес-перехода.
    /// </para>
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
