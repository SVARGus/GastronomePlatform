namespace GastronomePlatform.Modules.Subscriptions.Domain.Enums
{
    /// <summary>
    /// Причина отключения рекуррентных списаний по подписке.
    /// Хранится как <c>int</c> в БД. Используется
    /// в <c>UserSubscription.RecurringDisabledReason</c> (<c>null</c>,
    /// пока рекуррент активен).
    /// </summary>
    public enum RecurringDisabledReason
    {
        /// <summary>
        /// Отключён антифрод-проверкой платёжного шлюза
        /// (по значению <c>cancellation_details.reason</c>).
        /// </summary>
        Antifraud = 0,

        /// <summary>
        /// Исчерпаны ретраи без успешного списания — терминал
        /// dunning-лестницы (<c>FallbackPriceId = null</c>).
        /// </summary>
        AttemptsExhausted = 1,

        /// <summary>Отменён пользователем.</summary>
        UserCanceled = 2,

        /// <summary>
        /// Оплаченный период закончился без продления — подписка переведена
        /// в <c>Expired</c> фоновым сборщиком. Проставляется только тогда, когда
        /// причина ещё не задана: у отменённой пользователем подписки сохраняется
        /// <see cref="UserCanceled"/> как более точная.
        /// </summary>
        PeriodEnded = 3
    }
}
