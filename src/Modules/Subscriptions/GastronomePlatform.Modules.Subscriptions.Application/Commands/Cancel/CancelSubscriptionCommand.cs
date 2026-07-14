using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.Cancel
{
    /// <summary>
    /// Команда отмены автопродления подписки (UC-SUB-022).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Актор (владелец подписки либо администратор) отменяет автопродление —
    /// доступ сохраняется до <c>UserSubscription.CurrentPeriodEnd</c>, роль
    /// пользователя не изменяется до фактического истечения периода
    /// (переход в <c>Expired</c> выполняется фоновой задачей UC-SUB-203).
    /// </para>
    /// <para>
    /// Идентификатор актора берётся из <c>ICurrentUserService</c> в хендлере,
    /// в теле команды хранится только идентификатор отменяемой подписки.
    /// Реализует <see cref="ICommand"/> без generic — успешная отмена не возвращает
    /// нового значения, только <c>Result.Success()</c> → HTTP 204 No Content.
    /// </para>
    /// </remarks>
    /// <param name="SubscriptionId">Идентификатор отменяемой подписки.</param>
    public sealed record CancelSubscriptionCommand(Guid SubscriptionId) : ICommand;
}
