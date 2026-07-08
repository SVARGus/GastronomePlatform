namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.AddPlanPrice
{
    /// <summary>
    /// Результат успешного выполнения <see cref="AddPlanPriceCommand"/>.
    /// </summary>
    /// <param name="PriceId">Идентификатор созданного оффера.</param>
    public sealed record AddPlanPriceResult(Guid PriceId);
}
