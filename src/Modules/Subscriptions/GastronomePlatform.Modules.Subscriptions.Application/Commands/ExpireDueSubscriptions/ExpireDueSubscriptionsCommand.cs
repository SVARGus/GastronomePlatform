using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.ExpireDueSubscriptions
{
    /// <summary>
    /// Команда перевода истёкших подписок в статус <c>Expired</c> (UC-SUB-203).
    /// Системная операция: вызывается фоновым сборщиком, а не пользователем.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Обрабатывает подписки, у которых <c>CurrentPeriodEnd</c> уже наступил,
    /// а статус остался <c>Trialing</c>, <c>Active</c> или <c>Canceled</c>.
    /// За один вызов берётся не более <paramref name="BatchSize"/> записей —
    /// остальные достанутся следующему тику.
    /// </para>
    /// <para>
    /// Команда не отвечает за отсечение доступа: гранты истёкшей подписки
    /// перестают резолвиться сами, по guard-у <c>CurrentPeriodEnd</c>. Её задача —
    /// привести статус в соответствие реальности, проставить <c>EndedAt</c>
    /// и породить <c>SubscriptionExpiredEvent</c>.
    /// </para>
    /// <para>
    /// Идентификатор актора не запрашивается: <c>ICurrentUserService</c> в системном
    /// контексте пуст — HTTP-запроса за фоновой задачей нет.
    /// </para>
    /// </remarks>
    /// <param name="BatchSize">Максимальное количество подписок, обрабатываемых за один вызов.</param>
    public sealed record ExpireDueSubscriptionsCommand(int BatchSize)
        : ICommand<ExpireDueSubscriptionsResult>;
}
