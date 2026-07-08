using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Errors
{
    /// <summary>
    /// Доменные ошибки модуля Subscriptions.
    /// Префикс кодов — <c>SUBS.</c> по соглашению Development Guide §4.
    /// </summary>
    public static class SubscriptionsErrors
    {
        #region Доступ и поиск

        /// <summary>Подписка не существует.</summary>
        public static readonly Error NotFound =
            Error.NotFound("SUBS.NOT_FOUND", "Подписка не найдена.");

        #endregion

        #region Авторизация (POL-004)

        /// <summary>Actor не владелец и не Admin (POL-004 §4.1, §4.3).</summary>
        public static readonly Error ForbiddenNotOwner =
            Error.Forbidden("SUBS.FORBIDDEN_NOT_OWNER",
                "У вас нет прав на просмотр или изменение этой подписки.");

        /// <summary>План требует роль, не подтверждённую у пользователя (POL-004 §4.2 — покупочный гейт).</summary>
        public static readonly Error ForbiddenRoleRequired =
            Error.Forbidden("SUBS.FORBIDDEN_ROLE_REQUIRED",
                "Для оформления этого плана требуется специальная роль, отсутствующая у пользователя.");

        /// <summary>Эффективный набор грантов пользователя не содержит требуемую услугу (POL-004 §4.4).</summary>
        public static readonly Error FeatureNotGranted =
            Error.Forbidden("SUBS.FEATURE_NOT_GRANTED",
                "Запрошенная услуга недоступна в рамках текущей подписки.");

        #endregion

        #region Оформление и инварианты подписки

        /// <summary>У пользователя уже есть активная Base-подписка (POL-004 §4.2 — мульти-слот).</summary>
        public static readonly Error AlreadyHasBase =
            Error.Conflict("SUBS.ALREADY_HAS_BASE",
                "У пользователя уже есть активная Base-подписка. Перед оформлением новой отмените текущую.");

        /// <summary>Оффер недоступен к покупке (флаги <c>IsPurchasable</c>/<c>IsActive</c> или окно доступности).</summary>
        public static readonly Error OfferNotPurchasable =
            Error.Validation("SUBS.OFFER_NOT_PURCHASABLE",
                "Запрошенный оффер недоступен к покупке.");

        #endregion

        #region Инварианты каталога (SubscriptionPlan)

        /// <summary>У AddOn-плана не может быть покупочного роль-гейта <c>RequiredRole</c> (domain-model §4.7).</summary>
        public static readonly Error AddOnCannotHaveRequiredRole =
            Error.Validation("SUBS.ADDON_CANNOT_HAVE_ROLE",
                "У AddOn-плана не может быть покупочного роль-гейта (RequiredRole).");

        /// <summary>
        /// План с указанным <c>SubscriptionPlan.TechnicalName</c> уже существует
        /// (partial UNIQUE-индекс).
        /// </summary>
        public static readonly Error TechnicalNameTaken =
            Error.Conflict("SUBS.TECHNICAL_NAME_TAKEN",
                "План с таким системным именем уже существует.");

        #endregion

        #region Инварианты оффера (PlanPrice)

        /// <summary>Сумма оффера отрицательна.</summary>
        public static readonly Error PriceNegativeAmount =
            Error.Validation("SUBS.PRICE_NEGATIVE_AMOUNT",
                "Сумма оффера не может быть отрицательной.");

        /// <summary>
        /// Оффер вида <c>Trial</c> обязан иметь <c>Amount = 0</c> и заданный <c>TrialDays</c>
        /// (domain-model §9).
        /// </summary>
        public static readonly Error PriceTrialRequiresFreeWithDays =
            Error.Validation("SUBS.PRICE_TRIAL_REQUIRES_FREE_WITH_DAYS",
                "Оффер вида Trial обязан иметь Amount = 0 и заданный TrialDays.");

        /// <summary>
        /// У непродляющегося оффера (<c>IsRecurring = false</c>) не может быть переходов
        /// <c>RenewsAsPriceId</c>/<c>FallbackPriceId</c> — переходить некуда (domain-model §9).
        /// </summary>
        public static readonly Error PriceNonRecurringCannotTransition =
            Error.Validation("SUBS.PRICE_NON_RECURRING_CANNOT_TRANSITION",
                "У непродляющегося оффера не может быть переходов RenewsAs/Fallback.");

        #endregion

        #region Переходы UserSubscription

        /// <summary>
        /// Попытка активировать триал без заданного <c>TrialDays</c> оффера
        /// (не проходит валидацию оффера в фабрике).
        /// </summary>
        public static readonly Error ActivateTrialRequiresTrialDays =
            Error.Validation("SUBS.ACTIVATE_TRIAL_REQUIRES_TRIAL_DAYS",
                "Для активации триала оффер должен содержать TrialDays.");

        /// <summary>
        /// Попытка активировать платный оффер без заданного <c>DurationDays</c>
        /// (для Standard/Intro с ограниченной длительностью).
        /// </summary>
        public static readonly Error ActivatePaidRequiresDurationDays =
            Error.Validation("SUBS.ACTIVATE_PAID_REQUIRES_DURATION_DAYS",
                "Для активации платного оффера должен быть задан DurationDays.");

        /// <summary>Отмена возможна только из статусов <c>Trialing</c> или <c>Active</c>.</summary>
        public static readonly Error CannotCancelInStatus =
            Error.Conflict("SUBS.CANNOT_CANCEL_IN_STATUS",
                "Отмена возможна только для подписки в статусе Trialing или Active.");

        /// <summary>Истечение возможно только из статуса <c>Canceled</c> (Phase A подмножество).</summary>
        public static readonly Error CannotExpireInStatus =
            Error.Conflict("SUBS.CANNOT_EXPIRE_IN_STATUS",
                "Истечение возможно только для подписки в статусе Canceled.");

        #endregion

        #region Переходы SubscriptionPayment

        /// <summary>Недопустимый переход статуса платежа (например, <c>Refunded</c> из <c>Pending</c>).</summary>
        public static readonly Error PaymentInvalidTransition =
            Error.Conflict("SUBS.PAYMENT_INVALID_TRANSITION",
                "Недопустимый переход статуса платежа.");

        #endregion
    }
}
