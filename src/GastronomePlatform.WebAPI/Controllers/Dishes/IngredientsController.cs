using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Commands.CreateIngredient;
using GastronomePlatform.Modules.Dishes.Application.Commands.DeactivateIngredient;
using GastronomePlatform.Modules.Dishes.Application.Commands.UpdateIngredient;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetIngredientById;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Application.Queries.SearchIngredients;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер справочника ингредиентов модуля Dishes (UC-DSH-062..063, UC-DSH-110..112).
    /// Публичные query (search/by-id) и admin-команды (create/update/deactivate).
    /// </summary>
    [ApiController]
    [Route("api/ingredients")]
    public sealed class IngredientsController : ApiController
    {
        #region Request Models

        /// <summary>
        /// Данные для создания записи справочника ингредиентов (UC-DSH-110).
        /// </summary>
        /// <param name="Name">Уникальное название (2..200 символов).</param>
        /// <param name="PluralName">Форма родительного падежа. Опционально.</param>
        /// <param name="Description">Описание (markdown). Опционально.</param>
        /// <param name="ImageMediaId">Идентификатор изображения. Опционально.</param>
        /// <param name="IsLiquid">Флаг «продукт жидкий».</param>
        /// <param name="DensityApprox">Плотность, г/мл. Обязательна при <paramref name="IsLiquid"/>.</param>
        /// <param name="IsAllergen">Флаг «продукт-аллерген».</param>
        /// <param name="AllergenType">Тип аллергена. Обязателен при <paramref name="IsAllergen"/>.</param>
        /// <param name="DietConflictsMask">Маска конфликтующих диет-меток.</param>
        /// <param name="BaseMeasureUnitId">Базовая единица хранения.</param>
        /// <param name="DefaultNutritionId">Идентификатор КБЖУ по умолчанию. Опционально.</param>
        public sealed record CreateIngredientRequest(
            string Name,
            string? PluralName,
            string? Description,
            Guid? ImageMediaId,
            bool IsLiquid,
            decimal? DensityApprox,
            bool IsAllergen,
            AllergenType? AllergenType,
            DietLabels DietConflictsMask,
            Guid BaseMeasureUnitId,
            Guid? DefaultNutritionId);

        /// <summary>
        /// Данные для обновления записи справочника ингредиентов (UC-DSH-111).
        /// </summary>
        /// <param name="Name">Новое название (уникальное).</param>
        /// <param name="PluralName">Форма родительного падежа. Опционально.</param>
        /// <param name="Description">Описание. Опционально.</param>
        /// <param name="ImageMediaId">Идентификатор изображения. Опционально.</param>
        /// <param name="IsLiquid">Флаг «продукт жидкий».</param>
        /// <param name="DensityApprox">Плотность, г/мл.</param>
        /// <param name="IsAllergen">Флаг «продукт-аллерген».</param>
        /// <param name="AllergenType">Тип аллергена.</param>
        /// <param name="DietConflictsMask">Маска конфликтующих диет-меток.</param>
        /// <param name="BaseMeasureUnitId">Базовая единица хранения.</param>
        /// <param name="DefaultNutritionId">Идентификатор КБЖУ по умолчанию.</param>
        public sealed record UpdateIngredientRequest(
            string Name,
            string? PluralName,
            string? Description,
            Guid? ImageMediaId,
            bool IsLiquid,
            decimal? DensityApprox,
            bool IsAllergen,
            AllergenType? AllergenType,
            DietLabels DietConflictsMask,
            Guid BaseMeasureUnitId,
            Guid? DefaultNutritionId);

        #endregion

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

        /// <summary>
        /// Создаёт новую запись в справочнике ингредиентов (UC-DSH-110).
        /// </summary>
        /// <param name="request">Данные нового ингредиента.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с <see cref="CreateIngredientResult"/> и заголовком <c>Location</c>;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c>, если запрос не аутентифицирован;
        /// <c>403 Forbidden</c>, если пользователь не имеет роли <c>Admin</c>;
        /// <c>404 Not Found</c> (<c>DISHES.MEASURE_UNIT_NOT_FOUND</c>);
        /// <c>409 Conflict</c> (<c>DISHES.INGREDIENT_NAME_TAKEN</c>) при коллизии имени.
        /// </returns>
        [HttpPost]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateIngredientRequest request,
            CancellationToken ct)
        {
            var command = new CreateIngredientCommand(
                Name: request.Name,
                PluralName: request.PluralName,
                Description: request.Description,
                ImageMediaId: request.ImageMediaId,
                IsLiquid: request.IsLiquid,
                DensityApprox: request.DensityApprox,
                IsAllergen: request.IsAllergen,
                AllergenType: request.AllergenType,
                DietConflictsMask: request.DietConflictsMask,
                BaseMeasureUnitId: request.BaseMeasureUnitId,
                DefaultNutritionId: request.DefaultNutritionId);

            Result<CreateIngredientResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created($"/api/ingredients/{result.Value.Id}", result.Value);
        }

        /// <summary>
        /// Обновляет существующую запись справочника ингредиентов (UC-DSH-111). Флаг
        /// <c>IsActive</c> через этот эндпоинт не меняется — для активации/деактивации
        /// используется UC-DSH-112.
        /// </summary>
        /// <remarks>
        /// При изменении <c>AllergenType</c>/<c>DietConflictsMask</c> существующие
        /// <c>Dish.AllergensMask</c> / <c>DietLabelsMask</c> не пересчитываются —
        /// массовая инвалидация откладывается на фоновую задачу (Этап 4+/8+).
        /// </remarks>
        /// <param name="id">Идентификатор ингредиента.</param>
        /// <param name="request">Новые значения полей.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401</c> / <c>403</c> аналогично UC-DSH-110;
        /// <c>404 Not Found</c> (<c>DISHES.INGREDIENT_NOT_FOUND</c> или
        /// <c>DISHES.MEASURE_UNIT_NOT_FOUND</c>);
        /// <c>409 Conflict</c> (<c>DISHES.INGREDIENT_NAME_TAKEN</c>) при коллизии имени.
        /// </returns>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> UpdateAsync(
            Guid id,
            [FromBody] UpdateIngredientRequest request,
            CancellationToken ct)
        {
            var command = new UpdateIngredientCommand(
                IngredientId: id,
                Name: request.Name,
                PluralName: request.PluralName,
                Description: request.Description,
                ImageMediaId: request.ImageMediaId,
                IsLiquid: request.IsLiquid,
                DensityApprox: request.DensityApprox,
                IsAllergen: request.IsAllergen,
                AllergenType: request.AllergenType,
                DietConflictsMask: request.DietConflictsMask,
                BaseMeasureUnitId: request.BaseMeasureUnitId,
                DefaultNutritionId: request.DefaultNutritionId);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Деактивирует ингредиент (UC-DSH-112): переключает <c>IsActive = false</c>.
        /// Существующие <c>RecipeIngredient</c> сохраняются. Идемпотентно.
        /// </summary>
        /// <param name="id">Идентификатор ингредиента.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>400 Bad Request</c> при <c>Guid.Empty</c>;
        /// <c>401</c> / <c>403</c> аналогично остальным admin-эндпоинтам;
        /// <c>404 Not Found</c> (<c>DISHES.INGREDIENT_NOT_FOUND</c>) при отсутствии записи.
        /// </returns>
        [HttpPost("{id:guid}/deactivate")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken ct)
        {
            Result result = await Sender.Send(new DeactivateIngredientCommand(id), ct);
            return MapResult(result);
        }
    }
}
