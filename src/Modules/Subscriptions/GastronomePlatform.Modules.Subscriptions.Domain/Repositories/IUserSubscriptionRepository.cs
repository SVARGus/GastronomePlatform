using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с агрегатом <see cref="UserSubscription"/>.
    /// </summary>
    /// <remarks>
    /// На bootstrap-этапе модуля (Phase A, admin-каталог + резолвер грантов)
    /// содержит минимальный набор операций, необходимых <c>ISubscriptionAccessPolicy</c>
    /// (загрузка по идентификатору для проверки владения) и <c>ISubscriptionAccessService</c>
    /// (read-проекция активных грантов пользователя). Обе точки живут в
    /// <c>Subscriptions.Application.Authorization</c> — Domain не ссылается через
    /// <c>see cref</c>, потому что зависимость Application → Domain однонаправленная.
    /// Операции <c>AddAsync</c>, <c>HasActiveBaseAsync</c> и специализированные выборки
    /// для webhook-обработчиков добавляются по мере появления UC-потребителей
    /// (UC-SUB-020 Subscribe, UC-SUB-201 webhook и т. п.).
    /// </remarks>
    public interface IUserSubscriptionRepository
    {
        /// <summary>
        /// Загружает агрегат <see cref="UserSubscription"/> по идентификатору.
        /// Подколлекции <c>Payments</c>/<c>Agreements</c> не подгружаются — вызывающему
        /// коду (<c>ISubscriptionAccessPolicy</c>) достаточно корневых полей для
        /// проверки владения.
        /// </summary>
        /// <param name="id">Идентификатор подписки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="UserSubscription"/>, если запись найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<UserSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает эффективные гранты пользователя, полученные union-ом
        /// <see cref="PlanGrant.Grant"/> всех активных подписок (Base + AddOn),
        /// с фильтром по актуальности периода.
        /// </summary>
        /// <remarks>
        /// <para>
        /// «Активная» = <see cref="UserSubscription.Status"/> ∈ {
        /// <see cref="SubscriptionStatus.Trialing"/>, <see cref="SubscriptionStatus.Active"/>,
        /// <see cref="SubscriptionStatus.PastDue"/>, <see cref="SubscriptionStatus.Canceled"/> }
        /// И <see cref="UserSubscription.CurrentPeriodEnd"/> &gt; <paramref name="utcNow"/>.
        /// </para>
        /// <para>
        /// <see cref="SubscriptionStatus.Canceled"/> в фильтре не ошибка: пользователь
        /// сохраняет доступ до конца оплаченного периода (POL-004 §4.4, доменная модель §5.4).
        /// Guard <c>CurrentPeriodEnd &gt; utcNow</c> — защита от лага фоновой задачи истечения
        /// (UC-SUB-203).
        /// </para>
        /// <para>
        /// Реализация — read-проекция (JOIN <c>UserSubscriptions</c> ⋈ <c>PlanGrants</c>
        /// по <c>PlanId</c>) без загрузки агрегата и без навигационных свойств между агрегатами:
        /// кросс-агрегатная связь <c>UserSubscription.PlanId → SubscriptionPlan</c> хранится
        /// только по идентификатору (см. Dish.AuthorUserId, POL-001).
        /// </para>
        /// <para>
        /// Phase A: гейт усвоения POL-004 §4.4 (роль-привязанные гранты)
        /// не применяется — все реально используемые Phase A гранты (1–4) агностичны,
        /// гранты 5–8 инертны до Этапа 4+. Активация гейта — в <c>SubscriptionAccessService</c>
        /// вместе с подключением <c>IAuthUserService.GetUserRolesAsync</c>.
        /// </para>
        /// </remarks>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="utcNow">Текущее время UTC (для guard-фильтра по <c>CurrentPeriodEnd</c>).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Список уникальных <see cref="FeatureGrant"/> из активных подписок пользователя.
        /// Пустой список, если активных подписок нет.
        /// </returns>
        Task<IReadOnlyList<FeatureGrant>> ListActiveGrantsByUserAsync(
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет изменения в хранилище (Unit of Work).
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
