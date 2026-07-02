using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="SubscriptionAgreement"/>.
    /// Таблица <c>subscriptions.SubscriptionAgreements</c>: иммутабельные версии оферты
    /// подписки. <c>TermsSnapshot</c> — jsonb; UNIQUE <c>(SubscriptionId, Version)</c>.
    /// FK <c>SubscriptionId → UserSubscription (Cascade)</c> настроен на стороне
    /// <see cref="UserSubscriptionConfiguration"/>.
    /// </summary>
    internal sealed class SubscriptionAgreementConfiguration : IEntityTypeConfiguration<SubscriptionAgreement>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<SubscriptionAgreement> builder)
        {
            builder.ToTable("SubscriptionAgreements");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.SubscriptionId)
                .IsRequired();

            builder.Property(x => x.Version)
                .IsRequired();

            builder.Property(x => x.ChangeType)
                .IsRequired();

            builder.Property(x => x.TermsSnapshot)
                .IsRequired()
                .HasColumnType("jsonb");

            builder.Property(x => x.DocumentNumber)
                .HasMaxLength(SubscriptionAgreement.MAX_DOCUMENT_NUMBER_LENGTH);

            builder.Property(x => x.ContentHash)
                .HasMaxLength(SubscriptionAgreement.CONTENT_HASH_LENGTH)
                .IsFixedLength();

            builder.Property(x => x.AcceptedAt);

            builder.Property(x => x.EffectiveAt)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            // Уникальная пара (подписка, номер версии) — гарантия append-only порядка.
            builder.HasIndex(x => new { x.SubscriptionId, x.Version })
                .IsUnique();
        }
    }
}
