namespace GastronomePlatform.Modules.Subscriptions.Domain.Enums
{
    /// <summary>
    /// Тип таргетинга промоакции: по каким атрибутам подбираются
    /// целевые пользователи. Хранится как <c>int</c> в БД.
    /// Используется в <c>PromotionTarget.TargetType</c>.
    /// </summary>
    /// <remarks>
    /// Значение цели хранится в поле <c>PromotionTarget.TargetValue</c> (строкой):
    /// имя роли (<see cref="Role"/>), <c>PlanId</c> (<see cref="Plan"/>)
    /// или <c>UserId</c> (<see cref="User"/>).
    /// </remarks>
    public enum PromotionTargetType
    {
        /// <summary>Промо действует на всех пользователей с указанной ролью.</summary>
        Role = 0,

        /// <summary>Промо действует на держателей указанного плана.</summary>
        Plan = 1,

        /// <summary>Промо выдано конкретному пользователю.</summary>
        User = 2
    }
}
