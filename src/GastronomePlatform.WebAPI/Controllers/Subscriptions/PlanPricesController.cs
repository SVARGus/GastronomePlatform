using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.AddPlanPrice;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Subscriptions
{
    /// <summary>
    /// Контроллер офферов (SKU) в тарифных планах (UC-SUB-004..006, UC-SUB-040).
    /// Phase A содержит только UC-SUB-004 (AddOffer). UC-SUB-005 (UpdateOffer)
    /// и UC-SUB-006 (SetPricing) добавляются далее в этой же группе endpoint-ов.
    /// </summary>
    /// <remarks>
    /// Маршрут вложен под <c>/api/subscription-plans/{planId}</c>, чтобы отразить
    /// иерархию каталога (оффер живёт внутри плана). Ресурс имеет собственный ID
    /// на уровне сущности, поэтому будущие endpoint-ы редактирования оффера могут
    /// адресоваться как <c>/api/plan-prices/{priceId}</c> — отдельный маршрут в этом
    /// же контроллере (задел на UC-SUB-005/006).
    /// </remarks>
    [ApiController]
    [Route("api/subscription-plans/{planId:guid}/prices")]
    public sealed class PlanPricesController : ApiController
    {
        #region Request Models

        /// <summary>
        /// Данные для добавления оффера в план (UC-SUB-004).
        /// </summary>
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
        /// <param name="RenewsAsPriceId">Оффер продления. Опционально.</param>
        /// <param name="FallbackPriceId">Оффер понижения. Опционально.</param>
        /// <param name="AvailableFrom">Начало окна доступности. Опционально.</param>
        /// <param name="AvailableUntil">Конец окна доступности. Опционально.</param>
        /// <param name="InternalNotes">Служебные заметки. Опционально.</param>
        public sealed record AddPlanPriceRequest(
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
            string? InternalNotes);

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="PlanPricesController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public PlanPricesController(ISender sender) : base(sender) { }

        /// <summary>
        /// Добавляет новый оффер в тарифный план (UC-SUB-004).
        /// </summary>
        /// <param name="planId">Идентификатор плана-владельца оффера (из маршрута).</param>
        /// <param name="request">Данные оффера.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с <see cref="AddPlanPriceResult"/>;
        /// <c>400 Bad Request</c> при ошибке валидации / доменного инварианта
        /// (<c>SUBS.PRICE_NEGATIVE_AMOUNT</c>, <c>SUBS.PRICE_TRIAL_REQUIRES_FREE_WITH_DAYS</c>,
        /// <c>SUBS.PRICE_NON_RECURRING_CANNOT_TRANSITION</c>, <c>SUBS.TRANSITION_PRICE_NOT_FOUND</c>,
        /// <c>SUBS.TRANSITION_PRICE_CROSS_PLAN</c>);
        /// <c>401</c> / <c>403</c>;
        /// <c>404 Not Found</c> (<c>SUBS.PLAN_NOT_FOUND</c>), если план не существует.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> AddPriceAsync(
            [FromRoute] Guid planId,
            [FromBody] AddPlanPriceRequest request,
            CancellationToken ct)
        {
            var command = new AddPlanPriceCommand(
                PlanId:           planId,
                Kind:             request.Kind,
                PublicName:       request.PublicName,
                DurationDays:     request.DurationDays,
                Currency:         request.Currency,
                Amount:           request.Amount,
                CompareAtAmount:  request.CompareAtAmount,
                DiscountPercent:  request.DiscountPercent,
                TrialDays:        request.TrialDays,
                IsRecurring:      request.IsRecurring,
                IsPurchasable:    request.IsPurchasable,
                RenewsAsPriceId:  request.RenewsAsPriceId,
                FallbackPriceId:  request.FallbackPriceId,
                AvailableFrom:    request.AvailableFrom,
                AvailableUntil:   request.AvailableUntil,
                InternalNotes:    request.InternalNotes);

            Result<AddPlanPriceResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created($"/api/subscription-plans/{planId}/prices/{result.Value.PriceId}", result.Value);
        }
    }
}
