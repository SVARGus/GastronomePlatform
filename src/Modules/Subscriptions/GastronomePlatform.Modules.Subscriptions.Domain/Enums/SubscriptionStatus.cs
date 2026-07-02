namespace GastronomePlatform.Modules.Subscriptions.Domain.Enums
{
    /// <summary>
    /// Состояние подписки пользователя в жизненном цикле.
    /// Хранится как <c>int</c> в БД. Используется в <c>UserSubscription.Status</c>.
    /// </summary>
    /// <remarks>
    /// Полная машина состояний с переходами и триггерами описана
    /// в <c>docs/public/modules/subscriptions/subscription-state-machine.md</c>.
    /// Статус <c>Paused</c> (заморозка) сознательно не предусмотрен.
    /// </remarks>
    public enum SubscriptionStatus
    {
        /// <summary>
        /// Подписка создана, но начнёт действовать в будущем (<c>StartsAt</c> впереди).
        /// Типично для retention с отложенным первым списанием. Доступа к грантам нет.
        /// </summary>
        Scheduled = 0,

        /// <summary>
        /// Активный пробный период; списаний по тарифу нет до <c>TrialEnd</c>.
        /// Доступ к грантам есть.
        /// </summary>
        Trialing = 1,

        /// <summary>Оплачена и действует. Доступ к грантам есть.</summary>
        Active = 2,

        /// <summary>
        /// Очередное списание не прошло, идут ретраи по dunning-расписанию.
        /// Доступ к грантам сохраняется (грейс-период до исчерпания лестницы).
        /// </summary>
        PastDue = 3,

        /// <summary>
        /// Автопродление отменено пользователем; подписка доживает
        /// до <c>CurrentPeriodEnd</c>. Доступ к грантам сохраняется до конца периода.
        /// </summary>
        Canceled = 4,

        /// <summary>
        /// Период закончился без продления; рекуррент отключён.
        /// Доступа к грантам нет.
        /// </summary>
        Expired = 5
    }
}
