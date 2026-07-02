using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="PlanPrice"/>.
    /// Таблица <c>subscriptions.PlanPrices</c>: оффер каталога с денежными полями
    /// (<c>HasPrecision(18, 2)</c>), self-FK переходами <c>RenewsAs</c>/<c>Fallback</c>
    /// и CHECK-constraints, зафиксированными в domain-model §9.
    /// </summary>
    internal sealed class PlanPriceConfiguration : IEntityTypeConfiguration<PlanPrice>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<PlanPrice> builder)
        {
            builder.ToTable("PlanPrices", t =>
            {
                // Defense-in-depth поверх Domain-инварианта Amount >= 0.
                t.HasCheckConstraint(
                    "CK_PlanPrices_AmountNonNegative",
                    "\"Amount\" >= 0");

                // Trial ⇒ Amount = 0 и TrialDays IS NOT NULL. Значение OfferKind.Trial = 0.
                t.HasCheckConstraint(
                    "CK_PlanPrices_TrialRequiresFreeWithDays",
                    "\"Kind\" <> 0 OR (\"Amount\" = 0 AND \"TrialDays\" IS NOT NULL)");

                // Окно доступности согласовано, когда обе границы заданы.
                t.HasCheckConstraint(
                    "CK_PlanPrices_AvailabilityWindow",
                    "\"AvailableFrom\" IS NULL OR \"AvailableUntil\" IS NULL OR \"AvailableFrom\" < \"AvailableUntil\"");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PlanId)
                .IsRequired();

            builder.Property(x => x.Kind)
                .IsRequired();

            builder.Property(x => x.PublicName)
                .HasMaxLength(PlanPrice.MAX_PUBLIC_NAME_LENGTH);

            builder.Property(x => x.DurationDays);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(PlanPrice.CURRENCY_LENGTH)
                .IsFixedLength();

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.CompareAtAmount)
                .HasPrecision(18, 2);

            builder.Property(x => x.DiscountPercent);
            builder.Property(x => x.TrialDays);

            builder.Property(x => x.IsRecurring)
                .IsRequired();

            builder.Property(x => x.IsPurchasable)
                .IsRequired();

            builder.Property(x => x.RenewsAsPriceId);
            builder.Property(x => x.FallbackPriceId);

            builder.Property(x => x.AvailableFrom);
            builder.Property(x => x.AvailableUntil);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.InternalNotes)
                .HasMaxLength(PlanPrice.MAX_INTERNAL_NOTES_LENGTH);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // FK на план: Restrict (план с офферами не удаляют, гасят через IsActive).
            builder.HasOne<SubscriptionPlan>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-FK: RenewsAs — нельзя удалить оффер, на который ссылаются как цель продления.
            builder.HasOne<PlanPrice>()
                .WithMany()
                .HasForeignKey(x => x.RenewsAsPriceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-FK: Fallback — нельзя удалить оффер, на который ссылаются как цель понижения.
            builder.HasOne<PlanPrice>()
                .WithMany()
                .HasForeignKey(x => x.FallbackPriceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Индекс для запросов витрины по плану и активности.
            builder.HasIndex(x => new { x.PlanId, x.IsActive });
        }
    }
}
