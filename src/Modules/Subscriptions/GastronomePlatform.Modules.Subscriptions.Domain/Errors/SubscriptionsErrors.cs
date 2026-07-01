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
    }
}
