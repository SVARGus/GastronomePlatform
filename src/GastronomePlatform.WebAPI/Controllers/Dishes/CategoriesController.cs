using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Commands.CreateCategory;
using GastronomePlatform.Modules.Dishes.Application.Commands.DeleteOrDeactivateCategory;
using GastronomePlatform.Modules.Dishes.Application.Commands.MoveCategory;
using GastronomePlatform.Modules.Dishes.Application.Commands.RegenerateCategorySlug;
using GastronomePlatform.Modules.Dishes.Application.Commands.UpdateCategory;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryById;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryBySlug;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryTree;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер справочника категорий модуля Dishes (UC-DSH-057..059, UC-DSH-101..105).
    /// Публичные query (tree/by-id/by-slug) и admin-команды (CRUD + move + slug).
    /// </summary>
    [ApiController]
    [Route("api/categories")]
    public sealed class CategoriesController : ApiController
    {
        #region Request Models

        /// <summary>
        /// Данные для создания категории (UC-DSH-101).
        /// </summary>
        /// <param name="Name">Имя категории (2..100).</param>
        /// <param name="ParentId">Идентификатор родителя или <see langword="null"/>.</param>
        /// <param name="Order">Порядок отображения.</param>
        /// <param name="IconMediaId">Идентификатор иконки. Опционально.</param>
        public sealed record CreateCategoryRequest(
            string Name,
            Guid? ParentId,
            int Order,
            Guid? IconMediaId);

        /// <summary>
        /// Данные для обновления категории (UC-DSH-102).
        /// </summary>
        /// <param name="Name">Новое имя.</param>
        /// <param name="Order">Новый порядок отображения.</param>
        /// <param name="IconMediaId">Идентификатор иконки.</param>
        /// <param name="IsActive">Признак активности.</param>
        public sealed record UpdateCategoryRequest(
            string Name,
            int Order,
            Guid? IconMediaId,
            bool IsActive);

        /// <summary>
        /// Данные для перемещения категории в иерархии (UC-DSH-104).
        /// </summary>
        /// <param name="NewParentId">Новый родитель или <see langword="null"/> для перемещения в корень.</param>
        public sealed record MoveCategoryRequest(Guid? NewParentId);

        #endregion

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

        /// <summary>
        /// Создаёт новую категорию (UC-DSH-101). Slug генерируется сервером;
        /// проверяется глубина иерархии (≤ 3 уровня).
        /// </summary>
        /// <param name="request">Данные категории.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с <see cref="CreateCategoryResult"/>;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401</c> / <c>403</c>;
        /// <c>404 Not Found</c> (<c>DISHES.CATEGORY_PARENT_NOT_FOUND</c>) при отсутствии родителя;
        /// <c>409 Conflict</c> (<c>DISHES.CATEGORY_DEPTH_EXCEEDED</c>) при превышении глубины.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateCategoryRequest request,
            CancellationToken ct)
        {
            var command = new CreateCategoryCommand(
                Name: request.Name,
                ParentId: request.ParentId,
                Order: request.Order,
                IconMediaId: request.IconMediaId);

            Result<CreateCategoryResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created($"/api/categories/{result.Value.Id}", result.Value);
        }

        /// <summary>
        /// Обновляет категорию (UC-DSH-102): <c>Name</c>, <c>Order</c>, <c>IconMediaId</c>, <c>IsActive</c>.
        /// Slug и <c>ParentId</c> через этот эндпоинт не меняются — отдельные операции.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <param name="request">Новые значения полей.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>400</c> / <c>401</c> / <c>403</c>;
        /// <c>404 Not Found</c> (<c>DISHES.CATEGORY_NOT_FOUND</c>) при отсутствии категории.
        /// </returns>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> UpdateAsync(
            Guid id,
            [FromBody] UpdateCategoryRequest request,
            CancellationToken ct)
        {
            var command = new UpdateCategoryCommand(
                CategoryId: id,
                Name: request.Name,
                Order: request.Order,
                IconMediaId: request.IconMediaId,
                IsActive: request.IsActive);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Удаляет или деактивирует категорию (UC-DSH-103). Hard delete возможен
        /// только если нет дочерних категорий и нет связок <c>DishCategory</c> /
        /// <c>DishCategoryPublished</c>; иначе — мягкое <c>IsActive=false</c>.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="DeleteOrDeactivateCategoryResult"/>
        /// (поле <c>wasDeleted</c> сообщает о результате);
        /// <c>400</c> / <c>401</c> / <c>403</c>;
        /// <c>404 Not Found</c> (<c>DISHES.CATEGORY_NOT_FOUND</c>) при отсутствии категории.
        /// </returns>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> DeleteOrDeactivateAsync(Guid id, CancellationToken ct)
        {
            Result<DeleteOrDeactivateCategoryResult> result =
                await Sender.Send(new DeleteOrDeactivateCategoryCommand(id), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Перемещает категорию в иерархии (UC-DSH-104). Проверка отсутствия циклов
        /// и соблюдения глубины (≤ 3 уровня).
        /// </summary>
        /// <param name="id">Идентификатор перемещаемой категории.</param>
        /// <param name="request">Новый родитель или <see langword="null"/>.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>400</c> / <c>401</c> / <c>403</c>;
        /// <c>404 Not Found</c> (<c>DISHES.CATEGORY_NOT_FOUND</c> / <c>DISHES.CATEGORY_PARENT_NOT_FOUND</c>);
        /// <c>409 Conflict</c> (<c>DISHES.CATEGORY_MOVE_TO_OWN_DESCENDANT</c> /
        /// <c>DISHES.CATEGORY_DEPTH_EXCEEDED</c>).
        /// </returns>
        [HttpPut("{id:guid}/move")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> MoveAsync(
            Guid id,
            [FromBody] MoveCategoryRequest request,
            CancellationToken ct)
        {
            Result result = await Sender.Send(
                new MoveCategoryCommand(id, request.NewParentId), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Перегенерирует slug категории (UC-DSH-105). Опасная операция — ломает
        /// существующие публичные ссылки. Slug собирается из текущего <c>Name</c>
        /// через <c>ISlugGenerator</c> с разрешением коллизий суффиксом.
        /// </summary>
        /// <param name="id">Идентификатор категории.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с новым <c>NewSlug</c>;
        /// <c>400</c> / <c>401</c> / <c>403</c>;
        /// <c>404</c> при отсутствии категории.
        /// </returns>
        [HttpPost("{id:guid}/regenerate-slug")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> RegenerateSlugAsync(Guid id, CancellationToken ct)
        {
            Result<RegenerateCategorySlugResult> result =
                await Sender.Send(new RegenerateCategorySlugCommand(id), ct);
            return MapResult(result);
        }
    }
}
