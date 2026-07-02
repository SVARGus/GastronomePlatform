using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Authorization
{
    /// <summary>
    /// Реестр усвоения грантов: сопоставляет каждому значению <see cref="FeatureGrant"/>
    /// требуемую роль пользователя для того, чтобы грант работал в рантайме
    /// (гейт усвоения). По умолчанию грант агностичен — работает у любого
    /// авторизованного пользователя.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Два роль-гейта модели не путать (см. domain-model.md §4.2):
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>Покупочный роль-гейт</term>
    ///     <description>
    ///       <c>SubscriptionPlan.RequiredRole</c> — кто может <i>купить</i> Base-план
    ///       (порог «не ниже роли»). Проверяется при покупке/апгрейде.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>Гейт усвоения (этот реестр)</term>
    ///     <description>
    ///       Работает ли уже выданный грант <i>в рантайме</i> без соответствующей роли.
    ///       Роль-привязанные гранты (продвижение/реклама → <c>Chef</c>) инертны без роли —
    ///       это и есть карательное понижение (см. domain-model.md §4.2).
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// Используется резолвером <c>ISubscriptionAccessService</c> при сборке
    /// эффективных грантов пользователя (Application-слой, Этап 3).
    /// </para>
    /// </remarks>
    public static class FeatureGrantRoleRequirements
    {
        /// <summary>
        /// Возвращает имя роли (значение <see cref="PlatformRoles"/>), необходимой
        /// для усвоения гранта, или <c>null</c>, если грант работает у любого
        /// авторизованного пользователя.
        /// </summary>
        /// <param name="grant">Значение <see cref="FeatureGrant"/>.</param>
        /// <returns>
        /// <see cref="PlatformRoles.CHEF"/> для грантов продвижения и рекламы
        /// (<see cref="FeatureGrant.PromotionBasic"/>, <see cref="FeatureGrant.PromotionAdvanced"/>,
        /// <see cref="FeatureGrant.DashboardAds"/>, <see cref="FeatureGrant.DashboardAdsExtended"/>);
        /// <c>null</c> — для потребительских грантов (<see cref="FeatureGrant.FullRecipes"/>,
        /// <see cref="FeatureGrant.PortionCalculator"/>, <see cref="FeatureGrant.SeasonalRecipes"/>,
        /// <see cref="FeatureGrant.SpecialCategories"/>) и неизвестных значений enum.
        /// </returns>
        public static string? RequiredRoleFor(FeatureGrant grant) => grant switch
        {
            FeatureGrant.PromotionBasic
                or FeatureGrant.PromotionAdvanced
                or FeatureGrant.DashboardAds
                or FeatureGrant.DashboardAdsExtended => PlatformRoles.CHEF,
            _ => null
        };
    }
}
