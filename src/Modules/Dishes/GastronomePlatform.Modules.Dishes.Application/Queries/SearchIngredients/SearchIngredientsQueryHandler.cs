using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchIngredients
{
    /// <summary>
    /// Обработчик запроса <see cref="SearchIngredientsQuery"/> (UC-DSH-062).
    /// </summary>
    public sealed class SearchIngredientsQueryHandler
        : IQueryHandler<SearchIngredientsQuery, IReadOnlyList<IngredientDto>>
    {
        private readonly IIngredientRepository _ingredientRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SearchIngredientsQueryHandler"/>.
        /// </summary>
        /// <param name="ingredientRepository">Репозиторий ингредиентов.</param>
        public SearchIngredientsQueryHandler(IIngredientRepository ingredientRepository)
        {
            _ingredientRepository = ingredientRepository
                ?? throw new ArgumentNullException(nameof(ingredientRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyList<IngredientDto>>> Handle(
            SearchIngredientsQuery request,
            CancellationToken cancellationToken)
        {
            string prefix = request.Query.Trim();
            if (prefix.Length == 0)
            {
                return Result<IReadOnlyList<IngredientDto>>.Success(Array.Empty<IngredientDto>());
            }

            IReadOnlyList<Ingredient> ingredients = await _ingredientRepository
                .SearchActiveByNamePrefixAsync(prefix, request.Limit, cancellationToken);

            IReadOnlyList<IngredientDto> dtos = ingredients.Select(MapToDto).ToList();
            return Result<IReadOnlyList<IngredientDto>>.Success(dtos);
        }

        internal static IngredientDto MapToDto(Ingredient i) => new(
            Id: i.Id,
            Name: i.Name,
            PluralName: i.PluralName,
            Description: i.Description,
            ImageMediaId: i.ImageMediaId,
            IsLiquid: i.IsLiquid,
            DensityApprox: i.DensityApprox,
            IsAllergen: i.IsAllergen,
            AllergenType: i.AllergenType,
            DietConflictsMask: i.DietConflictsMask,
            BaseMeasureUnitId: i.BaseMeasureUnitId,
            DefaultNutritionId: i.DefaultNutritionId,
            IsActive: i.IsActive);
    }
}
