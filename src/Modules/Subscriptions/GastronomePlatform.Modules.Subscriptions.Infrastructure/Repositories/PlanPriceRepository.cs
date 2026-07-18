using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="IPlanPriceRepository"/> через EF Core.
    /// </summary>
    public sealed class PlanPriceRepository : IPlanPriceRepository
    {
        private readonly SubscriptionsDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PlanPriceRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Subscriptions.</param>
        public PlanPriceRepository(SubscriptionsDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public Task<PlanPrice?> GetByIdAsync(Guid priceId, CancellationToken cancellationToken = default)
            => _context.PlanPrices
                .FirstOrDefaultAsync(p => p.Id == priceId, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PlanPrice>> ListByPlanIdsAsync(
            IReadOnlyCollection<Guid> planIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(planIds);

            if (planIds.Count == 0)
            {
                return Array.Empty<PlanPrice>();
            }

            return await _context.PlanPrices
                .AsNoTracking()
                .Where(p => planIds.Contains(p.PlanId))
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(PlanPrice price, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(price);
            await _context.PlanPrices.AddAsync(price, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
