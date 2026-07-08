using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Authorization
{
    /// <summary>
    /// Phase A реализация <see cref="ISubscriptionAccessService"/> — union
    /// <c>PlanGrant.Grant</c> по активным подпискам пользователя через read-проекцию
    /// репозитория.
    /// </summary>
    public sealed class SubscriptionAccessService : ISubscriptionAccessService
    {
        private readonly IUserSubscriptionRepository _repository;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionAccessService"/>.
        /// </summary>
        /// <param name="repository">Репозиторий подписок пользователей.</param>
        /// <param name="clock">Поставщик системного времени (для guard-фильтра по <c>CurrentPeriodEnd</c>).</param>
        public SubscriptionAccessService(IUserSubscriptionRepository repository, IDateTimeProvider clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock      = clock      ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<bool> HasFeatureAsync(
            Guid userId,
            FeatureGrant grant,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<FeatureGrant> grants =
                await GetEffectiveGrantsAsync(userId, cancellationToken);
            return grants.Contains(grant);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<FeatureGrant>> GetEffectiveGrantsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<FeatureGrant> raw = await _repository.ListActiveGrantsByUserAsync(
                userId,
                _clock.UtcNow,
                cancellationToken);

            // SECURITY: гейт усвоения (POL-004 §4.4) отложен.
            // Триггер подключения: первый роль-привязанный грант из
            // FeatureGrantRoleRequirements (сейчас — гранты 5–8 инертны, требуют
            // роль Chef), ориентировочно Этап 4+. Порог — не равенство, а «есть
            // хотя бы один роль-привязанный грант, реально используемый в UC».
            // Источник ролей — IAuthUserService.GetUserRolesAsync(userId, ct).

            return raw;
        }
    }
}
