using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionById;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetMySubscriptions
{
    /// <summary>
    /// Запрос списка подписок текущего пользователя (UC-SUB-026).
    /// </summary>
    /// <remarks>
    /// Параметров нет: идентификатор пользователя берётся из JWT
    /// (<c>ICurrentUserService.UserId</c>) в обработчике — клиент не может
    /// запросить чужие подписки, поэтому политика POL-004 здесь не нужна.
    /// Валидатор отсутствует по той же причине.
    /// </remarks>
    public sealed record GetMySubscriptionsQuery()
        : IQuery<IReadOnlyList<SubscriptionResponse>>;
}
