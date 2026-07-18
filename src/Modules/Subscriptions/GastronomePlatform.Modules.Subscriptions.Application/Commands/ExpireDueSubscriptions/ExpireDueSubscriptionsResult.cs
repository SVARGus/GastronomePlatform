namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.ExpireDueSubscriptions
{
    /// <summary>
    /// Результат выполнения <see cref="ExpireDueSubscriptionsCommand"/> —
    /// сводка по обработанному батчу.
    /// </summary>
    /// <remarks>
    /// Ненулевой <paramref name="FailedCount"/> означает, что доменный переход
    /// отклонил часть кандидатов. В штатном режиме это невозможно: выборка отбирает
    /// ровно те статусы, которые принимает <c>UserSubscription.Expire</c>. Значит
    /// значение &gt; 0 — сигнал о рассинхронизации фильтра выборки и доменного
    /// инварианта, и его стоит видеть в логах.
    /// </remarks>
    /// <param name="ExpiredCount">Сколько подписок переведено в <c>Expired</c>.</param>
    /// <param name="FailedCount">Сколько кандидатов отклонено доменным переходом.</param>
    public sealed record ExpireDueSubscriptionsResult(int ExpiredCount, int FailedCount);
}
