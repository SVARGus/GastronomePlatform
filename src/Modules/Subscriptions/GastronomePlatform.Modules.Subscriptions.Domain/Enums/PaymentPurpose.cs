namespace GastronomePlatform.Modules.Subscriptions.Domain.Enums
{
    /// <summary>
    /// Назначение попытки списания — зачем платёжному шлюзу отправлен запрос.
    /// Хранится как <c>int</c> в БД. Используется в <c>SubscriptionPayment.Purpose</c>.
    /// </summary>
    public enum PaymentPurpose
    {
        /// <summary>
        /// Проверочное списание (1 ₽) для привязки рекуррента
        /// у <c>Trial</c>/<c>Scheduled</c>-подписок; возвращается синхронно
        /// в том же обработчике после успеха верификации.
        /// </summary>
        Verification = 0,

        /// <summary>
        /// Первое реальное списание при оформлении платного оффера
        /// (заодно создаёт привязку способа оплаты у шлюза).
        /// </summary>
        Initial = 1,

        /// <summary>
        /// Рекуррентное списание при продлении, в том числе
        /// по удешевлённому офферу после понижения по <c>Fallback</c>.
        /// </summary>
        Recurring = 2
    }
}
