using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById
{
    /// <summary>
    /// Запрос карточки подписки по идентификатору (UC-SUB-021).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Актор — владелец подписки (<c>UserSubscription.UserId == ICurrentUserService.UserId</c>)
    /// либо администратор (POL-004 §4.1). Проверка прав делегируется
    /// <c>ISubscriptionAccessPolicy</c> в хендлере — не Owner и не Admin →
    /// <c>SUBS.FORBIDDEN_NOT_OWNER</c>; подписка не найдена → <c>SUBS.NOT_FOUND</c>.
    /// </para>
    /// <para>
    /// В теле запроса — только идентификатор запрашиваемой подписки; идентификатор
    /// актора берётся из <c>ICurrentUserService</c> в хендлере (клиент не может
    /// прочитать чужую подписку, подставив собственный <c>userId</c>).
    /// </para>
    /// </remarks>
    /// <param name="SubscriptionId">Идентификатор запрашиваемой подписки.</param>
    public sealed record GetSubscriptionByIdQuery(Guid SubscriptionId) : IQuery<SubscriptionResponse>;
}
