using System.Text.Json;
using GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Snapshots
{
    /// <summary>
    /// Реализация <see cref="IPublishedDishSnapshotBuilder"/>. Маппит загруженный
    /// агрегат <see cref="Dish"/> в полиморфные DTO снепшота и сериализует их в JSON
    /// через <see cref="SnapshotJsonOptions.Default"/>.
    /// </summary>
    /// <remarks>
    /// Stateless — безопасно регистрировать как Singleton в DI-контейнере.
    /// </remarks>
    public sealed class PublishedDishSnapshotBuilder : IPublishedDishSnapshotBuilder
    {
        /// <inheritdoc/>
        public string Build(Dish dish)
        {
            ArgumentNullException.ThrowIfNull(dish);

            PublishedDishSnapshot snapshot = MapDish(dish);
            return JsonSerializer.Serialize(snapshot, SnapshotJsonOptions.Default);
        }

        private static PublishedDishSnapshot MapDish(Dish dish) => new(
            Name: dish.Name,
            Slug: dish.Slug,
            ShortDescription: dish.ShortDescription,
            Description: dish.Description,
            HistoryText: dish.HistoryText,
            MainImageId: dish.MainImageId,
            DifficultyLevel: dish.DifficultyLevel,
            CostEstimate: dish.CostEstimate,
            OwnerType: dish.OwnerType,
            DietLabelsMask: dish.DietLabelsMask,
            AllergensMask: dish.AllergensMask,
            HasUnverifiedAllergens: dish.HasUnverifiedAllergens,
            Recipe: MapRecipe(dish.Recipe),
            Categories: dish.Categories
                .Select(dc => new PublishedCategorySnapshotDto(dc.CategoryId))
                .ToList(),
            Tags: dish.Tags
                .Select(dt => new PublishedTagSnapshotDto(dt.TagId))
                .ToList());

        private static PublishedRecipeDto MapRecipe(Recipe recipe) => new(
            IntroductionText: recipe.IntroductionText,
            ServingsDefault: recipe.ServingsDefault,
            IsAlcoholic: recipe.IsAlcoholic,
            AuthorTips: recipe.AuthorTips,
            ServingSuggestions: recipe.ServingSuggestions,
            Notes: recipe.Notes,
            Timing: MapTiming(recipe.Timing),
            Yield: MapYield(recipe.Yield),
            Nutrition: recipe.Nutrition is null ? null : MapNutrition(recipe.Nutrition),
            Steps: recipe.Steps
                .OrderBy(s => s.Order)
                .Select(MapStep)
                .ToList(),
            Ingredients: recipe.Ingredients
                .OrderBy(i => i.Order)
                .Select(MapIngredient)
                .ToList());

        private static PublishedTimingDto MapTiming(Timing timing) => new(
            PrepTimeMinutes: timing.PrepTimeMinutes,
            CookTimeMinutes: timing.CookTimeMinutes,
            RestTimeMinutes: timing.RestTimeMinutes,
            ActiveTimeMinutes: timing.ActiveTimeMinutes,
            TotalTimeMinutes: timing.TotalTimeMinutes,
            IsTotalManual: timing.IsTotalManual);

        private static PublishedYieldDto MapYield(Yield yield) => new(
            QuantityTotal: yield.QuantityTotal,
            YieldUnit: yield.YieldUnit,
            ServingsCount: yield.ServingsCount,
            GramsPerServing: yield.GramsPerServing);

        private static PublishedNutritionDto MapNutrition(Nutrition nutrition) => new(
            CalcMethod: nutrition.CalcMethod,
            Calories: nutrition.Calories,
            Proteins: nutrition.Proteins,
            Fats: nutrition.Fats,
            SaturatedFats: nutrition.SaturatedFats,
            Carbs: nutrition.Carbs,
            Sugar: nutrition.Sugar,
            Fiber: nutrition.Fiber,
            Salt: nutrition.Salt);

        private static PublishedRecipeStepDto MapStep(RecipeStep step) => new(
            Id: step.Id,
            Order: step.Order,
            Title: step.Title,
            Description: step.Description,
            ImageMediaId: step.ImageMediaId,
            VideoUrl: step.VideoUrl,
            TemperatureCelsius: step.TemperatureCelsius,
            TimerMinutes: step.TimerMinutes);

        // Дискриминация по природе: IngredientId.HasValue == catalog, иначе freeform.
        // Инвариант XOR обеспечен Domain-фабриками (ADR-0012) — здесь это безусловная развилка.
        private static PublishedRecipeIngredientDto MapIngredient(RecipeIngredient ingredient)
        {
            if (ingredient.IngredientId.HasValue)
            {
                return new PublishedCatalogIngredientDto(
                    Id: ingredient.Id,
                    Order: ingredient.Order,
                    Quantity: ingredient.Quantity,
                    MeasureUnitId: ingredient.MeasureUnitId,
                    IsOptional: ingredient.IsOptional,
                    PreparationNote: ingredient.PreparationNote,
                    IngredientId: ingredient.IngredientId.Value,
                    IngredientSpecId: ingredient.IngredientSpecId);
            }

            return new PublishedFreeformIngredientDto(
                Id: ingredient.Id,
                Order: ingredient.Order,
                Quantity: ingredient.Quantity,
                MeasureUnitId: ingredient.MeasureUnitId,
                IsOptional: ingredient.IsOptional,
                PreparationNote: ingredient.PreparationNote,
                FreeformText: ingredient.FreeformText!);
        }
    }
}
