using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для <see cref="SubscriptionPayment"/>.
    /// Таблица <c>subscriptions.SubscriptionPayments</c>: журнал попыток списания.
    /// <c>GatewayTransactionId</c> — partial UNIQUE (когда не NULL) для
    /// идемпотентности webhook. <c>GatewayPayload</c> хранится в PostgreSQL как <c>jsonb</c>.
    /// </summary>
    internal sealed class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
        {
            builder.ToTable("SubscriptionPayments", t =>
            {
                t.HasCheckConstraint(
                    "CK_SubscriptionPayments_AmountNonNegative",
                    "\"Amount\" >= 0");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.SubscriptionId)
                .IsRequired();

            builder.Property(x => x.PriceId)
                .IsRequired();

            builder.Property(x => x.Purpose)
                .IsRequired();

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(SubscriptionPayment.CURRENCY_LENGTH)
                .IsFixedLength();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.AttemptNumber)
                .IsRequired();

            builder.Property(x => x.GatewayTransactionId)
                .HasMaxLength(SubscriptionPayment.MAX_GATEWAY_TRANSACTION_ID_LENGTH);

            builder.Property(x => x.GatewayPayload)
                .HasColumnType("jsonb");

            builder.Property(x => x.FailureReason)
                .HasMaxLength(SubscriptionPayment.MAX_FAILURE_REASON_LENGTH);

            builder.Property(x => x.OccurredAt)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            // FK PriceId → PlanPrice: Restrict (сохранить ссылку на оффер для аудита).
            // FK SubscriptionId → UserSubscription (Cascade) настроен в UserSubscriptionConfiguration.
            builder.HasOne<PlanPrice>()
                .WithMany()
                .HasForeignKey(x => x.PriceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Идемпотентность webhook: у каждой транзакции шлюза — не более одной
            // записи. Partial UNIQUE, потому что для Pending / оффлайновых ошибок
            // поле остаётся NULL.
            builder.HasIndex(x => x.GatewayTransactionId)
                .IsUnique()
                .HasFilter("\"GatewayTransactionId\" IS NOT NULL");

            // Для быстрого запроса истории платежей подписки.
            builder.HasIndex(x => new { x.SubscriptionId, x.OccurredAt });
        }
    }
}
