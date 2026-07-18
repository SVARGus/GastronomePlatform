using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Repositories
{
    /// <summary>
    /// Проекция подписки, у которой истёк оплаченный период: идентификатор
    /// и род плана. Результат выборки
    /// <see cref="IUserSubscriptionRepository.ListExpirationCandidatesAsync"/>
    /// для фонового сборщика UC-SUB-203.
    /// </summary>
    /// <remarks>
    /// Род плана попадает в проекцию потому, что <c>UserSubscription.Expire</c>
    /// принимает его параметром для доменного события, а сам агрегат его не хранит:
    /// связь <c>UserSubscription.PlanId → SubscriptionPlan</c> держится только
    /// по идентификатору, денормализация <c>PlanKind</c> сознательно не делалась.
    /// Без этой проекции сборщику пришлось бы догружать план на каждую подписку.
    /// </remarks>
    /// <param name="SubscriptionId">Идентификатор подписки.</param>
    /// <param name="PlanKind">Род плана, по которому оформлена подписка.</param>
    public sealed record ExpirationCandidate(Guid SubscriptionId, PlanKind PlanKind);
}
