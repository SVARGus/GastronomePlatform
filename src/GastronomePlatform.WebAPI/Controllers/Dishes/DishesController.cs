using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Commands.CreateDishDraft;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер модуля Dishes — каталог блюд и рецептов.
    /// Предоставляет эндпоинты для получения и обновления блюд.
    /// </summary>
    [ApiController]
    [Route("api/dishes")]
    public sealed class DishesController : ApiController
    {
        #region Request Models

        /// <summary>
        /// Данные для создания черновика блюда (UC-DSH-001).
        /// </summary>
        /// <param name="Name">Отображаемое название блюда (3–200 символов).</param>
        /// <param name="DifficultyLevel">Уровень сложности приготовления.</param>
        /// <param name="CostEstimate">Грубая оценка стоимости блюда.</param>
        /// <param name="ShortDescription">Краткая подводка для карточек каталога. Опционально, до 500 символов.</param>
        /// <param name="Description">Полное описание блюда (markdown). Опционально, до 4000 символов.</param>
        /// <param name="DietLabelsMask">Битовая маска диетических меток. Опционально.</param>
        /// <param name="HistoryText">Историко-культурный контекст блюда. Опционально, до 4000 символов.</param>
        public sealed record CreateDishDraftRequest(
            string Name,
            DifficultyLevel DifficultyLevel,
            CostEstimate CostEstimate,
            string? ShortDescription,
            string? Description,
            DietLabels? DietLabelsMask,
            string? HistoryText);

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DishesController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд MediatR.</param>
        public DishesController(ISender sender) : base(sender) { }

        #region GET Endpoints

        #endregion

        #region PUT Endpoints

        #endregion

        #region POST Endpoints

        /// <summary>
        /// Создаёт черновик блюда (UC-DSH-001).
        /// </summary>
        /// <param name="request">Данные для создания черновика.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с <see cref="CreateDishDraftResult"/> и заголовком <c>Location</c> при успехе;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован.
        /// </returns>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateDraftAsync(
            [FromBody] CreateDishDraftRequest request,
            CancellationToken ct)
        {
            var command = new CreateDishDraftCommand(
                request.Name,
                request.DifficultyLevel,
                request.CostEstimate,
                request.ShortDescription,
                request.Description,
                request.DietLabelsMask,
                request.HistoryText);

            Result<CreateDishDraftResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            // 201 Created с заголовком Location. Базовый MapResult<T> возвращает 200 OK,
            // но для семантики REST Create явно используем CreatedAtAction.
            return Created($"/api/dishes/{result.Value.Id}", result.Value);
        }

        #endregion

        #region DELETE Endpoints

        #endregion
    }
}
