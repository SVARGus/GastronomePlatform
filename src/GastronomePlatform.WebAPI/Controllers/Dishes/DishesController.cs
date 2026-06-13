using GastronomePlatform.Common.Application.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Commands.AddCatalogIngredientToRecipe;
using GastronomePlatform.Modules.Dishes.Application.Commands.AddFreeformIngredientToRecipe;
using GastronomePlatform.Modules.Dishes.Application.Commands.AddRecipeStep;
using GastronomePlatform.Modules.Dishes.Application.Commands.CreateDishDraft;
using GastronomePlatform.Modules.Dishes.Application.Commands.IncrementDishViews;
using GastronomePlatform.Modules.Dishes.Application.Commands.PublishDish;
using GastronomePlatform.Modules.Dishes.Application.Commands.RemoveRecipeIngredient;
using GastronomePlatform.Modules.Dishes.Application.Commands.RemoveRecipeStep;
using GastronomePlatform.Modules.Dishes.Application.Commands.ReorderRecipeIngredients;
using GastronomePlatform.Modules.Dishes.Application.Commands.ReorderRecipeSteps;
using GastronomePlatform.Modules.Dishes.Application.Commands.SetDietLabels;
using GastronomePlatform.Modules.Dishes.Application.Commands.UpdateDishCard;
using GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipe;
using GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipeIngredient;
using GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipeStep;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetMyDrafts;
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

        /// <summary>
        /// Данные для обновления публичной карточки блюда (UC-DSH-002).
        /// </summary>
        /// <remarks>
        /// Не включает <c>DietLabelsMask</c>, <c>MainImageId</c> и <c>HistoryText</c> —
        /// эти поля редактируются отдельными эндпоинтами с другой семантикой.
        /// <c>OwnerType</c> резолвится сервером из ролей пользователя.
        /// </remarks>
        /// <param name="Name">Новое название блюда (3–200 символов).</param>
        /// <param name="DifficultyLevel">Уровень сложности приготовления.</param>
        /// <param name="CostEstimate">Грубая оценка стоимости блюда.</param>
        /// <param name="ShortDescription">Краткая подводка. <see langword="null"/> — очистить.</param>
        /// <param name="Description">Полное описание (markdown). <see langword="null"/> — очистить.</param>
        public sealed record UpdateDishCardRequest(
            string Name,
            DifficultyLevel DifficultyLevel,
            CostEstimate CostEstimate,
            string? ShortDescription,
            string? Description);

        /// <summary>
        /// Данные для установки диетических меток блюда (UC-DSH-009).
        /// </summary>
        /// <param name="DietLabelsMask">Новая битовая маска диетических меток.
        /// <c>None</c> допустимо (снять все метки).</param>
        public sealed record SetDietLabelsRequest(DietLabels DietLabelsMask);

        /// <summary>
        /// Данные для обновления простых полей рецепта блюда (UC-DSH-003).
        /// </summary>
        /// <remarks>
        /// Содержит только поля рецепта верхнего уровня. Шаги, ингредиенты, тайминг,
        /// выход и КБЖУ имеют отдельные эндпоинты.
        /// </remarks>
        /// <param name="IntroductionText">Вводный текст. <see langword="null"/> — очистить.</param>
        /// <param name="ServingsDefault">Количество порций по умолчанию (не меньше 1).</param>
        /// <param name="IsAlcoholic">Признак содержания алкоголя в рецепте.</param>
        /// <param name="AuthorTips">Советы автора. <see langword="null"/> — очистить.</param>
        /// <param name="ServingSuggestions">Рекомендации по сервировке. <see langword="null"/> — очистить.</param>
        /// <param name="Notes">Дополнительные заметки. <see langword="null"/> — очистить.</param>
        public sealed record UpdateRecipeRequest(
            string? IntroductionText,
            int ServingsDefault,
            bool IsAlcoholic,
            string? AuthorTips,
            string? ServingSuggestions,
            string? Notes);

        /// <summary>
        /// Данные для добавления ингредиента из справочника в рецепт блюда
        /// (UC-DSH-030, catalog-ветка).
        /// </summary>
        /// <param name="IngredientId">Идентификатор ингредиента из справочника.</param>
        /// <param name="IngredientSpecId">Идентификатор спецификации (сорта). Опционально.</param>
        /// <param name="Quantity">Количество. Строго положительное.</param>
        /// <param name="MeasureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="IsOptional">Признак опциональности позиции.</param>
        /// <param name="PreparationNote">Заметка по подготовке (до 200 символов). Опционально.</param>
        public sealed record AddCatalogIngredientToRecipeRequest(
            Guid IngredientId,
            Guid? IngredientSpecId,
            decimal Quantity,
            Guid MeasureUnitId,
            bool IsOptional,
            string? PreparationNote);

        /// <summary>
        /// Данные для добавления ингредиента свободным текстом в рецепт блюда
        /// (UC-DSH-030, freeform-ветка).
        /// </summary>
        /// <param name="FreeformText">Свободный текст ингредиента (1–200 символов).</param>
        /// <param name="Quantity">Количество. Строго положительное.</param>
        /// <param name="MeasureUnitId">Идентификатор единицы измерения.</param>
        /// <param name="IsOptional">Признак опциональности позиции.</param>
        /// <param name="PreparationNote">Заметка по подготовке (до 200 символов). Опционально.</param>
        public sealed record AddFreeformIngredientToRecipeRequest(
            string FreeformText,
            decimal Quantity,
            Guid MeasureUnitId,
            bool IsOptional,
            string? PreparationNote);

        /// <summary>
        /// Данные для обновления позиции рецепта (UC-DSH-031). Допускается смена
        /// источника catalog↔freeform — задайте ровно один из <c>IngredientId</c>
        /// или <c>FreeformText</c>.
        /// </summary>
        /// <param name="IngredientId">Новый идентификатор ингредиента из справочника или <see langword="null"/>.</param>
        /// <param name="IngredientSpecId">Новый идентификатор спецификации или <see langword="null"/>. Допустим только при заполненном <c>IngredientId</c>.</param>
        /// <param name="FreeformText">Новый свободный текст или <see langword="null"/>.</param>
        /// <param name="Quantity">Новое количество. Строго положительное.</param>
        /// <param name="MeasureUnitId">Новый идентификатор единицы измерения.</param>
        /// <param name="IsOptional">Признак опциональности позиции.</param>
        /// <param name="PreparationNote">Новая заметка по подготовке. <see langword="null"/> — очистить.</param>
        public sealed record UpdateRecipeIngredientRequest(
            Guid? IngredientId,
            Guid? IngredientSpecId,
            string? FreeformText,
            decimal Quantity,
            Guid MeasureUnitId,
            bool IsOptional,
            string? PreparationNote);

        /// <summary>
        /// Данные для переупорядочивания позиций рецепта (UC-DSH-033). Список должен
        /// содержать все позиции рецепта без дубликатов.
        /// </summary>
        /// <param name="OrderedIngredientIds">Идентификаторы позиций в желаемом порядке.</param>
        public sealed record ReorderRecipeIngredientsRequest(
            IReadOnlyList<Guid> OrderedIngredientIds);

        /// <summary>
        /// Данные для добавления шага рецепта (UC-DSH-020).
        /// </summary>
        /// <param name="Description">Основной текст шага (10–4000 символов).</param>
        /// <param name="Title">Короткий заголовок шага. Опционально, до 200 символов.</param>
        /// <param name="ImageMediaId">Идентификатор иллюстрации шага в Media. Опционально.</param>
        /// <param name="VideoUrl">URL внешнего видео (http/https). Опционально, до 500 символов.</param>
        /// <param name="TemperatureCelsius">Температура приготовления (−30..300 °C). Опционально.</param>
        /// <param name="TimerMinutes">Время для UI-таймера (1..1440 минут). Опционально.</param>
        public sealed record AddRecipeStepRequest(
            string Description,
            string? Title,
            Guid? ImageMediaId,
            string? VideoUrl,
            int? TemperatureCelsius,
            int? TimerMinutes);

        /// <summary>
        /// Данные для обновления шага рецепта (UC-DSH-021). Все опциональные поля
        /// принимают <see langword="null"/> для очистки.
        /// </summary>
        /// <param name="Description">Основной текст шага (10–4000 символов).</param>
        /// <param name="Title">Заголовок шага. <see langword="null"/> — очистить.</param>
        /// <param name="ImageMediaId">Иллюстрация в Media. <see langword="null"/> — очистить.</param>
        /// <param name="VideoUrl">URL внешнего видео. <see langword="null"/> — очистить.</param>
        /// <param name="TemperatureCelsius">Температура. <see langword="null"/> — очистить.</param>
        /// <param name="TimerMinutes">Время таймера. <see langword="null"/> — очистить.</param>
        public sealed record UpdateRecipeStepRequest(
            string Description,
            string? Title,
            Guid? ImageMediaId,
            string? VideoUrl,
            int? TemperatureCelsius,
            int? TimerMinutes);

        /// <summary>
        /// Данные для переупорядочивания шагов рецепта (UC-DSH-023). Список должен
        /// содержать все шаги рецепта без дубликатов.
        /// </summary>
        /// <param name="OrderedStepIds">Идентификаторы шагов в желаемом порядке.</param>
        public sealed record ReorderRecipeStepsRequest(
            IReadOnlyList<Guid> OrderedStepIds);

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DishesController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд MediatR.</param>
        public DishesController(ISender sender) : base(sender) { }

        #region GET Endpoints

        /// <summary>
        /// Возвращает публичную карточку блюда по идентификатору (UC-DSH-050).
        /// Эндпоинт анонимный: видимость зависит от <c>Dish.Status</c>,
        /// наличия публичного снепшота и принадлежности текущего пользователя
        /// к автору / администратору.
        /// </summary>
        /// <remarks>
        /// <para>
        /// По умолчанию возвращается публичная версия (<c>PublishedVersionData</c>).
        /// Для автора и администратора при наличии правок в рабочем слое в ответе
        /// поднимается флаг <c>HasUnsavedChanges = true</c>.
        /// </para>
        /// <para>
        /// Если у блюда нет публичного снепшота (статус <c>Draft</c> / <c>Unpublished</c>),
        /// доступ имеют только автор и admin — они получают рабочую версию
        /// с <c>IsPublishedVersion = false</c>. Остальным запрос отдаёт <c>404</c>.
        /// </para>
        /// <para>
        /// Статус <c>Archived</c> всегда возвращает <c>404</c> на Этапе 2.
        /// Доступ admin к архивированным блюдам появится на Этапе 8+.
        /// </para>
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="DishDetailDto"/> при успехе;
        /// <c>400 Bad Request</c> при пустом идентификаторе;
        /// <c>404 Not Found</c>, если блюдо отсутствует, архивировано
        /// или недоступно текущему пользователю.
        /// </returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetByIdAsync(
            Guid id,
            CancellationToken ct)
        {
            GetDishByIdQuery query = new(DishId: id);

            Result<DishDetailDto> result = await Sender.Send(query, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Возвращает рецепт блюда с полным составом (UC-DSH-052). Эндпоинт требует
        /// аутентификации (политика <c>VALID_ACTOR</c>); видимость рабочей и публичной
        /// версии симметрична UC-DSH-050.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Если у блюда есть публичный снепшот (<c>PublishedVersionData</c>) — отдаётся
        /// рецепт из снепшота. Для автора и admin при наличии правок в рабочем слое
        /// добавляется флаг <c>HasUnsavedChanges = true</c>.
        /// </para>
        /// <para>
        /// Если снепшота нет (<c>Status = Draft</c> / <c>Unpublished</c>), доступ имеют
        /// только автор и admin — они получают рабочую версию рецепта с
        /// <c>IsPublishedVersion = false</c>. Остальным запрос отдаёт <c>404</c>.
        /// </para>
        /// <para>
        /// Статус <c>Archived</c> всегда возвращает <c>404</c> на Этапе 2. Premium-проверка
        /// через <c>ISubscriptionService</c> появится на Этапе 3+.
        /// </para>
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="DishRecipeDto"/> при успехе;
        /// <c>400 Bad Request</c> при пустом идентификаторе;
        /// <c>401 Unauthorized</c>, если запрос не аутентифицирован;
        /// <c>404 Not Found</c>, если блюдо отсутствует, архивировано
        /// или недоступно текущему пользователю.
        /// </returns>
        [HttpGet("{id:guid}/recipe")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> GetRecipeAsync(
            Guid id,
            CancellationToken ct)
        {
            GetDishRecipeQuery query = new(DishId: id);

            Result<DishRecipeDto> result = await Sender.Send(query, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Возвращает постраничный список черновиков текущего пользователя (UC-DSH-053).
        /// Отсортировано по дате последнего изменения (свежие сверху).
        /// </summary>
        /// <param name="page">Номер страницы, начиная с 1. По умолчанию 1.</param>
        /// <param name="pageSize">Количество элементов на странице (1–25). По умолчанию 5.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="GetMyDraftsResult"/> при успешном запросе
        /// (пустой список <c>Items</c> — допустимый ответ);
        /// <c>400 Bad Request</c> при ошибке валидации параметров пагинации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован.
        /// </returns>
        [HttpGet("my-drafts")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> GetMyDraftsAsync(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 5,
            CancellationToken ct = default)
        {
            GetMyDraftsQuery query = new(Page: page, PageSize: pageSize);

            Result<GetMyDraftsResult> result = await Sender.Send(query, ct);
            return MapResult(result);
        }

        #endregion

        #region PUT Endpoints

        /// <summary>
        /// Обновляет простые поля рецепта блюда (UC-DSH-003).
        /// Доступно только автору блюда (POL-001 DishOwnership).
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Новые значения полей рецепта.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном обновлении;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> если пользователь не является автором блюда;
        /// <c>404 Not Found</c> если блюдо с указанным идентификатором не существует.
        /// </returns>
        [HttpPut("{id:guid}/recipe")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> UpdateRecipeAsync(
            Guid id,
            [FromBody] UpdateRecipeRequest request,
            CancellationToken ct)
        {
            var command = new UpdateRecipeCommand(
                DishId: id,
                IntroductionText: request.IntroductionText,
                ServingsDefault: request.ServingsDefault,
                IsAlcoholic: request.IsAlcoholic,
                AuthorTips: request.AuthorTips,
                ServingSuggestions: request.ServingSuggestions,
                Notes: request.Notes);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Обновляет существующую позицию рецепта (UC-DSH-031). Допускает смену
        /// источника catalog↔freeform. Доступно автору или Admin (POL-001).
        /// </summary>
        /// <remarks>
        /// После обновления сервер вызывает <c>Dish.RecalculateDishMarkers</c>:
        /// пересчитываются <c>AllergensMask</c>, <c>HasUnverifiedAllergens</c>;
        /// биты <c>DietLabelsMask</c>, конфликтующие с новой композицией, снимаются
        /// (silent auto-clear по ADR-0016).
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="recipeIngredientId">Идентификатор позиции рецепта.</param>
        /// <param name="request">Новые значения полей позиции.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном обновлении;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда (<c>DISHES.DISH_NOT_FOUND</c>),
        /// позиции (<c>DISHES.RECIPE_INGREDIENT_NOT_FOUND</c>), нового ингредиента
        /// (<c>DISHES.INGREDIENT_NOT_FOUND</c>), спецификации (<c>DISHES.INGREDIENT_SPEC_NOT_FOUND</c>)
        /// или единицы измерения (<c>DISHES.MEASURE_UNIT_NOT_FOUND</c>);
        /// <c>409 Conflict</c> при <c>DISHES.INGREDIENT_INACTIVE</c>,
        /// <c>DISHES.INGREDIENT_SPEC_MISMATCH</c>, <c>DISHES.INVALID_INGREDIENT_COMPOSITION</c>
        /// или <c>DISHES.INVALID_QUANTITY</c>.
        /// </returns>
        [HttpPut("{id:guid}/recipe/ingredients/{recipeIngredientId:guid}")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> UpdateRecipeIngredientAsync(
            Guid id,
            Guid recipeIngredientId,
            [FromBody] UpdateRecipeIngredientRequest request,
            CancellationToken ct)
        {
            var command = new UpdateRecipeIngredientCommand(
                DishId: id,
                RecipeIngredientId: recipeIngredientId,
                IngredientId: request.IngredientId,
                IngredientSpecId: request.IngredientSpecId,
                FreeformText: request.FreeformText,
                Quantity: request.Quantity,
                MeasureUnitId: request.MeasureUnitId,
                IsOptional: request.IsOptional,
                PreparationNote: request.PreparationNote);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Переупорядочивает позиции рецепта (UC-DSH-033). Состав не меняется —
        /// меняется только <c>Order</c>. Доступно автору или Admin (POL-001).
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Список идентификаторов позиций в желаемом порядке.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном переупорядочивании;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда или позиции из списка;
        /// <c>409 Conflict</c> (<c>DISHES.INVALID_INGREDIENT_ORDER</c>) если список неполон
        /// или содержит дубликаты.
        /// </returns>
        [HttpPut("{id:guid}/recipe/ingredients/order")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> ReorderRecipeIngredientsAsync(
            Guid id,
            [FromBody] ReorderRecipeIngredientsRequest request,
            CancellationToken ct)
        {
            var command = new ReorderRecipeIngredientsCommand(
                DishId: id,
                OrderedIngredientIds: request.OrderedIngredientIds);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Обновляет существующий шаг рецепта (UC-DSH-021). Доступно автору или Admin (POL-001).
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="stepId">Идентификатор шага рецепта.</param>
        /// <param name="request">Новые значения полей шага.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном обновлении;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда (<c>DISHES.DISH_NOT_FOUND</c>)
        /// или шага (<c>DISHES.STEP_NOT_FOUND</c>);
        /// <c>409 Conflict</c> (<c>DISHES.INVALID_TEMPERATURE</c> / <c>DISHES.INVALID_TIMER_MINUTES</c>)
        /// если значения вне допустимого диапазона.
        /// </returns>
        [HttpPut("{id:guid}/recipe/steps/{stepId:guid}")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> UpdateRecipeStepAsync(
            Guid id,
            Guid stepId,
            [FromBody] UpdateRecipeStepRequest request,
            CancellationToken ct)
        {
            var command = new UpdateRecipeStepCommand(
                DishId: id,
                StepId: stepId,
                Description: request.Description,
                Title: request.Title,
                ImageMediaId: request.ImageMediaId,
                VideoUrl: request.VideoUrl,
                TemperatureCelsius: request.TemperatureCelsius,
                TimerMinutes: request.TimerMinutes);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Переупорядочивает шаги рецепта (UC-DSH-023). Состав не меняется — только <c>Order</c>.
        /// Доступно автору или Admin (POL-001).
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Список идентификаторов шагов в желаемом порядке.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном переупорядочивании;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда или шага из списка;
        /// <c>409 Conflict</c> (<c>DISHES.INVALID_STEP_ORDER</c>) если список неполон или содержит дубликаты.
        /// </returns>
        [HttpPut("{id:guid}/recipe/steps/order")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> ReorderRecipeStepsAsync(
            Guid id,
            [FromBody] ReorderRecipeStepsRequest request,
            CancellationToken ct)
        {
            var command = new ReorderRecipeStepsCommand(
                DishId: id,
                OrderedStepIds: request.OrderedStepIds);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        #endregion

        #region PATCH Endpoints

        /// <summary>
        /// Обновляет публичную карточку блюда (UC-DSH-002).
        /// Доступно только автору блюда (POL-001 DishOwnership).
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Новые значения полей карточки.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном обновлении;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> если пользователь не является автором блюда;
        /// <c>404 Not Found</c> если блюдо с указанным идентификатором не существует.
        /// </returns>
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> UpdateCardAsync(
            Guid id,
            [FromBody] UpdateDishCardRequest request,
            CancellationToken ct)
        {
            var command = new UpdateDishCardCommand(
                DishId: id,
                Name: request.Name,
                ShortDescription: request.ShortDescription,
                Description: request.Description,
                DifficultyLevel: request.DifficultyLevel,
                CostEstimate: request.CostEstimate);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Устанавливает битовую маску диетических меток блюда (UC-DSH-009).
        /// Доступно автору или Admin (POL-001). Реализует ADR-0016: при попытке
        /// поставить метку, конфликтующую с составом рецепта, возвращается
        /// <c>409 DISHES.DIET_LABELS_CONFLICT_WITH_COMPOSITION</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Серверная проверка — backstop поверх UI-валидации (UI помечает
        /// недоступные метки серыми). Конфликт ингредиента определяется по
        /// <c>Ingredient.DietConflictsMask</c>; freeform-ингредиенты в проверке
        /// не участвуют (ответственность автора).
        /// </para>
        /// <para>
        /// Маска <c>None</c> допустима — снять все метки.
        /// </para>
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Новая битовая маска диетических меток.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешной установке;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c>, если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>), если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> (<c>DISHES.DISH_NOT_FOUND</c>), если блюдо не существует;
        /// <c>409 Conflict</c> (<c>DISHES.DIET_LABELS_CONFLICT_WITH_COMPOSITION</c>),
        /// если запрошенная маска конфликтует с составом рецепта.
        /// </returns>
        [HttpPatch("{id:guid}/diet-labels")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> SetDietLabelsAsync(
            Guid id,
            [FromBody] SetDietLabelsRequest request,
            CancellationToken ct)
        {
            var command = new SetDietLabelsCommand(
                DishId: id,
                DietLabelsMask: request.DietLabelsMask);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

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
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
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

        /// <summary>
        /// Публикует блюдо (UC-DSH-004). Покрывает три ветки: первую публикацию
        /// (<c>Draft → Published</c>), повторную (<c>Published</c> с несохранёнными
        /// правками) и возврат с <c>Unpublished → Published</c>. Доступно только автору
        /// блюда (POL-001 DishOwnership).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Тело запроса не требуется: публикация — переход состояния на основе уже
        /// сохранённого содержимого блюда. Все данные для jsonb-снепшота
        /// (<c>PublishedVersionData</c>) собираются сервером из текущего агрегата.
        /// </para>
        /// <para>
        /// Доменные инварианты публикации (главное фото обязательно, рецепт содержит
        /// ≥ 1 шаг и ≥ 1 ингредиент, общее время приготовления &gt; 0, блюдо не архивировано,
        /// нет повторной публикации без правок) проверяются <c>Dish.Publish</c> и при
        /// нарушении возвращаются как <c>409 Conflict</c> с конкретным доменным кодом.
        /// </para>
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешной публикации;
        /// <c>400 Bad Request</c> при ошибке валидации (<c>DishId = Guid.Empty</c>);
        /// <c>401 Unauthorized</c>, если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>), если пользователь не является автором;
        /// <c>404 Not Found</c> (<c>DISHES.DISH_NOT_FOUND</c>), если блюдо не существует;
        /// <c>409 Conflict</c> с одним из доменных кодов
        /// (<c>DISHES.CANNOT_PUBLISH_ARCHIVED_DISH</c>, <c>DISHES.DISH_ALREADY_PUBLISHED</c>,
        /// <c>DISHES.MAIN_IMAGE_REQUIRED_FOR_PUBLISH</c>, <c>DISHES.STEPS_REQUIRED_FOR_PUBLISH</c>,
        /// <c>DISHES.INGREDIENTS_REQUIRED_FOR_PUBLISH</c>, <c>DISHES.TIMING_REQUIRED_FOR_PUBLISH</c>),
        /// если нарушен один из инвариантов публикации.
        /// </returns>
        [HttpPost("{id:guid}/publish")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> PublishAsync(
            Guid id,
            CancellationToken ct)
        {
            Result result = await Sender.Send(new PublishDishCommand(id), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Регистрирует факт просмотра карточки блюда — атомарно увеличивает
        /// <c>Dish.ViewsCount</c> на 1 (UC-DSH-070). Эндпоинт публичный
        /// (<see cref="AllowAnonymousAttribute"/>): пинг шлёт клиент после успешного
        /// рендера карточки (UC-DSH-050 / UC-DSH-051), момент вызова определяется фронтом.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Условие «блюдо опубликовано» зашито в SQL-запрос инкремента. Для черновика,
        /// снятого с публикации или архивного блюда возвращается <c>404</c> — без раскрытия
        /// существования записи. Идентификатор передаётся через route; тело запроса
        /// и параметры не требуются.
        /// </para>
        /// <para>
        /// TODO: при появлении бизнес-причины — добавить фильтр самопросмотров автора
        /// (не инкрементировать, если <c>CurrentUserId == Dish.AuthorUserId</c>).
        /// Сейчас считаем все просмотры, включая просмотры автора и анонимных гостей.
        /// </para>
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном инкременте;
        /// <c>404 Not Found</c> (<c>DISHES.DISH_NOT_FOUND</c>), если блюда нет
        /// либо оно не находится в статусе <c>Published</c>.
        /// </returns>
        [HttpPost("{id:guid}/views")]
        [AllowAnonymous]
        public async Task<IActionResult> IncrementViewsAsync(
            Guid id,
            CancellationToken ct)
        {
            Result result = await Sender.Send(new IncrementDishViewsCommand(id), ct);
            return MapResult(result);
        }

        /// <summary>
        /// Добавляет ингредиент из справочника в рецепт блюда (UC-DSH-030, catalog-ветка).
        /// Доступно автору или Admin (POL-001).
        /// </summary>
        /// <remarks>
        /// После добавления сервер вызывает <c>Dish.RecalculateDishMarkers</c> —
        /// пересчитываются <c>AllergensMask</c> и автокорректируется <c>DietLabelsMask</c>
        /// (ADR-0016, silent auto-clear конфликтующих бит).
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Параметры добавляемой позиции.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с заголовком <c>Location</c> и телом
        /// <see cref="AddCatalogIngredientToRecipeResult"/>;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда (<c>DISHES.DISH_NOT_FOUND</c>),
        /// ингредиента (<c>DISHES.INGREDIENT_NOT_FOUND</c>),
        /// спецификации (<c>DISHES.INGREDIENT_SPEC_NOT_FOUND</c>)
        /// или единицы измерения (<c>DISHES.MEASURE_UNIT_NOT_FOUND</c>);
        /// <c>409 Conflict</c> при <c>DISHES.INGREDIENT_INACTIVE</c>
        /// или <c>DISHES.INGREDIENT_SPEC_MISMATCH</c>.
        /// </returns>
        [HttpPost("{id:guid}/recipe/ingredients/catalog")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> AddCatalogIngredientToRecipeAsync(
            Guid id,
            [FromBody] AddCatalogIngredientToRecipeRequest request,
            CancellationToken ct)
        {
            var command = new AddCatalogIngredientToRecipeCommand(
                DishId: id,
                IngredientId: request.IngredientId,
                IngredientSpecId: request.IngredientSpecId,
                Quantity: request.Quantity,
                MeasureUnitId: request.MeasureUnitId,
                IsOptional: request.IsOptional,
                PreparationNote: request.PreparationNote);

            Result<AddCatalogIngredientToRecipeResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created(
                $"/api/dishes/{id}/recipe/ingredients/{result.Value.Id}",
                result.Value);
        }

        /// <summary>
        /// Добавляет ингредиент свободным текстом в рецепт блюда (UC-DSH-030, freeform-ветка).
        /// Доступно автору или Admin (POL-001).
        /// </summary>
        /// <remarks>
        /// Freeform-позиция не имеет справочной маски аллергенов и диет-конфликтов:
        /// после добавления <c>Dish.RecalculateDishMarkers</c> поднимает
        /// <c>HasUnverifiedAllergens = true</c>. Диет-метки блюда автоматически не снимаются.
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Параметры добавляемой позиции.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с заголовком <c>Location</c> и телом
        /// <see cref="AddFreeformIngredientToRecipeResult"/>;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда или единицы измерения.
        /// </returns>
        [HttpPost("{id:guid}/recipe/ingredients/freeform")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> AddFreeformIngredientToRecipeAsync(
            Guid id,
            [FromBody] AddFreeformIngredientToRecipeRequest request,
            CancellationToken ct)
        {
            var command = new AddFreeformIngredientToRecipeCommand(
                DishId: id,
                FreeformText: request.FreeformText,
                Quantity: request.Quantity,
                MeasureUnitId: request.MeasureUnitId,
                IsOptional: request.IsOptional,
                PreparationNote: request.PreparationNote);

            Result<AddFreeformIngredientToRecipeResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created(
                $"/api/dishes/{id}/recipe/ingredients/{result.Value.Id}",
                result.Value);
        }

        /// <summary>
        /// Добавляет шаг в рецепт блюда (UC-DSH-020). Порядковый номер назначается автоматически
        /// (<c>Order = max+1</c>). Доступно автору или Admin (POL-001).
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="request">Данные нового шага.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>201 Created</c> с <c>{ "id": "..." }</c> и заголовком <c>Location</c>
        /// при успешном добавлении;
        /// <c>400 Bad Request</c> при ошибке валидации;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда (<c>DISHES.DISH_NOT_FOUND</c>);
        /// <c>409 Conflict</c> (<c>DISHES.INVALID_TEMPERATURE</c> / <c>DISHES.INVALID_TIMER_MINUTES</c>)
        /// если значения вне допустимого диапазона.
        /// </returns>
        [HttpPost("{id:guid}/recipe/steps")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> AddRecipeStepAsync(
            Guid id,
            [FromBody] AddRecipeStepRequest request,
            CancellationToken ct)
        {
            var command = new AddRecipeStepCommand(
                DishId: id,
                Description: request.Description,
                Title: request.Title,
                ImageMediaId: request.ImageMediaId,
                VideoUrl: request.VideoUrl,
                TemperatureCelsius: request.TemperatureCelsius,
                TimerMinutes: request.TimerMinutes);

            Result<AddRecipeStepResult> result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapResult(result);
            }

            return Created(
                $"/api/dishes/{id}/recipe/steps/{result.Value.Id}",
                result.Value);
        }

        #endregion

        #region DELETE Endpoints

        /// <summary>
        /// Удаляет позицию рецепта (UC-DSH-032) с переупорядочиванием оставшихся.
        /// Доступно автору или Admin (POL-001).
        /// </summary>
        /// <remarks>
        /// После удаления сервер вызывает <c>Dish.RecalculateDishMarkers</c>:
        /// маркеры аллергенов и диет-метки могут измениться (ADR-0016).
        /// </remarks>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="recipeIngredientId">Идентификатор удаляемой позиции рецепта.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном удалении;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда (<c>DISHES.DISH_NOT_FOUND</c>)
        /// или позиции (<c>DISHES.RECIPE_INGREDIENT_NOT_FOUND</c>).
        /// </returns>
        [HttpDelete("{id:guid}/recipe/ingredients/{recipeIngredientId:guid}")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> RemoveRecipeIngredientAsync(
            Guid id,
            Guid recipeIngredientId,
            CancellationToken ct)
        {
            var command = new RemoveRecipeIngredientCommand(
                DishId: id,
                RecipeIngredientId: recipeIngredientId);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        /// <summary>
        /// Удаляет шаг рецепта (UC-DSH-022) с переупорядочиванием оставшихся
        /// (<c>Order = 1..N</c>). Доступно автору или Admin (POL-001).
        /// </summary>
        /// <param name="id">Идентификатор блюда.</param>
        /// <param name="stepId">Идентификатор удаляемого шага.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успешном удалении;
        /// <c>401 Unauthorized</c> если запрос не аутентифицирован;
        /// <c>403 Forbidden</c> (<c>DISHES.NOT_DISH_OWNER</c>) если пользователь не автор и не Admin;
        /// <c>404 Not Found</c> при отсутствии блюда (<c>DISHES.DISH_NOT_FOUND</c>)
        /// или шага (<c>DISHES.STEP_NOT_FOUND</c>).
        /// </returns>
        [HttpDelete("{id:guid}/recipe/steps/{stepId:guid}")]
        [Authorize(Policy = AuthorizationPolicies.VALID_ACTOR)]
        public async Task<IActionResult> RemoveRecipeStepAsync(
            Guid id,
            Guid stepId,
            CancellationToken ct)
        {
            var command = new RemoveRecipeStepCommand(DishId: id, StepId: stepId);

            Result result = await Sender.Send(command, ct);
            return MapResult(result);
        }

        #endregion
    }
}
