namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan
{
    /// <summary>
    /// Результат успешного выполнения <see cref="CreateSubscriptionPlanCommand"/>.
    /// </summary>
    /// <param name="PlanId">Идентификатор созданного плана.</param>
    public sealed record CreateSubscriptionPlanResult(Guid PlanId);
}
