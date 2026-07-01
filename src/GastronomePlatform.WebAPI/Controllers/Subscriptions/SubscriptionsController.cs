using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Subscriptions
{
    /// <summary>
    /// Контроллер модуля Subscriptions — управление подписками, каталогом тарифов и платежами.
    /// </summary>
    /// <remarks>
    /// Эндпоинты будут добавлены по мере реализации UC-SUB-* (Phase A → Phase B → Phase C).
    /// См. <c>docs/public/modules/subscriptions/use-cases/README.md</c> для полного списка сценариев.
    /// </remarks>
    [ApiController]
    [Route("api/subscriptions")]
    public sealed class SubscriptionsController : ApiController
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SubscriptionsController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд MediatR.</param>
        public SubscriptionsController(ISender sender) : base(sender) { }
    }
}
