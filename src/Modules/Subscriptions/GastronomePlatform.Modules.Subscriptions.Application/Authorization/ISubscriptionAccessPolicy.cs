using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Subscriptions.Application.Authorization
{
    /// <summary>
    /// Политика авторизации операций над подпиской — реализация POL-004 §4.1, §4.3
    /// (Owner или Admin).
    /// </summary>
    /// <remarks>
    /// Используется хендлерами UC-SUB-021 (Просмотр), UC-SUB-022 (Отмена),
    /// UC-SUB-023 (Реактивация — Phase C), UC-SUB-024 (Смена способа оплаты — Phase C).
    /// Не применяется в admin-каталожных UC (UC-SUB-001..010) — там прямое ограничение
    /// на роль <c>Admin</c> на уровне контроллера.
    /// </remarks>
    public interface ISubscriptionAccessPolicy
    {
        /// <summary>
        /// Проверяет, разрешено ли актору выполнить операцию над указанной подпиской.
        /// </summary>
        /// <param name="subscriptionId">Идентификатор подписки.</param>
        /// <param name="actorUserId">Идентификатор актора (текущий пользователь).</param>
        /// <param name="actorRoles">Роли актора (из JWT claims).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result.Success()"/>, если операция разрешена;
        /// <c>SUBS.NOT_FOUND</c>, если подписка не существует;
        /// <c>SUBS.FORBIDDEN_NOT_OWNER</c>, если актор не владелец и не Admin.
        /// </returns>
        Task<Result> AuthorizeOperationAsync(
            Guid subscriptionId,
            Guid actorUserId,
            IReadOnlyCollection<string> actorRoles,
            CancellationToken cancellationToken = default);
    }
}
