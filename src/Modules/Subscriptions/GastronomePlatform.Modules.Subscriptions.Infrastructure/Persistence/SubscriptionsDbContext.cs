using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext модуля Subscriptions.
    /// Работает со схемой <c>subscriptions</c> базы данных PostgreSQL.
    /// </summary>
    public sealed class SubscriptionsDbContext : DbContext
    {
        // DbSet-ы будут добавлены по мере появления Domain-сущностей Phase A:
        //   SubscriptionPlan, PlanGrant, PlanPrice,
        //   UserSubscription, SubscriptionPayment, SubscriptionAgreement,
        //   и forward-compat пустые Promotion, PromotionTarget, PromotionGrant.

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionsDbContext"/>.
        /// </summary>
        /// <param name="options">Параметры конфигурации DbContext.</param>
        public SubscriptionsDbContext(DbContextOptions<SubscriptionsDbContext> options) : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriptionsDbContext).Assembly);

            modelBuilder.HasDefaultSchema("subscriptions");
        }
    }
}
