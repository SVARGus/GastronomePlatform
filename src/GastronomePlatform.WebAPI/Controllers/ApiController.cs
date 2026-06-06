using GastronomePlatform.Common.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers
{
    /// <summary>
    /// Базовый класс для всех API-контроллеров.
    /// Содержит централизованный маппинг <see cref="Result"/> → <see cref="IActionResult"/>.
    /// </summary>
    [ApiController]
    public abstract class ApiController : ControllerBase
    {
        /// <summary>
        /// Отправитель команд и запросов MediatR.
        /// </summary>
        protected readonly ISender Sender;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ApiController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд и запросов MediatR.</param>
        protected ApiController(ISender sender)
        {
            Sender = sender;
        }

        /// <summary>
        /// Маппит <see cref="Result"/> без значения в <see cref="IActionResult"/>.
        /// При успехе возвращает <c>204 No Content</c>.
        /// </summary>
        /// <param name="result">Результат операции.</param>
        protected IActionResult MapResult(Result result)
        {
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return MapError(result.Error);
        }

        /// <summary>
        /// Маппит <see cref="Result{T}"/> со значением в <see cref="IActionResult"/>.
        /// При успехе возвращает <c>200 OK</c> со значением в теле ответа.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
        /// <param name="result">Результат операции.</param>
        protected IActionResult MapResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return MapError(result.Error);
        }

        /// <summary>
        /// Маппит <see cref="Error"/> в соответствующий HTTP-статус.
        /// Единственное место в проекте где <see cref="ErrorType"/> переводится в HTTP Status Code.
        /// </summary>
        /// <param name="error">Доменная ошибка.</param>
        /// <remarks>
        /// Для <see cref="ErrorType.Forbidden"/> используется <see cref="ControllerBase.StatusCode(int, object?)"/>
        /// вместо <see cref="ControllerBase.Forbid()"/> — стандартный <c>Forbid()</c> не сериализует тело,
        /// а клиенту нужен доменный код ошибки (например, <c>DISHES.NOT_DISH_OWNER</c>) для UX и диагностики.
        /// </remarks>
        protected IActionResult MapError(Error error)
        {
            return error.Type switch
            {
                ErrorType.NotFound      => NotFound(error),
                ErrorType.Validation    => BadRequest(error),
                ErrorType.Conflict      => Conflict(error),
                ErrorType.Forbidden     => StatusCode(StatusCodes.Status403Forbidden, error),
                _                       => BadRequest(error)
            };
        }
    }
}
