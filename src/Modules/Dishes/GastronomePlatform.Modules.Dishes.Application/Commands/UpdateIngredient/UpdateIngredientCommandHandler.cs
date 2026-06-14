using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateIngredient
{
    /// <summary>
    /// Обработчик команды <see cref="UpdateIngredientCommand"/> (UC-DSH-111).
    /// </summary>
    /// <remarks>
    /// Поток:
    /// <list type="number">
    ///   <item>Загрузка ингредиента → <see cref="DishesErrors.IngredientNotFound"/>.</item>
    ///   <item>Проверка существования новой <see cref="MeasureUnit"/>.</item>
    ///   <item>Проверка уникальности нового <c>Name</c> — конфликт только с другой записью
    ///         (та же запись с тем же именем — это просто без изменений).</item>
    ///   <item>Делегирование Domain: <see cref="Ingredient.Update"/>.</item>
    ///   <item>Сохранение.</item>
    /// </list>
    /// <para>
    /// Существующие <c>Dish.AllergensMask</c> и <c>Dish.DietLabelsMask</c> при изменении
    /// <c>AllergenType</c>/<c>DietConflictsMask</c> ингредиента <b>не пересчитываются</b>.
    /// Массовая инвалидация — фоновая задача (см. README §UC-DSH-111).
    /// </para>
    /// </remarks>
    public sealed class UpdateIngredientCommandHandler : ICommandHandler<UpdateIngredientCommand>
    {
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IMeasureUnitRepository _measureUnitRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateIngredientCommandHandler"/>.
        /// </summary>
        /// <param name="ingredientRepository">Репозиторий ингредиентов.</param>
        /// <param name="measureUnitRepository">Репозиторий единиц измерения.</param>
        public UpdateIngredientCommandHandler(
            IIngredientRepository ingredientRepository,
            IMeasureUnitRepository measureUnitRepository)
        {
            _ingredientRepository = ingredientRepository
                ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _measureUnitRepository = measureUnitRepository
                ?? throw new ArgumentNullException(nameof(measureUnitRepository));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            UpdateIngredientCommand request,
            CancellationToken cancellationToken)
        {
            Ingredient? ingredient = await _ingredientRepository.GetByIdAsync(
                request.IngredientId, cancellationToken);
            if (ingredient is null)
            {
                return DishesErrors.IngredientNotFound;
            }

            MeasureUnit? unit = await _measureUnitRepository.GetByIdAsync(
                request.BaseMeasureUnitId, cancellationToken);
            if (unit is null)
            {
                return DishesErrors.MeasureUnitNotFound;
            }

            // Конфликт по имени — только с другим ингредиентом.
            Ingredient? duplicate = await _ingredientRepository.GetByNameAsync(
                request.Name, cancellationToken);
            if (duplicate is not null && duplicate.Id != ingredient.Id)
            {
                return DishesErrors.IngredientNameTaken;
            }

            ingredient.Update(
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
                defaultNutritionId: request.DefaultNutritionId);

            await _ingredientRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
