using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Services
{
    /// <summary>
    /// Реализация <see cref="IDishMarkersRecalculator"/> через
    /// <see cref="IIngredientRepository.GetMarkersByIdsAsync"/>.
    /// </summary>
    public sealed class DishMarkersRecalculator : IDishMarkersRecalculator
    {
        private readonly IIngredientRepository _ingredientRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DishMarkersRecalculator"/>.
        /// </summary>
        /// <param name="ingredientRepository">Репозиторий справочника ингредиентов.</param>
        public DishMarkersRecalculator(IIngredientRepository ingredientRepository)
        {
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
        }

        /// <inheritdoc/>
        public async Task RecalculateAsync(Dish dish, DateTimeOffset utcNow, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dish);

            List<Guid> catalogIds = dish.Recipe.Ingredients
                .Where(ri => ri.IngredientId.HasValue)
                .Select(ri => ri.IngredientId!.Value)
                .Distinct()
                .ToList();

            IReadOnlyDictionary<Guid, IngredientMarkers> markers = catalogIds.Count == 0
                ? new Dictionary<Guid, IngredientMarkers>(capacity: 0)
                : await _ingredientRepository.GetMarkersByIdsAsync(catalogIds, cancellationToken);

            dish.RecalculateDishMarkers(markers, utcNow);
        }
    }
}
