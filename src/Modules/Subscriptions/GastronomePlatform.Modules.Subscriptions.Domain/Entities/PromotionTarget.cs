using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Таргетинг промоакции — по каким атрибутам подбираются целевые пользователи
    /// (роль / план / конкретный пользователь). Forward-compat заготовка для Phase C.
    /// </summary>
    /// <remarks>
    /// В Phase A — только приватный конструктор для EF Core; фабрика и Update-методы
    /// будут в Phase C вместе с UC-SUB-009.
    /// </remarks>
    public sealed class PromotionTarget : Entity<Guid>
    {
        #region Limits

        /// <summary>Максимальная длина <see cref="TargetValue"/>.</summary>
        public const int MAX_TARGET_VALUE_LENGTH = 100;

        #endregion

        #region Properties

        /// <summary>FK на <see cref="Promotion"/>. <c>OnDelete: Cascade</c>.</summary>
        public Guid PromotionId { get; private set; }

        /// <summary>Тип таргетинга (Role / Plan / User).</summary>
        public PromotionTargetType TargetType { get; private set; }

        /// <summary>
        /// Значение цели: имя роли (<see cref="PromotionTargetType.Role"/>),
        /// <c>PlanId</c> (<see cref="PromotionTargetType.Plan"/>) или
        /// <c>UserId</c> (<see cref="PromotionTargetType.User"/>) — строкой.
        /// </summary>
        public string TargetValue { get; private set; } = string.Empty;

        #endregion

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private PromotionTarget() : base() { }
    }
}
