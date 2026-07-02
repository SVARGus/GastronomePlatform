using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Связь плана с услугой (грантом) — позиция в join-таблице между
    /// <see cref="SubscriptionPlan"/> и значением enum <see cref="FeatureGrant"/>.
    /// Часть композиции <see cref="SubscriptionPlan"/>.
    /// </summary>
    /// <remarks>
    /// Не наследует <c>Entity&lt;TId&gt;</c> — composite PK <c>(PlanId, Grant)</c>.
    /// Создание возможно только из <see cref="SubscriptionPlan.SetGrants"/>.
    /// </remarks>
    public sealed class PlanGrant : IEquatable<PlanGrant>
    {
        /// <summary>Идентификатор плана. Часть composite PK.</summary>
        public Guid PlanId { get; private set; }

        /// <summary>Значение enum <see cref="FeatureGrant"/>. Часть composite PK.</summary>
        public FeatureGrant Grant { get; private set; }

        /// <summary>
        /// Квота права для квотовых грантов (напр. <see cref="FeatureGrant.PromotionAdvanced"/>
        /// — число блюд). <see langword="null"/> = безлимит либо неприменимо для не-квотовых.
        /// Резолвер эффективных грантов суммирует квоту по всем активным подпискам.
        /// </summary>
        public int? Quantity { get; private set; }

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private PlanGrant() { }

        /// <summary>
        /// Создаёт связь плана с грантом. Вызывается только из
        /// <see cref="SubscriptionPlan.SetGrants"/>.
        /// </summary>
        /// <param name="planId">Идентификатор плана.</param>
        /// <param name="grant">Значение <see cref="FeatureGrant"/>.</param>
        /// <param name="quantity">Квота, если применимо.</param>
        internal PlanGrant(Guid planId, FeatureGrant grant, int? quantity)
        {
            PlanId = planId;
            Grant = grant;
            Quantity = quantity;
        }

        /// <inheritdoc/>
        public bool Equals(PlanGrant? other)
        {
            return other is not null
                && PlanId == other.PlanId
                && Grant == other.Grant;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PlanGrant other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(PlanId, Grant);
    }
}
