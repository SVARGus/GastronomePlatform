using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Authorization
{
    /// <summary>
    /// Резолвер эффективных грантов пользователя — реализация POL-004 §4.4
    /// (UC-SUB-050 <see cref="HasFeatureAsync"/> / UC-SUB-051 <see cref="GetEffectiveGrantsAsync"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Кросс-модульная точка входа: вызывается из других модулей (Dishes, Orders)
    /// для проверки доступа к монетизированному функционалу.
    /// </para>
    /// <para>
    /// Phase A: эффективные гранты = union <c>PlanGrant.Grant</c> по всем активным
    /// подпискам пользователя (<see cref="SubscriptionStatus.Trialing"/> /
    /// <see cref="SubscriptionStatus.Active"/> / <see cref="SubscriptionStatus.PastDue"/> /
    /// <see cref="SubscriptionStatus.Canceled"/> с <c>CurrentPeriodEnd &gt; now</c>).
    /// </para>
    /// <para>
    /// Не входит в Phase A (по решению сессии):
    /// <list type="bullet">
    ///   <item>Промо-оверлей (<c>PromotionGrant</c> add/remove) — Phase C.</item>
    ///   <item>Грантовый floor (базовый набор без активной Base) — Phase C.</item>
    ///   <item>Квотовые гранты (сумма <c>PlanGrant.Quantity</c>) — Phase C, UC-SUB-051 будет расширен.</item>
    ///   <item>Гейт усвоения (POL-004 §4.4, роль-привязанные гранты) — все реально
    ///         используемые Phase A гранты (1–4) агностичны; активация вместе с
    ///         подключением <c>IAuthUserService.GetUserRolesAsync</c>, ориентировочно
    ///         Этап 4+ на первом роль-привязанном гранте.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ISubscriptionAccessService
    {
        /// <summary>
        /// Проверяет, доступна ли пользователю указанная услуга.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="grant">Проверяемая услуга.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если <paramref name="grant"/> входит в эффективный
        /// набор грантов пользователя; иначе <see langword="false"/>.
        /// </returns>
        Task<bool> HasFeatureAsync(
            Guid userId,
            FeatureGrant grant,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает эффективный набор грантов пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Уникальный набор грантов из всех активных подписок пользователя. Пустая
        /// коллекция, если активных подписок нет.
        /// </returns>
        Task<IReadOnlyCollection<FeatureGrant>> GetEffectiveGrantsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
