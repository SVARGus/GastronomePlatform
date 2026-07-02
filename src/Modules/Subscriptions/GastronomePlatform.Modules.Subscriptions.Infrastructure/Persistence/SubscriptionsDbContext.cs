using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext модуля Subscriptions.
    /// Работает со схемой <c>subscriptions</c> базы данных PostgreSQL.
    /// </summary>
    public sealed class SubscriptionsDbContext : DbContext
    {
        /// <summary>Тарифные планы каталога.</summary>
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

        /// <summary>Состав грантов планов.</summary>
        public DbSet<PlanGrant> PlanGrants => Set<PlanGrant>();

        /// <summary>Офферы (SKU) внутри планов.</summary>
        public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();

        /// <summary>Подписки пользователей.</summary>
        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

        /// <summary>Журнал платежей подписок.</summary>
        public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();

        /// <summary>Иммутабельные версии оферты подписок.</summary>
        public DbSet<SubscriptionAgreement> SubscriptionAgreements => Set<SubscriptionAgreement>();

        /// <summary>Промоакции (forward-compat, Phase C).</summary>
        public DbSet<Promotion> Promotions => Set<Promotion>();

        /// <summary>Таргетинг промоакций (forward-compat, Phase C).</summary>
        public DbSet<PromotionTarget> PromotionTargets => Set<PromotionTarget>();

        /// <summary>Изменяемые гранты промоакций (forward-compat, Phase C).</summary>
        public DbSet<PromotionGrant> PromotionGrants => Set<PromotionGrant>();

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

            modelBuilder.HasDefaultSchema("subscriptions");

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubscriptionsDbContext).Assembly);
        }
    }
}
