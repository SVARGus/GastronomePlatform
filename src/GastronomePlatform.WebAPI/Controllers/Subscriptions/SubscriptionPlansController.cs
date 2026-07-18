using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.CreateSubscriptionPlan;
using GastronomePlatform.Modules.Subscriptions.Application.Commands.SetPlanGrants;
using GastronomePlatform.Modules.Subscriptions.Application.Queries.GetSubscriptionCatalog;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Subscriptions
{
    /// <summary>
    /// Контроллер каталога тарифных планов (UC-SUB-001..003, UC-SUB-007, UC-SUB-040).
    /// Реализованы витрина (UC-SUB-040), создание плана (UC-SUB-001) и настройка
    /// грантов (UC-SUB-007); правка (UC-SUB-002) и деактивация (UC-SUB-003) —
    /// в Phase C. Гранты остаются в этом контроллере, так как не имеют
    /// самостоятельной идентичности (composite PK <c>PlanId + Grant</c>);
    /// офферы вынесены в отдельный <c>PlanPricesController</c>.
    /// </summary>
    /// <remarks>
    /// Права различаются на уровне метода, а не контроллера: чтение каталога
    /// публично, изменение — только для роли <c>Admin</c>. Разделять витрину
    /// и администрирование по разным контроллерам не стали — ресурс один и тот же,
    /// и REST-разбиение по правам вместо ресурсов усложнило бы маршруты.
    /// </remarks>
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

        /// <summary>
        /// Данные для полной замены состава грантов плана (UC-SUB-007).
        /// </summary>
        /// <param name="Grants">Новый состав грантов. Пустой список = снять все гранты.</param>
        public sealed record SetPlanGrantsRequest(IReadOnlyList<PlanGrantItemRequest> Grants);

        /// <summary>
        /// Спецификация одного гранта в запросе <see cref="SetPlanGrantsRequest"/>.
        /// </summary>
        /// <param name="Grant">Значение <see cref="FeatureGrant"/> (enum).</param>
        /// <param name="Quantity">Квота права. Опционально; должно быть <see langword="null"/> для не-квотовых грантов.</param>
        public sealed record PlanGrantItemRequest(FeatureGrant Grant, int? Quantity);

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionPlansController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public SubscriptionPlansController(ISender sender) : base(sender) { }

        /// <summary>
        /// Возвращает витрину каталога подписок (UC-SUB-040): планы, доступные
        /// к покупке, с составом услуг и вариантами оплаты.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Публичный эндпоинт — доступен гостям. Ответ не зависит от того, кто
        /// спрашивает: витрина информирует, но не авторизует.
        /// </para>
        /// <para>
        /// В выдачу попадают только планы, предлагаемые к покупке и имеющие хотя бы
        /// один покупаемый оффер. План с заполненным <c>RequiredRole</c> показывается
        /// всем — по этому полю клиент выводит пометку о необходимости подтвердить
        /// статус; фактическая проверка права выполняется при оформлении (UC-SUB-020)
        /// и может вернуть <c>403</c> (<c>SUBS.FORBIDDEN_ROLE_REQUIRED</c>).
        /// </para>
        /// </remarks>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> со списком <see cref="SubscriptionCatalogPlanResponse"/>.
        /// Пустой каталог — пустой список, а не <c>404</c>.
        /// </returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetCatalogAsync(CancellationToken ct)
        {
            Result<IReadOnlyList<SubscriptionCatalogPlanResponse>> result =
                await Sender.Send(new GetSubscriptionCatalogQuery(), ct);

            return MapResult(result);
        }

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

        /// <summary>
        /// Полностью заменяет состав грантов плана (UC-SUB-007). Пустой список
        /// в теле — валидный запрос, снимает все гранты с плана.
        /// </summary>
        /// <param name="planId">Идентификатор плана (из маршрута).</param>
        /// <param name="request">Новый состав грантов.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешной замене;
        /// <c>400 Bad Request</c> при ошибке валидации (в том числе
        /// <c>SUBS.PLAN_GRANT_QUOTA_NOT_APPLICABLE</c>);
        /// <c>401</c> / <c>403</c>;
        /// <c>404 Not Found</c> (<c>SUBS.PLAN_NOT_FOUND</c>), если план не существует.
        /// </returns>
        [HttpPut("{planId:guid}/grants")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> SetGrantsAsync(
            [FromRoute] Guid planId,
            [FromBody] SetPlanGrantsRequest request,
            CancellationToken ct)
        {
            var grants = request.Grants
                .Select(item => new PlanGrantSpec(item.Grant, item.Quantity))
                .ToList();

            var command = new SetPlanGrantsCommand(planId, grants);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }
    }
}
