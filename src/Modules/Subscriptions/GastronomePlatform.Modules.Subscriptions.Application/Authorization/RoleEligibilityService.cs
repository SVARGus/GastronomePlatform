namespace GastronomePlatform.Modules.Subscriptions.Application.Authorization
{
    /// <summary>
    /// Phase A-заглушка <see cref="IRoleEligibilityService"/> — всегда разрешает
    /// покупочный гейт.
    /// </summary>
    /// <remarks>
    /// Роль-гейтованные Base-планы в Phase A не создаются (в модели есть
    /// поле <c>SubscriptionPlan.RequiredRole</c>, но UC-SUB-001 Phase A не заводит
    /// таких планов). Реальная реализация появится на Этапе 6 вместе с KYC-флоу
    /// в модуле Users (см. POL-004 §5.1, UC-SUB-072).
    /// </remarks>
    public sealed class RoleEligibilityService : IRoleEligibilityService
    {
        /// <inheritdoc/>
        public Task<bool> IsEligibleForRoleAsync(
            Guid userId,
            string requiredRole,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }
}
