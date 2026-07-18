using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionCatalog
{
    /// <summary>
    /// Витринная карточка тарифного плана (UC-SUB-040 response): что за продукт,
    /// что в него входит и по каким офферам его можно купить.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Наружу отдаётся только витринное. Внутренние поля каталога и биллинга —
    /// <c>TechnicalName</c>, <c>InternalNotes</c>, <c>IsActive</c>, окна доступности —
    /// в DTO не входят: они управляют показом, но сами показу не подлежат.
    /// </para>
    /// <para>
    /// План попадает в выдачу, только если предлагается к покупке
    /// (<c>SubscriptionPlan.IsAvailableAt</c>) и имеет хотя бы один покупаемый оффер.
    /// Поэтому <see cref="Offers"/> непуст по построению.
    /// </para>
    /// </remarks>
    /// <param name="Id">Идентификатор плана — с ним клиент переходит к оформлению.</param>
    /// <param name="PlanKind">Род плана: тарифный уровень (<c>Base</c>) или докупаемая услуга (<c>AddOn</c>).</param>
    /// <param name="PublicName">Витринное название тарифа.</param>
    /// <param name="Description">Витринное описание. <see langword="null"/>, если не задано.</param>
    /// <param name="RequiredRole">
    /// Роль, необходимая для покупки плана; <see langword="null"/> — доступен всем.
    /// Клиент показывает по этому полю пометку «требуется подтверждение статуса».
    /// Право на покупку проверяется при оформлении, а не здесь.
    /// </param>
    /// <param name="Grants">Состав услуг плана — что пользователь получает за деньги.</param>
    /// <param name="Offers">Покупаемые офферы плана: варианты периода и цены.</param>
    public sealed record SubscriptionCatalogPlanResponse(
        Guid Id,
        PlanKind PlanKind,
        string PublicName,
        string? Description,
        string? RequiredRole,
        IReadOnlyList<SubscriptionCatalogGrantResponse> Grants,
        IReadOnlyList<SubscriptionCatalogOfferResponse> Offers);
}
