using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetPopularTags;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Application.Queries.SearchTagsAutocomplete;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер справочника тегов модуля Dishes (UC-DSH-060..061).
    /// Все эндпоинты публичные.
    /// </summary>
    [ApiController]
    [Route("api/tags")]
    public sealed class TagsController : ApiController
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="TagsController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public TagsController(ISender sender) : base(sender) { }

        /// <summary>
        /// Возвращает автокомплит тегов по префиксу (UC-DSH-060). Ранжирование
        /// по <c>UsageCount</c> убыванию; нормализация ввода — та же, что в UC-DSH-008.
        /// </summary>
        /// <param name="query">Подстрока для поиска.</param>
        /// <param name="limit">Максимальное число записей (1..50). По умолчанию 10.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> со списком <see cref="TagDto"/> (может быть пустым);
        /// <c>400 Bad Request</c> при ошибке валидации.
        /// </returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchAsync(
            [FromQuery] string query,
            [FromQuery] int limit = 10,
            CancellationToken ct = default)
        {
            Result<IReadOnlyList<TagDto>> result =
                await Sender.Send(new SearchTagsAutocompleteQuery(query, limit), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Возвращает топ-N верифицированных тегов по <c>UsageCount</c> (UC-DSH-061).
        /// </summary>
        /// <param name="limit">Максимальное число записей (1..50). По умолчанию 20.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> со списком <see cref="TagDto"/>;
        /// <c>400 Bad Request</c> при ошибке валидации.
        /// </returns>
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularAsync(
            [FromQuery] int limit = 20,
            CancellationToken ct = default)
        {
            Result<IReadOnlyList<TagDto>> result =
                await Sender.Send(new GetPopularTagsQuery(limit), ct);
            return MapResult(result);
        }
    }
}
