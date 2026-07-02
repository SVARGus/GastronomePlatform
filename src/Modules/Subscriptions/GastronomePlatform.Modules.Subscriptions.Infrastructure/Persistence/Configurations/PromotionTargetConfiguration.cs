using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="PromotionTarget"/> — forward-compat заготовка Phase C.
    /// Таблица <c>subscriptions.PromotionTargets</c>. FK <c>PromotionId → Promotion (Cascade)</c>.
    /// UNIQUE <c>(PromotionId, TargetType, TargetValue)</c> предотвращает дубли таргетов.
    /// </summary>
    internal sealed class PromotionTargetConfiguration : IEntityTypeConfiguration<PromotionTarget>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<PromotionTarget> builder)
        {
            builder.ToTable("PromotionTargets");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PromotionId)
                .IsRequired();

            builder.Property(x => x.TargetType)
                .IsRequired();

            builder.Property(x => x.TargetValue)
                .IsRequired()
                .HasMaxLength(PromotionTarget.MAX_TARGET_VALUE_LENGTH);

            builder.HasOne<Promotion>()
                .WithMany()
                .HasForeignKey(x => x.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.PromotionId, x.TargetType, x.TargetValue })
                .IsUnique();
        }
    }
}
