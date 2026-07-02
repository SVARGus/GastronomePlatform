namespace GastronomePlatform.Modules.Subscriptions.Domain.Enums
{
    /// <summary>
    /// Тип версии оферты — по какому событию была создана
    /// эта строка <c>SubscriptionAgreement</c>.
    /// Хранится как <c>int</c> в БД. Используется в <c>SubscriptionAgreement.ChangeType</c>.
    /// </summary>
    /// <remarks>
    /// Новая версия оферты создаётся только при материальном изменении условий.
    /// Обычное продление с неизменной ценой и базовой информацией версию не плодит —
    /// факт продления фиксируется в <c>SubscriptionPayment</c> и полях периода
    /// <c>UserSubscription</c>, а не в оферте. Именно поэтому здесь нет
    /// отдельного значения <c>Renewal</c>.
    /// </remarks>
    public enum AgreementChangeType
    {
        /// <summary>Первичная оферта при оформлении подписки (<c>Version = 1</c>).</summary>
        Initial = 0,

        /// <summary>
        /// Изменение цены действующей подписки (на границе продления,
        /// после уведомления; либо переход интро → базовый оффер через
        /// <c>RenewsAsPriceId</c>, если меняется сумма).
        /// </summary>
        PriceChange = 1,

        /// <summary>Смена плана по инициативе пользователя (апгрейд/даунгрейд).</summary>
        PlanChange = 2,

        /// <summary>Автоматическое понижение по <c>FallbackPriceId</c> при исчерпании ретраев.</summary>
        Downgrade = 3
    }
}
