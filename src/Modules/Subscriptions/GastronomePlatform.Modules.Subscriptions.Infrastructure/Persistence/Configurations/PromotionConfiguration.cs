using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="Promotion"/> — forward-compat заготовка Phase C.
    /// Таблица <c>subscriptions.Promotions</c> создаётся в initial-миграции, но реально
    /// заполняется только при реализации UC-SUB-008.
    /// </summary>
    internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.ToTable("Promotions", t =>
            {
                t.HasCheckConstraint(
                    "CK_Promotions_PeriodOrdered",
                    "\"From\" < \"To\"");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(Promotion.MAX_NAME_LENGTH);

            builder.Property(x => x.Description)
                .HasMaxLength(Promotion.MAX_DESCRIPTION_LENGTH);

            builder.Property(x => x.From)
                .IsRequired();

            builder.Property(x => x.To)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.InternalNotes)
                .HasMaxLength(Promotion.MAX_INTERNAL_NOTES_LENGTH);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();
        }
    }
}
