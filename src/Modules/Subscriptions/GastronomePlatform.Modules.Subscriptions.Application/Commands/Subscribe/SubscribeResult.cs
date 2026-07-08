namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.Subscribe
{
    /// <summary>
    /// Результат успешного выполнения <see cref="SubscribeCommand"/>.
    /// </summary>
    /// <param name="SubscriptionId">Идентификатор созданной подписки.</param>
    public sealed record SubscribeResult(Guid SubscriptionId);
}
