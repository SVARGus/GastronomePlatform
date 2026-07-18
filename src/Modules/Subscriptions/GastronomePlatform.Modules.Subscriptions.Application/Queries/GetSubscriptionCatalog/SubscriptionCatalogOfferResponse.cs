using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionCatalog
{
    /// <summary>
    /// Витринный оффер плана (UC-SUB-040 response) — вариант периода и цены,
    /// который пользователь выбирает при оформлении.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Наружу не отдаются служебные поля оффера: флаги <c>IsPurchasable</c>/<c>IsActive</c>
    /// и окна доступности (по ним выполнен отбор — непокупаемый оффер в выдачу
    /// не попадает), переходы <c>RenewsAsPriceId</c>/<c>FallbackPriceId</c> (внутренняя
    /// механика продления и dunning) и <c>InternalNotes</c>.
    /// </para>
    /// <para>
    /// <see cref="CompareAtAmount"/> и <see cref="DiscountPercent"/> — витринные
    /// метаданные для показа выгоды. Источником истины для списания остаётся
    /// <see cref="Amount"/>: биллинг считает по нему, а не по проценту скидки.
    /// </para>
    /// </remarks>
    /// <param name="Id">Идентификатор оффера — передаётся при оформлении подписки.</param>
    /// <param name="Kind">Природа оффера: пробный, вводный, обычный и т. д.</param>
    /// <param name="PublicName">Витринное имя оффера («Год со скидкой 25%»). <see langword="null"/>, если не задано.</param>
    /// <param name="Amount">Сумма списания за период — источник истины для биллинга.</param>
    /// <param name="Currency">Код валюты (ISO 4217).</param>
    /// <param name="CompareAtAmount">«Старая цена» для зачёркнутого показа. <see langword="null"/>, если скидки нет.</param>
    /// <param name="DiscountPercent">Размер скидки в процентах для витрины. <see langword="null"/>, если не задан.</param>
    /// <param name="DurationDays">Длительность оплаченного периода в днях. <see langword="null"/> — бессрочный оффер.</param>
    /// <param name="TrialDays">Длительность пробного периода в днях. Заполнен у пробных офферов.</param>
    /// <param name="IsRecurring">Продлевается ли автоматически по окончании периода.</param>
    public sealed record SubscriptionCatalogOfferResponse(
        Guid Id,
        OfferKind Kind,
        string? PublicName,
        decimal Amount,
        string Currency,
        decimal? CompareAtAmount,
        int? DiscountPercent,
        int? DurationDays,
        int? TrialDays,
        bool IsRecurring);
}
