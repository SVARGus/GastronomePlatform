using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionCatalog
{
    /// <summary>
    /// Запрос витрины каталога подписок (UC-SUB-040): планы, предлагаемые
    /// к покупке, вместе с их офферами и составом услуг.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Публичный сценарий — параметров не имеет и контекста пользователя не требует.
    /// Ответ одинаков для гостя и для авторизованного: витрина информирует,
    /// но не авторизует.
    /// </para>
    /// <para>
    /// Персональная проверка права на покупку в витрину намеренно не заводится.
    /// Роль-гейтованные планы показываются всем с указанием требуемой роли
    /// в <see cref="SubscriptionCatalogPlanResponse.RequiredRole"/> — пометка является
    /// свойством плана, а не пользователя. Фактическая проверка выполняется при
    /// оформлении (UC-SUB-020, <c>SUBS.FORBIDDEN_ROLE_REQUIRED</c>).
    /// </para>
    /// <para>
    /// Промо-оверлей (<c>PromotionGrant</c>) в выдаче не участвует — резолвер промо
    /// появится вместе с UC-SUB-042.
    /// </para>
    /// </remarks>
    public sealed record GetSubscriptionCatalogQuery
        : IQuery<IReadOnlyList<SubscriptionCatalogPlanResponse>>;
}
