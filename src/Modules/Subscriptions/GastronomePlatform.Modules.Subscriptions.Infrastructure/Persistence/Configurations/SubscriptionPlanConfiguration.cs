using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="SubscriptionPlan"/>.
    /// Таблица <c>subscriptions.SubscriptionPlans</c>: атрибуты продукта каталога,
    /// покупочный роль-гейт, окно доступности, состав грантов (композиция с
    /// <see cref="PlanGrant"/> через backing field <c>_grants</c>).
    /// </summary>
    internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.ToTable("SubscriptionPlans");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PlanKind)
                .IsRequired();

            builder.Property(x => x.PublicName)
                .IsRequired()
                .HasMaxLength(SubscriptionPlan.MAX_PUBLIC_NAME_LENGTH);

            builder.Property(x => x.TechnicalName)
                .HasMaxLength(SubscriptionPlan.MAX_TECHNICAL_NAME_LENGTH);

            builder.Property(x => x.Description)
                .HasMaxLength(SubscriptionPlan.MAX_DESCRIPTION_LENGTH);

            // Покупочный роль-гейт. Значения — из PlatformRoles.
            builder.Property(x => x.RequiredRole)
                .HasMaxLength(100);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.AvailableFrom);
            builder.Property(x => x.AvailableUntil);

            builder.Property(x => x.InternalNotes)
                .HasMaxLength(SubscriptionPlan.MAX_INTERNAL_NOTES_LENGTH);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // Уникальность системного имени — только для непустых значений.
            // Postgres partial unique index: WHERE "TechnicalName" IS NOT NULL.
            builder.HasIndex(x => x.TechnicalName)
                .IsUnique()
                .HasFilter("\"TechnicalName\" IS NOT NULL");

            // Композиция с грантами. FK PlanGrant.PlanId → SubscriptionPlan.Id: Cascade (см. §8 domain-model).
            builder.HasMany(x => x.Grants)
                .WithOne()
                .HasForeignKey(pg => pg.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Grants)
                .HasField("_grants")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
