using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.DeactivateIngredient
{
    /// <summary>
    /// Обработчик команды <see cref="DeactivateIngredientCommand"/> (UC-DSH-112).
    /// </summary>
    public sealed class DeactivateIngredientCommandHandler
        : ICommandHandler<DeactivateIngredientCommand>
    {
        private readonly IIngredientRepository _ingredientRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DeactivateIngredientCommandHandler"/>.
        /// </summary>
        /// <param name="ingredientRepository">Репозиторий ингредиентов.</param>
        public DeactivateIngredientCommandHandler(IIngredientRepository ingredientRepository)
        {
            _ingredientRepository = ingredientRepository
                ?? throw new ArgumentNullException(nameof(ingredientRepository));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            DeactivateIngredientCommand request,
            CancellationToken cancellationToken)
        {
            Ingredient? ingredient = await _ingredientRepository.GetByIdAsync(
                request.IngredientId, cancellationToken);
            if (ingredient is null)
            {
                return DishesErrors.IngredientNotFound;
            }

            ingredient.Deactivate();

            await _ingredientRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
