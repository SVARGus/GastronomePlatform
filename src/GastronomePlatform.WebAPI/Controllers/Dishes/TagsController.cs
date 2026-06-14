using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Commands.DeleteTag;
using GastronomePlatform.Modules.Dishes.Application.Commands.VerifyTag;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetPopularTags;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Application.Queries.SearchTagsAutocomplete;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер справочника тегов модуля Dishes (UC-DSH-060..061, UC-DSH-130..131).
    /// Публичные query (search/popular) и admin-команды (verify/delete).
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

        /// <summary>
        /// Верифицирует тег администратором (UC-DSH-130). После верификации тег
        /// появляется в облаке популярных и в общем автокомплите даже при низком
        /// <c>UsageCount</c>. Идемпотентно: повторный вызов на уже верифицированном
        /// теге также возвращает <c>204</c>.
        /// </summary>
        /// <param name="id">Идентификатор тега.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>400 Bad Request</c> при <c>Guid.Empty</c>;
        /// <c>401 Unauthorized</c>, если запрос не аутентифицирован;
        /// <c>403 Forbidden</c>, если пользователь не имеет роли <c>Admin</c>;
        /// <c>404 Not Found</c> (<c>DISHES.TAG_NOT_FOUND</c>) при отсутствии тега.
        /// </returns>
        [HttpPost("{id:guid}/verify")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> VerifyAsync(Guid id, CancellationToken ct)
        {
            Result result = await Sender.Send(new VerifyTagCommand(id), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Удаляет тег администратором (UC-DSH-131). Hard delete: тег и все его связки
        /// <c>DishTag</c>/<c>DishTagPublished</c> исчезают из БД. У всех затронутых блюд
        /// обновляется <c>Dish.UpdatedAt</c>.
        /// </summary>
        /// <remarks>
        /// Используется для очистки спама/мата. <c>Tag.UsageCount</c> при этом не
        /// пересчитывается у других тегов — он касается только этого тега, который удаляется.
        /// </remarks>
        /// <param name="id">Идентификатор тега.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>400 Bad Request</c> при <c>Guid.Empty</c>;
        /// <c>401 Unauthorized</c>, если запрос не аутентифицирован;
        /// <c>403 Forbidden</c>, если пользователь не имеет роли <c>Admin</c>;
        /// <c>404 Not Found</c> (<c>DISHES.TAG_NOT_FOUND</c>) при отсутствии тега.
        /// </returns>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
        {
            Result result = await Sender.Send(new DeleteTagCommand(id), ct);
            return MapResult(result);
        }
    }
}
