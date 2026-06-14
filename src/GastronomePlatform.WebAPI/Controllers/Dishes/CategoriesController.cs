using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryById;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryBySlug;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryTree;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер справочника категорий модуля Dishes (UC-DSH-057..059).
    /// Все эндпоинты публичные.
    /// </summary>
    [ApiController]
    [Route("api/categories")]
    public sealed class CategoriesController : ApiController
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CategoriesController"/>.
        /// </summary>
        /// <param name="sender">Отправитель MediatR.</param>
        public CategoriesController(ISender sender) : base(sender) { }

        /// <summary>
        /// Возвращает полное дерево активных категорий (UC-DSH-057).
        /// </summary>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns><c>200 OK</c> со списком корневых узлов (с рекурсивными детьми).</returns>
        [HttpGet("tree")]
        public async Task<IActionResult> GetTreeAsync(CancellationToken ct)
        {
            Result<IReadOnlyList<CategoryNodeDto>> result =
                await Sender.Send(new GetCategoryTreeQuery(), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Возвращает карточку категории по идентификатору с непосредственными
        /// дочерними категориями (UC-DSH-058).
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="CategoryDetailDto"/>;
        /// <c>400 Bad Request</c> при <c>Guid.Empty</c>;
        /// <c>404 Not Found</c> (<c>DISHES.CATEGORY_NOT_FOUND</c>) при отсутствии
        /// активной категории.
        /// </returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
        {
            Result<CategoryDetailDto> result =
                await Sender.Send(new GetCategoryByIdQuery(id), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Возвращает карточку категории по slug с непосредственными
        /// дочерними категориями (UC-DSH-059).
        /// </summary>
        /// <param name="slug">URL-friendly идентификатор категории.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="CategoryDetailDto"/>;
        /// <c>400 Bad Request</c> при пустом или слишком длинном slug;
        /// <c>404 Not Found</c> при отсутствии активной категории.
        /// </returns>
        [HttpGet("by-slug/{slug}")]
        public async Task<IActionResult> GetBySlugAsync(string slug, CancellationToken ct)
        {
            Result<CategoryDetailDto> result =
                await Sender.Send(new GetCategoryBySlugQuery(slug), ct);
            return MapResult(result);
        }
    }
}
