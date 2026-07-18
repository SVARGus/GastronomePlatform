namespace GastronomePlatform.Modules.Subscriptions.Application.Authorization
{
    /// <summary>
    /// Сервис проверки права пользователя на покупку тарифного плана с покупочным
    /// роль-гейтом (POL-004 §4.2, §5.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Отделён от <see cref="ISubscriptionAccessPolicy"/> и
    /// <c>ISubscriptionAccessService</c> сознательно: покупочный гейт (порог
    /// «не ниже роли») содержательно совпадает с KYC-проверкой (реальная реализация
    /// на Этапе 6, домены Users/verification).
    /// </para>
    /// <para>
    /// Phase A — заглушка (<see cref="RoleEligibilityService"/> возвращает
    /// <see langword="true"/>). Роль-гейтованных Base-планов в Phase A не создаётся;
    /// реальная активация — вместе с UC-SUB-072 «Проверить eligibility для
    /// бизнес-плана» на Этапе 6.
    /// </para>
    /// </remarks>
    public interface IRoleEligibilityService
    {
        /// <summary>
        /// Проверяет, соответствует ли пользователь требуемой роли для покупки плана.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="requiredRole">
        /// Требуемая роль (значение из <see cref="GastronomePlatform.Common.Domain.Constants.PlatformRoles"/>).
        /// </param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/>, если пользователь удовлетворяет условию покупочного гейта;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> IsEligibleForRoleAsync(
            Guid userId,
            string requiredRole,
            CancellationToken cancellationToken = default);
    }
}
