using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="PromotionGrant"/> — forward-compat заготовка Phase C.
    /// Таблица <c>subscriptions.PromotionGrants</c> с composite PK
    /// <c>(PromotionId, Grant)</c>. FK <c>PromotionId → Promotion (Cascade)</c>.
    /// </summary>
    internal sealed class PromotionGrantConfiguration : IEntityTypeConfiguration<PromotionGrant>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<PromotionGrant> builder)
        {
            builder.ToTable("PromotionGrants");

            builder.HasKey(x => new { x.PromotionId, x.Grant });

            builder.Property(x => x.PromotionId)
                .IsRequired();

            builder.Property(x => x.Grant)
                .IsRequired();

            builder.Property(x => x.IsGrant)
                .IsRequired();

            builder.HasOne<Promotion>()
                .WithMany()
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
