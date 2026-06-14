using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetIngredientById;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Application.Queries.SearchIngredients;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер справочника ингредиентов модуля Dishes (UC-DSH-062..063).
    /// Все эндпоинты публичные.
    /// </summary>
    [ApiController]
    [Route("api/ingredients")]
    public sealed class IngredientsController : ApiController
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="IngredientsController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public IngredientsController(ISender sender) : base(sender) { }

        /// <summary>
        /// Возвращает активные ингредиенты, имя которых начинается с присланного
        /// префикса (UC-DSH-062). Case-insensitive поиск через <c>ILIKE</c>;
        /// ранжирование по алфавиту имени.
        /// </summary>
        /// <param name="query">Префикс имени для поиска.</param>
        /// <param name="limit">Максимальное число записей (1..50). По умолчанию 20.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> со списком <see cref="IngredientDto"/>;
        /// <c>400 Bad Request</c> при ошибке валидации.
        /// </returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchAsync(
            [FromQuery] string query,
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            Result<IReadOnlyList<IngredientDto>> result =
                await Sender.Send(new SearchIngredientsQuery(query, limit), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Возвращает карточку ингредиента по идентификатору (UC-DSH-063).
        /// Включает и неактивные ингредиенты (флаг <c>IsActive</c> приходит в DTO).
        /// </summary>
        /// <param name="id">Идентификатор ингредиента.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="IngredientDto"/>;
        /// <c>400 Bad Request</c> при <c>Guid.Empty</c>;
        /// <c>404 Not Found</c> (<c>DISHES.INGREDIENT_NOT_FOUND</c>) при отсутствии записи.
        /// </returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
        {
            Result<IngredientDto> result =
                await Sender.Send(new GetIngredientByIdQuery(id), ct);
            return MapResult(result);
        }
    }
}
