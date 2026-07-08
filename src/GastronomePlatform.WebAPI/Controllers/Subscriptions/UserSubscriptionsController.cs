using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.Subscribe;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Subscriptions
{
    /// <summary>
    /// Контроллер операций пользователя над своими подписками (UC-SUB-020..025).
    /// Phase A содержит только UC-SUB-020 (Subscribe). Просмотр, отмена, смена
    /// способа оплаты и реактивация добавляются далее.
    /// </summary>
    [ApiController]
    [Route("api/user-subscriptions")]
    public sealed class UserSubscriptionsController : ApiController
    {
        #region Request Models

        /// <summary>
        /// Данные для оформления новой подписки (UC-SUB-020).
        /// </summary>
        /// <param name="PriceId">Идентификатор оффера (<c>PlanPrice</c>) для покупки.</param>
        /// <param name="PaymentMethodId">Токен способа оплаты (получен от шлюза на клиенте).</param>
        /// <param name="AcceptedTermsAt">Момент явного акта согласия пользователя с офертой.</param>
        public sealed record SubscribeRequest(
            Guid PriceId,
            string PaymentMethodId,
            DateTimeOffset AcceptedTermsAt);

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserSubscriptionsController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public UserSubscriptionsController(ISender sender) : base(sender) { }

        /// <summary>
        /// Оформляет новую подписку (UC-SUB-020). Идентификатор пользователя берётся
        /// из JWT (<c>ICurrentUserService.UserId</c>) — клиент не может передать чужой.
        /// </summary>
        /// <param name="request">Данные оформления.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с <see cref="SubscribeResult"/>;
        /// <c>400 Bad Request</c> при ошибке валидации или доменного инварианта
        /// (<c>SUBS.ACTIVATE_TRIAL_REQUIRES_TRIAL_DAYS</c>,
        /// <c>SUBS.ACTIVATE_PAID_REQUIRES_DURATION_DAYS</c>);
        /// <c>401</c> без JWT;
        /// <c>403 Forbidden</c> (<c>SUBS.FORBIDDEN_ROLE_REQUIRED</c>) при непройденном
        /// покупочном роль-гейте POL-004 §4.2;
        /// <c>404 Not Found</c> (<c>SUBS.PRICE_NOT_FOUND</c>, <c>SUBS.PLAN_NOT_FOUND</c>);
        /// <c>409 Conflict</c> (<c>SUBS.ALREADY_HAS_BASE</c>);
        /// <c>400 Bad Request</c> (<c>SUBS.OFFER_NOT_PURCHASABLE</c>) — оффер недоступен к покупке.
        /// </returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubscribeAsync(
            [FromBody] SubscribeRequest request,
            CancellationToken ct)
        {
            var command = new SubscribeCommand(
                PriceId:         request.PriceId,
                PaymentMethodId: request.PaymentMethodId,
                AcceptedTermsAt: request.AcceptedTermsAt);

            Result<SubscribeResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created($"/api/user-subscriptions/{result.Value.SubscriptionId}", result.Value);
        }
    }
}
