using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Snapshots;
using GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Subscriptions.Domain.Contracts;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishRecipe
{
    /// <summary>
    /// Обработчик запроса <see cref="GetDishRecipeQuery"/> (UC-DSH-052).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Эндпоинт защищён политикой <c>VALID_ACTOR</c> — гарантирует валидный
    /// <see cref="ICurrentUserService.UserId"/>. На уровне Handler-а пользователь
    /// идентифицируется для определения видимости рабочего слоя:
    /// </para>
    /// <list type="number">
    ///   <item>Загрузка блюда по Id (без <c>Recipe</c>).</item>
    ///   <item>404, если блюдо не найдено или <c>Status = Archived</c>.</item>
    ///   <item>Если есть <c>PublishedVersionData</c>: для не-автора/не-admin
    ///         выполняется проверка Premium-гранта
    ///         <c>FeatureGrant.FullRecipes</c> через
    ///         <see cref="ISubscriptionAccessService"/>; при отсутствии гранта
    ///         возвращается <c>403 (DISHES.PREMIUM_REQUIRED)</c>. Затем парсится снепшот
    ///         через <see cref="IPublishedDishSnapshotReader"/>, маппится в
    ///         <see cref="DishRecipeDto"/>; для автора/admin добавляется
    ///         флаг <c>HasUnsavedChanges</c>.</item>
    ///   <item>Если снепшота нет — доступ только автору/admin (иначе 404);
    ///         выполняется повторная загрузка с полным агрегатом через
    ///         <see cref="IDishRepository.GetByIdWithFullRecipeAsync"/>,
    ///         маппится рабочая версия рецепта. Premium-проверка в этой ветке
    ///         не выполняется — она отсечена ролевым фильтром выше.</item>
    /// </list>
    /// <para>
    /// Параметр <c>?version=working</c> для явного запроса рабочей версии
    /// автором при опубликованном блюде отложен до Этапа 5+ (см. UC-DSH-083).
    /// </para>
    /// </remarks>
    public sealed class GetDishRecipeQueryHandler : IQueryHandler<GetDishRecipeQuery, DishRecipeDto>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IPublishedDishSnapshotReader _snapshotReader;
        private readonly ISubscriptionAccessService _subscriptionAccess;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishRecipeQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="snapshotReader">Парсер jsonb-снепшота публичной версии.</param>
        /// <param name="subscriptionAccess">Резолвер эффективных грантов подписки для Premium-гейта.</param>
        public GetDishRecipeQueryHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IPublishedDishSnapshotReader snapshotReader,
            ISubscriptionAccessService subscriptionAccess)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
            _subscriptionAccess = subscriptionAccess ?? throw new ArgumentNullException(nameof(subscriptionAccess));
        }

        /// <inheritdoc/>
        public async Task<Result<DishRecipeDto>> Handle(
            GetDishRecipeQuery request,
            CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdAsync(request.DishId, cancellationToken);
            if (dish is null || dish.Status == DishStatus.Archived)
            {
                return DishesErrors.DishNotFound;
            }

            Guid? currentUserId = _currentUser.UserId;
            bool isOwner = currentUserId.HasValue && currentUserId.Value == dish.AuthorUserId;
            bool isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN);
            bool isOwnerOrAdmin = isOwner || isAdmin;

            if (dish.PublishedVersionData is not null)
            {
                // Автор и admin проходят Premium-гейт мимо. Для остальных —
                // проверка гранта FullRecipes; парсинг snapshot откладываем
                // до подтверждения доступа, чтобы не тратить CPU при отказе.
                if (!isOwnerOrAdmin)
                {
                    bool hasFullRecipes = await _subscriptionAccess.HasFeatureAsync(
                        currentUserId!.Value, FeatureGrant.FullRecipes, cancellationToken);

                    if (!hasFullRecipes)
                    {
                        return DishesErrors.PremiumFeatureRequired;
                    }
                }

                PublishedDishSnapshot snapshot = _snapshotReader.Read(dish.PublishedVersionData);

                bool? hasUnsavedChanges = isOwnerOrAdmin
                    ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value)
                    : null;

                return MapFromSnapshot(
                    dishId: dish.Id,
                    snapshot: snapshot,
                    isPublishedVersion: true,
                    hasUnsavedChanges: hasUnsavedChanges);
            }

            // Снепшота нет — это Draft или Unpublished. Видеть может только автор/admin.
            if (!isOwnerOrAdmin)
            {
                return DishesErrors.DishNotFound;
            }

            Dish? withRecipe = await _dishRepository.GetByIdWithFullRecipeAsync(request.DishId, cancellationToken);
            if (withRecipe is null || withRecipe.Status == DishStatus.Archived)
            {
                // Защита от гонок: блюдо могло быть архивировано/удалено между двумя загрузками.
                return DishesErrors.DishNotFound;
            }

            return MapFromWorking(
                dish: withRecipe,
                isPublishedVersion: false,
                hasUnsavedChanges: false);
        }

        private static DishRecipeDto MapFromSnapshot(
            Guid dishId,
            PublishedDishSnapshot snapshot,
            bool isPublishedVersion,
            bool? hasUnsavedChanges)
        {
            PublishedRecipeDto recipe = snapshot.Recipe;

            RecipeViewDto recipeView = new(
                IntroductionText: recipe.IntroductionText,
                ServingsDefault: recipe.ServingsDefault,
                IsAlcoholic: recipe.IsAlcoholic,
                AuthorTips: recipe.AuthorTips,
                ServingSuggestions: recipe.ServingSuggestions,
                Notes: recipe.Notes,
                Timing: MapTimingFromSnapshot(recipe.Timing),
                Yield: MapYieldFromSnapshot(recipe.Yield),
                Nutrition: recipe.Nutrition is null ? null : MapNutritionFromSnapshot(recipe.Nutrition),
                Steps: recipe.Steps.Select(MapStepFromSnapshot).ToList(),
                Ingredients: recipe.Ingredients.Select(MapIngredientFromSnapshot).ToList());

            return new DishRecipeDto(
                DishId: dishId,
                IsPublishedVersion: isPublishedVersion,
                HasUnsavedChanges: hasUnsavedChanges,
                Recipe: recipeView);
        }

        private static TimingViewDto MapTimingFromSnapshot(PublishedTimingDto t) => new(
            PrepTimeMinutes: t.PrepTimeMinutes,
            CookTimeMinutes: t.CookTimeMinutes,
            RestTimeMinutes: t.RestTimeMinutes,
            ActiveTimeMinutes: t.ActiveTimeMinutes,
            TotalTimeMinutes: t.TotalTimeMinutes,
            IsTotalManual: t.IsTotalManual);

        private static YieldViewDto MapYieldFromSnapshot(PublishedYieldDto y) => new(
            QuantityTotal: y.QuantityTotal,
            YieldUnit: y.YieldUnit,
            ServingsCount: y.ServingsCount,
            GramsPerServing: y.GramsPerServing);

        private static NutritionViewDto MapNutritionFromSnapshot(PublishedNutritionDto n) => new(
            CalcMethod: n.CalcMethod,
            Calories: n.Calories,
            Proteins: n.Proteins,
            Fats: n.Fats,
            SaturatedFats: n.SaturatedFats,
            Carbs: n.Carbs,
            Sugar: n.Sugar,
            Fiber: n.Fiber,
            Salt: n.Salt);

        private static RecipeStepViewDto MapStepFromSnapshot(PublishedRecipeStepDto s) => new(
            Id: s.Id,
            Order: s.Order,
            Title: s.Title,
            Description: s.Description,
            ImageMediaId: s.ImageMediaId,
            VideoUrl: s.VideoUrl,
            TemperatureCelsius: s.TemperatureCelsius,
            TimerMinutes: s.TimerMinutes);

        private static RecipeIngredientViewDto MapIngredientFromSnapshot(PublishedRecipeIngredientDto i) => i switch
        {
            PublishedCatalogIngredientDto c => new CatalogRecipeIngredientViewDto(
                Id: c.Id,
                Order: c.Order,
                Quantity: c.Quantity,
                MeasureUnitId: c.MeasureUnitId,
                IsOptional: c.IsOptional,
                PreparationNote: c.PreparationNote,
                IngredientId: c.IngredientId,
                IngredientSpecId: c.IngredientSpecId),
            PublishedFreeformIngredientDto f => new FreeformRecipeIngredientViewDto(
                Id: f.Id,
                Order: f.Order,
                Quantity: f.Quantity,
                MeasureUnitId: f.MeasureUnitId,
                IsOptional: f.IsOptional,
                PreparationNote: f.PreparationNote,
                FreeformText: f.FreeformText),
            _ => throw new InvalidOperationException(
                $"Неизвестная природа ингредиента в снепшоте: {i.GetType().Name}.")
        };

        private static DishRecipeDto MapFromWorking(
            Dish dish,
            bool isPublishedVersion,
            bool? hasUnsavedChanges)
        {
            Recipe recipe = dish.Recipe;

            RecipeViewDto recipeView = new(
                IntroductionText: recipe.IntroductionText,
                ServingsDefault: recipe.ServingsDefault,
                IsAlcoholic: recipe.IsAlcoholic,
                AuthorTips: recipe.AuthorTips,
                ServingSuggestions: recipe.ServingSuggestions,
                Notes: recipe.Notes,
                Timing: MapTimingFromDomain(recipe.Timing),
                Yield: MapYieldFromDomain(recipe.Yield),
                Nutrition: recipe.Nutrition is null ? null : MapNutritionFromDomain(recipe.Nutrition),
                Steps: recipe.Steps.OrderBy(s => s.Order).Select(MapStepFromDomain).ToList(),
                Ingredients: recipe.Ingredients.OrderBy(i => i.Order).Select(MapIngredientFromDomain).ToList());

            return new DishRecipeDto(
                DishId: dish.Id,
                IsPublishedVersion: isPublishedVersion,
                HasUnsavedChanges: hasUnsavedChanges,
                Recipe: recipeView);
        }

        private static TimingViewDto MapTimingFromDomain(Timing t) => new(
            PrepTimeMinutes: t.PrepTimeMinutes,
            CookTimeMinutes: t.CookTimeMinutes,
            RestTimeMinutes: t.RestTimeMinutes,
            ActiveTimeMinutes: t.ActiveTimeMinutes,
            TotalTimeMinutes: t.TotalTimeMinutes,
            IsTotalManual: t.IsTotalManual);

        private static YieldViewDto MapYieldFromDomain(Yield y) => new(
            QuantityTotal: y.QuantityTotal,
            YieldUnit: y.YieldUnit,
            ServingsCount: y.ServingsCount,
            GramsPerServing: y.GramsPerServing);

        private static NutritionViewDto MapNutritionFromDomain(Nutrition n) => new(
            CalcMethod: n.CalcMethod,
            Calories: n.Calories,
            Proteins: n.Proteins,
            Fats: n.Fats,
            SaturatedFats: n.SaturatedFats,
            Carbs: n.Carbs,
            Sugar: n.Sugar,
            Fiber: n.Fiber,
            Salt: n.Salt);

        private static RecipeStepViewDto MapStepFromDomain(RecipeStep s) => new(
            Id: s.Id,
            Order: s.Order,
            Title: s.Title,
            Description: s.Description,
            ImageMediaId: s.ImageMediaId,
            VideoUrl: s.VideoUrl,
            TemperatureCelsius: s.TemperatureCelsius,
            TimerMinutes: s.TimerMinutes);

        // Дискриминация по природе симметрична PublishedDishSnapshotBuilder.MapIngredient:
        // IngredientId.HasValue == catalog, иначе freeform. Инвариант XOR обеспечен Domain-фабриками (ADR-0012).
        private static RecipeIngredientViewDto MapIngredientFromDomain(RecipeIngredient i)
        {
            if (i.IngredientId.HasValue)
            {
                return new CatalogRecipeIngredientViewDto(
                    Id: i.Id,
                    Order: i.Order,
                    Quantity: i.Quantity,
                    MeasureUnitId: i.MeasureUnitId,
                    IsOptional: i.IsOptional,
                    PreparationNote: i.PreparationNote,
                    IngredientId: i.IngredientId.Value,
                    IngredientSpecId: i.IngredientSpecId);
            }

            return new FreeformRecipeIngredientViewDto(
                Id: i.Id,
                Order: i.Order,
                Quantity: i.Quantity,
                MeasureUnitId: i.MeasureUnitId,
                IsOptional: i.IsOptional,
                PreparationNote: i.PreparationNote,
                FreeformText: i.FreeformText!);
        }
    }
}
