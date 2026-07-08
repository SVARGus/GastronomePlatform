using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan
{
    /// <summary>
    /// Команда создания тарифного плана каталога (UC-SUB-001, admin).
    /// </summary>
    /// <remarks>
    /// Состав грантов настраивается отдельно через UC-SUB-007 (SetPlanGrants) — здесь
    /// только атрибуты продукта. Возвращает <see cref="CreateSubscriptionPlanResult"/>
    /// с идентификатором созданного плана. Обёртка <c>Result&lt;&gt;</c> добавляется
    /// автоматически через <see cref="ICommand{TResponse}"/>.
    /// </remarks>
    /// <param name="PlanKind">
    /// Род плана: <see cref="PlanKind.Base"/> (тарифный уровень, ≤1 активной, несёт роль)
    /// или <see cref="PlanKind.AddOn"/> (докупаемая услуга параллельно Base).
    /// </param>
    /// <param name="PublicName">Публичное название плана для витрины.</param>
    /// <param name="TechnicalName">
    /// Уникальное системное имя (используется в коде/конфигах). Опционально; если задано —
    /// уникальность проверяется на уровне Application до вызова доменной фабрики.
    /// </param>
    /// <param name="Description">Публичное описание плана. Опционально.</param>
    /// <param name="RequiredRole">
    /// Покупочный роль-гейт — порог «не ниже роли», значение из
    /// <see cref="GastronomePlatform.Common.Domain.Constants.PlatformRoles"/>. Только для
    /// <see cref="PlanKind.Base"/>; для <see cref="PlanKind.AddOn"/> должен быть <see langword="null"/>.
    /// </param>
    /// <param name="AvailableFrom">Начало окна доступности продукта. Опционально.</param>
    /// <param name="AvailableUntil">Конец окна доступности продукта. Опционально.</param>
    /// <param name="InternalNotes">Служебные заметки маркетолога. Опционально.</param>
    public sealed record CreateSubscriptionPlanCommand(
        PlanKind PlanKind,
        string PublicName,
        string? TechnicalName,
        string? Description,
        string? RequiredRole,
        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        string? InternalNotes) : ICommand<CreateSubscriptionPlanResult>;
}
