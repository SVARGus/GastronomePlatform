using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="IUserSubscriptionRepository"/> через EF Core.
    /// </summary>
    public sealed class UserSubscriptionRepository : IUserSubscriptionRepository
    {
        private readonly SubscriptionsDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserSubscriptionRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Subscriptions.</param>
        public UserSubscriptionRepository(SubscriptionsDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<UserSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.UserSubscriptions
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FeatureGrant>> ListActiveGrantsByUserAsync(
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default)
        {
            var query =
                from subscription in _context.UserSubscriptions
                where subscription.UserId == userId
                      && (subscription.Status == SubscriptionStatus.Trialing
                          || subscription.Status == SubscriptionStatus.Active
                          || subscription.Status == SubscriptionStatus.PastDue
                          || subscription.Status == SubscriptionStatus.Canceled)
                      && subscription.CurrentPeriodEnd > utcNow
                join grant in _context.PlanGrants
                    on subscription.PlanId equals grant.PlanId
                select grant.Grant;

            return await query
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<bool> HasActiveBaseAsync(
            Guid userId,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken = default)
        {
            var query =
                from subscription in _context.UserSubscriptions
                where subscription.UserId == userId
                      && (subscription.Status == SubscriptionStatus.Trialing
                          || subscription.Status == SubscriptionStatus.Active
                          || subscription.Status == SubscriptionStatus.PastDue
                          || subscription.Status == SubscriptionStatus.Canceled)
                      && subscription.CurrentPeriodEnd > utcNow
                join plan in _context.SubscriptionPlans
                    on subscription.PlanId equals plan.Id
                where plan.PlanKind == PlanKind.Base
                select subscription.Id;

            return query.AnyAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ExpirationCandidate>> ListExpirationCandidatesAsync(
            DateTimeOffset utcNow,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            var query =
                from subscription in _context.UserSubscriptions
                where (subscription.Status == SubscriptionStatus.Trialing
                       || subscription.Status == SubscriptionStatus.Active
                       || subscription.Status == SubscriptionStatus.Canceled)
                      && subscription.CurrentPeriodEnd <= utcNow
                join plan in _context.SubscriptionPlans
                    on subscription.PlanId equals plan.Id
                orderby subscription.CurrentPeriodEnd
                select new ExpirationCandidate(subscription.Id, plan.PlanKind);

            return await query
                .Take(batchSize)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<UserSubscription>> ListByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ids);

            if (ids.Count == 0)
            {
                return Array.Empty<UserSubscription>();
            }

            return await _context.UserSubscriptions
                .Where(s => ids.Contains(s.Id))
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(subscription);
            await _context.UserSubscriptions.AddAsync(subscription, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
