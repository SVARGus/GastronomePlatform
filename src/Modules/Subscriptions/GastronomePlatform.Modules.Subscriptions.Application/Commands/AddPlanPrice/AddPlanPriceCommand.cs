using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.AddPlanPrice
{
    /// <summary>
    /// Команда добавления оффера (SKU) в тарифный план (UC-SUB-004, admin).
    /// </summary>
    /// <remarks>
    /// Внутриполевые инварианты оффера проверяются доменной фабрикой
    /// <c>PlanPrice.Create</c> (Amount ≥ 0, Trial ⇒ Amount=0 &amp; TrialDays.HasValue,
    /// !IsRecurring ⇒ переходы null). Cross-field инварианты цепочек
    /// (<c>RenewsAsPriceId</c>/<c>FallbackPriceId</c> той же <c>PlanId</c>,
    /// существование целевых офферов) проверяются в хендлере через
    /// <c>IPlanPriceRepository</c>.
    /// </remarks>
    /// <param name="PlanId">Идентификатор плана-владельца оффера.</param>
    /// <param name="Kind">Природа оффера (Trial / Intro / Standard / Retention / DunningFallback).</param>
    /// <param name="PublicName">Витринное имя оффера. Опционально.</param>
    /// <param name="DurationDays">Длительность периода в днях. <see langword="null"/> = бессрочный.</param>
    /// <param name="Currency">Код валюты (ISO 4217, 3 символа).</param>
    /// <param name="Amount">Сумма списания за период.</param>
    /// <param name="CompareAtAmount">«Старая цена» для витрины. Опционально.</param>
    /// <param name="DiscountPercent">Скидка (%) для витрины. Опционально.</param>
    /// <param name="TrialDays">Дней триала. Обязателен для <see cref="OfferKind.Trial"/>.</param>
    /// <param name="IsRecurring">Поддерживает ли автопродление.</param>
    /// <param name="IsPurchasable">Можно ли купить напрямую.</param>
    /// <param name="RenewsAsPriceId">Оффер продления той же <paramref name="PlanId"/>. Нельзя задать при <paramref name="IsRecurring"/>=false.</param>
    /// <param name="FallbackPriceId">Оффер понижения той же <paramref name="PlanId"/>. Нельзя задать при <paramref name="IsRecurring"/>=false.</param>
    /// <param name="AvailableFrom">Начало окна доступности. Опционально.</param>
    /// <param name="AvailableUntil">Конец окна доступности. Опционально.</param>
    /// <param name="InternalNotes">Служебные заметки. Опционально.</param>
    public sealed record AddPlanPriceCommand(
        Guid PlanId,
        OfferKind Kind,
        string? PublicName,
        int? DurationDays,
        string Currency,
        decimal Amount,
        decimal? CompareAtAmount,
        int? DiscountPercent,
        int? TrialDays,
        bool IsRecurring,
        bool IsPurchasable,
        Guid? RenewsAsPriceId,
        Guid? FallbackPriceId,
        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        string? InternalNotes) : ICommand<AddPlanPriceResult>;
}
