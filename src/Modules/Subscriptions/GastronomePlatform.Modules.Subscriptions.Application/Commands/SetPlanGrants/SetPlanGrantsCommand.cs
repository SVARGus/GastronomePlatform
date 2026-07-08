using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.SetPlanGrants
{
    /// <summary>
    /// Команда полной замены состава грантов тарифного плана (UC-SUB-007, admin).
    /// </summary>
    /// <remarks>
    /// Replace-семантика: <c>plan.SetGrants(...)</c> внутри полностью очищает существующий
    /// набор грантов и заполняет заново. Пустой <paramref name="Grants"/> — валидный
    /// сценарий (снимает все гранты). Уникальность значений <c>FeatureGrant</c>
    /// проверяется валидатором (запрет дубликатов в списке). Реализует <see cref="ICommand"/>
    /// без generic — команда не возвращает нового значения, только <c>Result</c>.
    /// </remarks>
    /// <param name="PlanId">Идентификатор плана.</param>
    /// <param name="Grants">Новый состав грантов. Пустой список = снять все гранты.</param>
    public sealed record SetPlanGrantsCommand(
        Guid PlanId,
        IReadOnlyList<PlanGrantSpec> Grants) : ICommand;

    /// <summary>
    /// Спецификация одного гранта в составе команды <see cref="SetPlanGrantsCommand"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Quantity"/> = <see langword="null"/> — безлимит (для квотовых грантов)
    /// либо неприменимо (для не-квотовых). По Phase A квотовым является только
    /// <see cref="FeatureGrant.PromotionAdvanced"/>; для остальных грантов
    /// <see cref="Quantity"/> обязан быть <see langword="null"/> — проверка в хендлере
    /// (<c>SUBS.PLAN_GRANT_QUOTA_NOT_APPLICABLE</c>).
    /// </remarks>
    /// <param name="Grant">Значение <see cref="FeatureGrant"/>.</param>
    /// <param name="Quantity">Квота права. Опционально.</param>
    public sealed record PlanGrantSpec(FeatureGrant Grant, int? Quantity);
}
