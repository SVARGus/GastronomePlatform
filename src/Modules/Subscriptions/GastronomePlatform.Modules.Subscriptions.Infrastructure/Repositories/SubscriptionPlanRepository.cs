using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="ISubscriptionPlanRepository"/> через EF Core.
    /// </summary>
    public sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly SubscriptionsDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionPlanRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Subscriptions.</param>
        public SubscriptionPlanRepository(SubscriptionsDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public Task<bool> TechnicalNameExistsAsync(string technicalName, CancellationToken cancellationToken = default)
            => _context.SubscriptionPlans
                .AnyAsync(p => p.TechnicalName == technicalName, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(plan);
            await _context.SubscriptionPlans.AddAsync(plan, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
