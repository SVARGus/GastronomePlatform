using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.CreateIngredient
{
    /// <summary>
    /// Обработчик команды <see cref="CreateIngredientCommand"/> (UC-DSH-110).
    /// </summary>
    /// <remarks>
    /// Поток:
    /// <list type="number">
    ///   <item>Проверка существования <see cref="MeasureUnit"/> по
    ///         <see cref="CreateIngredientCommand.BaseMeasureUnitId"/>.</item>
    ///   <item>Проверка уникальности <see cref="Ingredient.Name"/> (case-sensitive по индексу БД).
    ///         При коллизии — <see cref="DishesErrors.IngredientNameTaken"/>.</item>
    ///   <item>Создание через <see cref="Ingredient.Create"/> и сохранение.</item>
    /// </list>
    /// Существование <c>DefaultNutritionId</c> на Этапе 2 не проверяется — гарантируется
    /// FK-constraint в БД (<c>DefaultNutritionId</c> → <c>Nutritions.Id</c>).
    /// При несуществующем Id запрос завершится <c>DbUpdateException</c>; на Этапе 2
    /// это допустимо — admin-сценарий, отдельная обработка появится по требованию.
    /// </remarks>
    public sealed class CreateIngredientCommandHandler
        : ICommandHandler<CreateIngredientCommand, CreateIngredientResult>
    {
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IMeasureUnitRepository _measureUnitRepository;
        private readonly IDateTimeProvider _clock;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CreateIngredientCommandHandler"/>.
        /// </summary>
        /// <param name="ingredientRepository">Репозиторий ингредиентов.</param>
        /// <param name="measureUnitRepository">Репозиторий единиц измерения.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        public CreateIngredientCommandHandler(
            IIngredientRepository ingredientRepository,
            IMeasureUnitRepository measureUnitRepository,
            IDateTimeProvider clock)
        {
            _ingredientRepository = ingredientRepository
                ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _measureUnitRepository = measureUnitRepository
                ?? throw new ArgumentNullException(nameof(measureUnitRepository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc/>
        public async Task<Result<CreateIngredientResult>> Handle(
            CreateIngredientCommand request,
            CancellationToken cancellationToken)
        {
            MeasureUnit? unit = await _measureUnitRepository.GetByIdAsync(
                request.BaseMeasureUnitId, cancellationToken);
            if (unit is null)
            {
                return DishesErrors.MeasureUnitNotFound;
            }

            Ingredient? existing = await _ingredientRepository.GetByNameAsync(
                request.Name, cancellationToken);
            if (existing is not null)
            {
                return DishesErrors.IngredientNameTaken;
            }

            Ingredient ingredient = Ingredient.Create(
                name: request.Name,
                pluralName: request.PluralName,
                description: request.Description,
                imageMediaId: request.ImageMediaId,
                isLiquid: request.IsLiquid,
                densityApprox: request.DensityApprox,
                isAllergen: request.IsAllergen,
                allergenType: request.AllergenType,
                dietConflictsMask: request.DietConflictsMask,
                baseMeasureUnitId: request.BaseMeasureUnitId,
                defaultNutritionId: request.DefaultNutritionId,
                createdAt: _clock.UtcNow);

            await _ingredientRepository.AddAsync(ingredient, cancellationToken);
            await _ingredientRepository.SaveChangesAsync(cancellationToken);

            return new CreateIngredientResult(ingredient.Id);
        }
    }
}
