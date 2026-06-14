using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Application.Queries.SearchIngredients;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetIngredientById
{
    /// <summary>
    /// Обработчик запроса <see cref="GetIngredientByIdQuery"/> (UC-DSH-063).
    /// </summary>
    public sealed class GetIngredientByIdQueryHandler
        : IQueryHandler<GetIngredientByIdQuery, IngredientDto>
    {
        private readonly IIngredientRepository _ingredientRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetIngredientByIdQueryHandler"/>.
        /// </summary>
        /// <param name="ingredientRepository">Репозиторий ингредиентов.</param>
        public GetIngredientByIdQueryHandler(IIngredientRepository ingredientRepository)
        {
            _ingredientRepository = ingredientRepository
                ?? throw new ArgumentNullException(nameof(ingredientRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<IngredientDto>> Handle(
            GetIngredientByIdQuery request,
            CancellationToken cancellationToken)
        {
            Ingredient? ingredient = await _ingredientRepository
                .GetByIdAsync(request.IngredientId, cancellationToken);

            if (ingredient is null)
            {
                return DishesErrors.IngredientNotFound;
            }

            return SearchIngredientsQueryHandler.MapToDto(ingredient);
        }
    }
}
