using GastronomePlatform.Modules.Subscriptions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// EF Core конфигурация для агрегата <see cref="UserSubscription"/>.
    /// Таблица <c>subscriptions.UserSubscriptions</c>: снепшот цены (grandfathering),
    /// счётчики dunning, композиция с <see cref="SubscriptionPayment"/> и
    /// <see cref="SubscriptionAgreement"/> через backing fields.
    /// </summary>
    internal sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<UserSubscription> builder)
        {
            builder.ToTable("UserSubscriptions", t =>
            {
                // Период оплаченного цикла осмысленно ограничен слева и справа.
                t.HasCheckConstraint(
                    "CK_UserSubscriptions_CurrentPeriodOrdered",
                    "\"CurrentPeriodStart\" < \"CurrentPeriodEnd\"");

                // Счётчик dunning не уходит в минус.
                t.HasCheckConstraint(
                    "CK_UserSubscriptions_FailedAttemptsNonNegative",
                    "\"FailedAttempts\" >= 0");
            });

            builder.HasKey(x => x.Id);

            // UserId — кросс-модульная логическая ссылка на users.UserProfiles.UserId;
            // FK в БД сознательно нет (правило модульного монолита).
            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.PlanId)
                .IsRequired();

            builder.Property(x => x.CurrentPriceId)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.SnapshotAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.SnapshotCurrency)
                .IsRequired()
                .HasMaxLength(3)
                .IsFixedLength();

            builder.Property(x => x.StartsAt)
                .IsRequired();

            builder.Property(x => x.CurrentPeriodStart)
                .IsRequired();

            builder.Property(x => x.CurrentPeriodEnd)
                .IsRequired();

            builder.Property(x => x.TrialEnd);

            builder.Property(x => x.AutoRenew)
                .IsRequired();

            builder.Property(x => x.CancelAtPeriodEnd)
                .IsRequired();

            builder.Property(x => x.FailedAttempts)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.NextBillingAt);

            builder.Property(x => x.GatewayPaymentMethodId)
                .HasMaxLength(UserSubscription.MAX_GATEWAY_PAYMENT_METHOD_ID_LENGTH);

            builder.Property(x => x.RecurringDisabledReason);

            builder.Property(x => x.CanceledAt);
            builder.Property(x => x.EndedAt);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            // FK PlanId → SubscriptionPlan: Restrict (защита от потери ссылки на продукт).
            builder.HasOne<SubscriptionPlan>()
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // FK CurrentPriceId → PlanPrice: Restrict (защита grandfathering — цену,
            // по которой кто-то живёт, удалить нельзя).
            builder.HasOne<PlanPrice>()
                .WithMany()
                .HasForeignKey(x => x.CurrentPriceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Композиция с журналом платежей. FK SubscriptionPayment.SubscriptionId → UserSubscription: Cascade.
            builder.HasMany(x => x.Payments)
                .WithOne()
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Payments)
                .HasField("_payments")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // Композиция с версиями оферты. FK SubscriptionAgreement.SubscriptionId → UserSubscription: Cascade.
            builder.HasMany(x => x.Agreements)
                .WithOne()
                .HasForeignKey(a => a.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Agreements)
                .HasField("_agreements")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // Найти все подписки пользователя по статусу (§6.4).
            builder.HasIndex(x => new { x.UserId, x.Status });

            // Индекс для фонового сборщика UC-SUB-200.
            builder.HasIndex(x => x.NextBillingAt);
        }
    }
}
