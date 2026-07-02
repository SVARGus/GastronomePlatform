using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="PlanGrant"/>.
    /// Join-таблица <c>subscriptions.PlanGrants</c> с composite PK
    /// <c>(PlanId, Grant)</c>. FK <c>PlanId</c> и связь с
    /// <see cref="SubscriptionPlan"/> настраиваются в
    /// <see cref="SubscriptionPlanConfiguration"/>.
    /// </summary>
    internal sealed class PlanGrantConfiguration : IEntityTypeConfiguration<PlanGrant>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<PlanGrant> builder)
        {
            builder.ToTable("PlanGrants");

            // Composite PK; уникальность гранта в плане гарантируется ключом.
            builder.HasKey(x => new { x.PlanId, x.Grant });

            builder.Property(x => x.PlanId)
                .IsRequired();

            builder.Property(x => x.Grant)
                .IsRequired();

            // Квота: null = безлимит либо неприменимо для не-квотовых грантов.
            builder.Property(x => x.Quantity);
        }
    }
}
