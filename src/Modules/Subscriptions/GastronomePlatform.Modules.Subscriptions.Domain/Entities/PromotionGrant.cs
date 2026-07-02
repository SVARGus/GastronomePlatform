using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Гранты промоакции — какие услуги (значения <see cref="FeatureGrant"/>)
    /// промо добавляет или убирает у затронутого пользователя.
    /// Forward-compat заготовка для Phase C.
    /// </summary>
    /// <remarks>
    /// Не наследует <c>Entity&lt;TId&gt;</c> — composite PK <c>(PromotionId, Grant)</c>.
    /// В Phase A только приватный конструктор для EF Core; фабрика — Phase C
    /// вместе с UC-SUB-009.
    /// </remarks>
    public sealed class PromotionGrant : IEquatable<PromotionGrant>
    {
        /// <summary>FK на <see cref="Promotion"/>. Часть composite PK. <c>OnDelete: Cascade</c>.</summary>
        public Guid PromotionId { get; private set; }

        /// <summary>Значение <see cref="FeatureGrant"/>. Часть composite PK.</summary>
        public FeatureGrant Grant { get; private set; }

        /// <summary>
        /// <see langword="true"/> — промо добавляет услугу; <see langword="false"/> —
        /// убирает (например, у пользователя роль-привязанный грант, промо его снимает
        /// на срок акции).
        /// </summary>
        public bool IsGrant { get; private set; }

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private PromotionGrant() { }

        /// <inheritdoc/>
        public bool Equals(PromotionGrant? other)
        {
            return other is not null
                && PromotionId == other.PromotionId
                && Grant == other.Grant;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PromotionGrant other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(PromotionId, Grant);
    }
}
