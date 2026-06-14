using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Snapshots;
using GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetScaledRecipeIngredients
{
    /// <summary>
    /// Обработчик запроса <see cref="GetScaledRecipeIngredientsQuery"/> (UC-DSH-056).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Поток:
    /// </para>
    /// <list type="number">
    ///   <item>Загрузка корневого <see cref="Dish"/> по Id.</item>
    ///   <item><c>404</c>, если блюдо не найдено, <c>Archived</c> или
    ///         <c>PublishedVersionData IS NULL</c> — рецепт отдаётся только из snapshot.</item>
    ///   <item>Парсинг jsonb через <see cref="IPublishedDishSnapshotReader"/>.</item>
    ///   <item>Расчёт множителя <c>Servings / ServingsDefault</c> и линейное масштабирование
    ///         <c>Quantity</c> каждой позиции.</item>
    ///   <item>Сборка <see cref="GetScaledRecipeIngredientsResult"/>.</item>
    /// </list>
    /// <para>
    /// Конвертация единиц (Mass ↔ Volume через <c>Ingredient.DensityApprox</c>) не выполняется —
    /// клиент получает тот же <c>MeasureUnitId</c>, что в snapshot. Реализация — будущий ADR/UC.
    /// </para>
    /// </remarks>
    public sealed class GetScaledRecipeIngredientsQueryHandler
        : IQueryHandler<GetScaledRecipeIngredientsQuery, GetScaledRecipeIngredientsResult>
    {
        private readonly IDishRepository _dishRepository;
        private readonly IPublishedDishSnapshotReader _snapshotReader;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetScaledRecipeIngredientsQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="snapshotReader">Парсер jsonb-снепшота.</param>
        public GetScaledRecipeIngredientsQueryHandler(
            IDishRepository dishRepository,
            IPublishedDishSnapshotReader snapshotReader)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        }

        /// <inheritdoc/>
        public async Task<Result<GetScaledRecipeIngredientsResult>> Handle(
            GetScaledRecipeIngredientsQuery request,
            CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdAsync(request.DishId, cancellationToken);
            if (dish is null
                || dish.Status == DishStatus.Archived
                || dish.PublishedVersionData is null)
            {
                return DishesErrors.DishNotFound;
            }

            PublishedDishSnapshot snapshot = _snapshotReader.Read(dish.PublishedVersionData);

            int servingsDefault = snapshot.Recipe.ServingsDefault;
            // ServingsDefault ≥ 1 — инвариант, проверяется при публикации блюда
            // (Dish.Publish + Recipe.ServingsDefault.SetServingsDefault). Защита на ноль
            // не требуется, но прописана как условие здесь для читаемости.
            decimal multiplier = servingsDefault > 0
                ? (decimal)request.Servings / servingsDefault
                : 1m;

            IReadOnlyList<ScaledIngredientDto> scaled = snapshot.Recipe.Ingredients
                .Select(item => Scale(item, multiplier))
                .ToList();

            return new GetScaledRecipeIngredientsResult(
                ServingsDefault: servingsDefault,
                ServingsRequested: request.Servings,
                Multiplier: multiplier,
                Ingredients: scaled);
        }

        private static ScaledIngredientDto Scale(PublishedRecipeIngredientDto item, decimal multiplier)
        {
            decimal scaledQuantity = item.Quantity * multiplier;

            return item switch
            {
                PublishedCatalogIngredientDto catalog => new ScaledIngredientDto(
                    Id: catalog.Id,
                    Order: catalog.Order,
                    Type: "catalog",
                    IngredientId: catalog.IngredientId,
                    IngredientSpecId: catalog.IngredientSpecId,
                    FreeformText: null,
                    OriginalQuantity: catalog.Quantity,
                    ScaledQuantity: scaledQuantity,
                    MeasureUnitId: catalog.MeasureUnitId,
                    IsOptional: catalog.IsOptional,
                    PreparationNote: catalog.PreparationNote),

                PublishedFreeformIngredientDto freeform => new ScaledIngredientDto(
                    Id: freeform.Id,
                    Order: freeform.Order,
                    Type: "freeform",
                    IngredientId: null,
                    IngredientSpecId: null,
                    FreeformText: freeform.FreeformText,
                    OriginalQuantity: freeform.Quantity,
                    ScaledQuantity: scaledQuantity,
                    MeasureUnitId: freeform.MeasureUnitId,
                    IsOptional: freeform.IsOptional,
                    PreparationNote: freeform.PreparationNote),

                _ => throw new InvalidOperationException(
                    $"Неизвестный тип позиции рецепта в snapshot: {item.GetType().Name}."),
            };
        }
    }
}
