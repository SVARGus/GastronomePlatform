using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;

namespace GastronomePlatform.Modules.Subscriptions.Application.Authorization
{
    /// <summary>
    /// Реализация <see cref="ISubscriptionAccessPolicy"/> — проверяет владение
    /// подпиской или наличие роли Admin у актора.
    /// </summary>
    public sealed class SubscriptionAccessPolicy : ISubscriptionAccessPolicy
    {
        private readonly IUserSubscriptionRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionAccessPolicy"/>.
        /// </summary>
        /// <param name="repository">Репозиторий подписок пользователей.</param>
        public SubscriptionAccessPolicy(IUserSubscriptionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public async Task<Result> AuthorizeOperationAsync(
            Guid subscriptionId,
            Guid actorUserId,
            IReadOnlyCollection<string> actorRoles,
            CancellationToken cancellationToken = default)
        {
            var subscription = await _repository.GetByIdAsync(subscriptionId, cancellationToken);
            if (subscription is null)
            {
                return SubscriptionsErrors.NotFound;
            }

            // Admin имеет доступ к любой подписке (POL-004 §4.1, §4.3).
            if (actorRoles.Contains(PlatformRoles.ADMIN))
            {
                return Result.Success();
            }

            if (subscription.UserId != actorUserId)
            {
                return SubscriptionsErrors.ForbiddenNotOwner;
            }

            return Result.Success();
        }
    }
}
