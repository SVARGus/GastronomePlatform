using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Services;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipeIngredient
{
    /// <summary>
    /// Обработчик команды <see cref="UpdateRecipeIngredientCommand"/> (UC-DSH-031).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с полным рецептом.</item>
    ///   <item>POL-001: автор блюда или Admin.</item>
    ///   <item>Поиск позиции по <c>RecipeIngredientId</c>. Если нет —
    ///         <see cref="DishesErrors.RecipeIngredientNotFound"/>.</item>
    ///   <item>Если в команде задан <c>IngredientId</c> — проверка существования и активности
    ///         ингредиента; если задан <c>IngredientSpecId</c> — проверка существования
    ///         и принадлежности родительскому ингредиенту.</item>
    ///   <item>Проверка существования <see cref="MeasureUnit"/>.</item>
    ///   <item>Вызов <see cref="Dish.UpdateRecipeIngredient"/> — Domain валидирует XOR и
    ///         <c>Quantity &gt; 0</c>; возвращает <see cref="Result"/>.</item>
    ///   <item>Пересчёт маркеров через <see cref="IDishMarkersRecalculator"/>.</item>
    ///   <item>Сохранение и публикация доменных событий через <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// </remarks>
    public sealed class UpdateRecipeIngredientCommandHandler : ICommandHandler<UpdateRecipeIngredientCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IIngredientSpecRepository _specRepository;
        private readonly IMeasureUnitRepository _measureUnitRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDishMarkersRecalculator _markersRecalculator;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeIngredientCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="ingredientRepository">Репозиторий справочника ингредиентов (для existence/IsActive-проверок).</param>
        /// <param name="specRepository">Репозиторий справочника спецификаций.</param>
        /// <param name="measureUnitRepository">Репозиторий справочника единиц измерения.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="markersRecalculator">Сервис пересчёта маркеров блюда.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public UpdateRecipeIngredientCommandHandler(
            IDishRepository dishRepository,
            IIngredientRepository ingredientRepository,
            IIngredientSpecRepository specRepository,
            IMeasureUnitRepository measureUnitRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IDishMarkersRecalculator markersRecalculator,
            IDomainEventDispatcher eventDispatcher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _specRepository = specRepository ?? throw new ArgumentNullException(nameof(specRepository));
            _measureUnitRepository = measureUnitRepository ?? throw new ArgumentNullException(nameof(measureUnitRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _markersRecalculator = markersRecalculator ?? throw new ArgumentNullException(nameof(markersRecalculator));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            UpdateRecipeIngredientCommand request,
            CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdWithFullRecipeAsync(request.DishId, cancellationToken);
            if (dish is null)
            {
                return DishesErrors.DishNotFound;
            }

            Guid actorUserId = _currentUser.UserId!.Value;
            bool isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN);
            if (dish.AuthorUserId != actorUserId && !isAdmin)
            {
                return DishesErrors.NotDishOwner;
            }

            // Существование позиции на уровне домена не проверяется до вызова Update —
            // оно отлавливается в Recipe.UpdateIngredient через RecipeIngredientNotFound.
            // Делаем явную раннюю проверку, чтобы не гонять справочные проверки впустую.
            bool positionExists = dish.Recipe.Ingredients.Any(ri => ri.Id == request.RecipeIngredientId);
            if (!positionExists)
            {
                return DishesErrors.RecipeIngredientNotFound;
            }

            if (request.IngredientId.HasValue)
            {
                Ingredient? ingredient = await _ingredientRepository.GetByIdAsync(
                    request.IngredientId.Value,
                    cancellationToken);
                if (ingredient is null)
                {
                    return DishesErrors.IngredientNotFound;
                }

                if (!ingredient.IsActive)
                {
                    return DishesErrors.IngredientInactive;
                }

                if (request.IngredientSpecId.HasValue)
                {
                    IngredientSpec? spec = await _specRepository.GetByIdAsync(
                        request.IngredientSpecId.Value,
                        cancellationToken);
                    if (spec is null)
                    {
                        return DishesErrors.IngredientSpecNotFound;
                    }

                    if (spec.IngredientId != request.IngredientId.Value)
                    {
                        return DishesErrors.IngredientSpecMismatch;
                    }
                }
            }

            MeasureUnit? unit = await _measureUnitRepository.GetByIdAsync(request.MeasureUnitId, cancellationToken);
            if (unit is null)
            {
                return DishesErrors.MeasureUnitNotFound;
            }

            DateTimeOffset utcNow = _clock.UtcNow;

            Result updateResult = dish.UpdateRecipeIngredient(
                recipeIngredientId: request.RecipeIngredientId,
                ingredientId: request.IngredientId,
                ingredientSpecId: request.IngredientSpecId,
                freeformText: request.FreeformText,
                quantity: request.Quantity,
                measureUnitId: request.MeasureUnitId,
                isOptional: request.IsOptional,
                preparationNote: request.PreparationNote,
                utcNow: utcNow);

            if (updateResult.IsFailure)
            {
                return updateResult;
            }

            await _markersRecalculator.RecalculateAsync(dish, utcNow, cancellationToken);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
