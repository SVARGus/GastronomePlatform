using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Subscriptions
{
    /// <summary>
    /// Контроллер каталога тарифных планов (UC-SUB-001..003, UC-SUB-040). Phase A
    /// содержит только UC-SUB-001 (Create). Витрина каталога (UC-SUB-040), правка
    /// (UC-SUB-002) и деактивация (UC-SUB-003) добавляются в Phase C.
    /// </summary>
    [ApiController]
    [Route("api/subscription-plans")]
    public sealed class SubscriptionPlansController : ApiController
    {
        #region Request Models

        /// <summary>
        /// Данные для создания тарифного плана (UC-SUB-001).
        /// </summary>
        /// <param name="PlanKind">Род плана (Base / AddOn).</param>
        /// <param name="PublicName">Публичное название плана.</param>
        /// <param name="TechnicalName">Системное имя. Опционально.</param>
        /// <param name="Description">Публичное описание. Опционально.</param>
        /// <param name="RequiredRole">Покупочный роль-гейт (только для Base). Опционально.</param>
        /// <param name="AvailableFrom">Начало окна доступности. Опционально.</param>
        /// <param name="AvailableUntil">Конец окна доступности. Опционально.</param>
        /// <param name="InternalNotes">Служебные заметки маркетолога. Опционально.</param>
        public sealed record CreateSubscriptionPlanRequest(
            PlanKind PlanKind,
            string PublicName,
            string? TechnicalName,
            string? Description,
            string? RequiredRole,
            DateTimeOffset? AvailableFrom,
            DateTimeOffset? AvailableUntil,
            string? InternalNotes);

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionPlansController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public SubscriptionPlansController(ISender sender) : base(sender) { }

        /// <summary>
        /// Создаёт новый тарифный план (UC-SUB-001). Состав грантов настраивается
        /// отдельно (UC-SUB-007).
        /// </summary>
        /// <param name="request">Данные плана.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с <see cref="CreateSubscriptionPlanResult"/>;
        /// <c>400 Bad Request</c> при ошибке валидации (в том числе доменного инварианта
        /// <c>SUBS.ADDON_CANNOT_HAVE_ROLE</c>);
        /// <c>401</c> / <c>403</c>;
        /// <c>409 Conflict</c> (<c>SUBS.TECHNICAL_NAME_TAKEN</c>) при коллизии
        /// системного имени.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateSubscriptionPlanRequest request,
            CancellationToken ct)
        {
            var command = new CreateSubscriptionPlanCommand(
                PlanKind:       request.PlanKind,
                PublicName:     request.PublicName,
                TechnicalName:  request.TechnicalName,
                Description:    request.Description,
                RequiredRole:   request.RequiredRole,
                AvailableFrom:  request.AvailableFrom,
                AvailableUntil: request.AvailableUntil,
                InternalNotes:  request.InternalNotes);

            Result<CreateSubscriptionPlanResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created($"/api/subscription-plans/{result.Value.PlanId}", result.Value);
        }
    }
}
