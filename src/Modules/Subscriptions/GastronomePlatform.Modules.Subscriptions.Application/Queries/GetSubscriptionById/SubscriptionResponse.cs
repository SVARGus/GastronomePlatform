using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById
{
    /// <summary>
    /// DTO карточки подписки (UC-SUB-021 response).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Плоское представление скалярных полей <c>UserSubscription</c>. Обогащение
    /// именем плана (<c>SubscriptionPlan.PublicName</c>) и деталями оффера
    /// (<c>PlanPrice.Kind</c>, <c>Amount</c>, <c>Currency</c>) не выполняется —
    /// клиент получает <c>PlanId</c>/<c>CurrentPriceId</c> и при необходимости
    /// вызывает витринные UC-SUB-040/041.
    /// </para>
    /// <para>
    /// Служебные поля <c>FailedAttempts</c> (dunning-счётчик, в Phase A всегда 0)
    /// и <c>GatewayPaymentMethodId</c> (сырой токен способа оплаты у шлюза) в DTO
    /// не включены — первое неинформативно, второе требует маскирования на
    /// стороне шлюза (Phase B, при появлении реальной ЮKassa).
    /// </para>
    /// <para>
    /// Именование полей <c>SnapshotAmount</c>/<c>SnapshotCurrency</c> сохранено
    /// как в Domain (grandfathering): это сумма и валюта, зафиксированные в
    /// момент оформления текущего периода. При смене цены оффера уже действующие
    /// подписки продолжают платить по старой цене — это отражено именем.
    /// </para>
    /// </remarks>
    /// <param name="Id">Идентификатор подписки.</param>
    /// <param name="UserId">Идентификатор владельца.</param>
    /// <param name="PlanId">Идентификатор тарифного плана.</param>
    /// <param name="CurrentPriceId">Идентификатор текущего действующего оффера.</param>
    /// <param name="Status">Статус подписки в машине состояний.</param>
    /// <param name="SnapshotAmount">Зафиксированная сумма текущего периода (grandfathering). Для триала = 0.</param>
    /// <param name="SnapshotCurrency">Зафиксированная валюта текущего периода (ISO 4217).</param>
    /// <param name="StartsAt">Момент начала действия подписки.</param>
    /// <param name="CurrentPeriodStart">Начало текущего оплаченного периода.</param>
    /// <param name="CurrentPeriodEnd">Конец текущего оплаченного периода (доступ сохраняется до этой даты даже при <c>Canceled</c>).</param>
    /// <param name="TrialEnd">Конец триала, если подписка в триале; иначе <see langword="null"/>.</param>
    /// <param name="NextBillingAt">Когда планируется следующее списание; <see langword="null"/>, если автопродление отключено.</param>
    /// <param name="AutoRenew">Настроено ли автопродление.</param>
    /// <param name="CancelAtPeriodEnd">Флаг «отменить в конце периода».</param>
    /// <param name="RecurringDisabledReason">Причина отключения рекуррента; <see langword="null"/>, пока рекуррент активен.</param>
    /// <param name="CanceledAt">Когда пользователь инициировал отмену; <see langword="null"/>, если подписка не отменена.</param>
    /// <param name="EndedAt">Когда подписка фактически истекла; <see langword="null"/>, пока не в статусе <c>Expired</c>.</param>
    /// <param name="CreatedAt">Дата оформления подписки.</param>
    /// <param name="UpdatedAt">Дата последней правки.</param>
    public sealed record SubscriptionResponse(
        Guid Id,
        Guid UserId,
        Guid PlanId,
        Guid CurrentPriceId,
        SubscriptionStatus Status,
        decimal SnapshotAmount,
        string SnapshotCurrency,
        DateTimeOffset StartsAt,
        DateTimeOffset CurrentPeriodStart,
        DateTimeOffset CurrentPeriodEnd,
        DateTimeOffset? TrialEnd,
        DateTimeOffset? NextBillingAt,
        bool AutoRenew,
        bool CancelAtPeriodEnd,
        RecurringDisabledReason? RecurringDisabledReason,
        DateTimeOffset? CanceledAt,
        DateTimeOffset? EndedAt,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
