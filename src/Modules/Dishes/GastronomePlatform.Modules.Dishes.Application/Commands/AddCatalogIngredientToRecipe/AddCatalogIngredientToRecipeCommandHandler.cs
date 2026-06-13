using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Services;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddCatalogIngredientToRecipe
{
    /// <summary>
    /// Обработчик команды <see cref="AddCatalogIngredientToRecipeCommand"/> (UC-DSH-030, catalog-ветка).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с полным рецептом — нужен текущий состав для последующего
    ///         <see cref="Dish.RecalculateDishMarkers"/>.</item>
    ///   <item>POL-001: автор блюда или Admin. Иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Проверка существования и активности <see cref="Ingredient"/>.</item>
    ///   <item>Если задан <c>IngredientSpecId</c> — проверка существования и принадлежности
    ///         к указанному ингредиенту.</item>
    ///   <item>Проверка существования <see cref="MeasureUnit"/>.</item>
    ///   <item>Вызов <see cref="Dish.AddRecipeIngredientFromCatalog"/> — позиция добавляется
    ///         с <c>Order = max+1</c>.</item>
    ///   <item>Пересчёт маркеров через <see cref="IDishMarkersRecalculator"/>
    ///         (<see cref="Dish.AllergensMask"/>, <see cref="Dish.HasUnverifiedAllergens"/>,
    ///         автокоррекция <see cref="Dish.DietLabelsMask"/> — ADR-0016).</item>
    ///   <item>Сохранение и публикация доменных событий через <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// </remarks>
    public sealed class AddCatalogIngredientToRecipeCommandHandler
        : ICommandHandler<AddCatalogIngredientToRecipeCommand, AddCatalogIngredientToRecipeResult>
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
        /// Инициализирует новый экземпляр <see cref="AddCatalogIngredientToRecipeCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="ingredientRepository">Репозиторий справочника ингредиентов (нужен для existence/IsActive-проверок).</param>
        /// <param name="specRepository">Репозиторий справочника спецификаций.</param>
        /// <param name="measureUnitRepository">Репозиторий справочника единиц измерения.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="markersRecalculator">Сервис пересчёта маркеров блюда.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public AddCatalogIngredientToRecipeCommandHandler(
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
        public async Task<Result<AddCatalogIngredientToRecipeResult>> Handle(
            AddCatalogIngredientToRecipeCommand request,
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

            Ingredient? ingredient = await _ingredientRepository.GetByIdAsync(request.IngredientId, cancellationToken);
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
                IngredientSpec? spec = await _specRepository.GetByIdAsync(request.IngredientSpecId.Value, cancellationToken);
                if (spec is null)
                {
                    return DishesErrors.IngredientSpecNotFound;
                }

                if (spec.IngredientId != request.IngredientId)
                {
                    return DishesErrors.IngredientSpecMismatch;
                }
            }

            MeasureUnit? unit = await _measureUnitRepository.GetByIdAsync(request.MeasureUnitId, cancellationToken);
            if (unit is null)
            {
                return DishesErrors.MeasureUnitNotFound;
            }

            DateTimeOffset utcNow = _clock.UtcNow;

            Guid newRecipeIngredientId = dish.AddRecipeIngredientFromCatalog(
                ingredientId: request.IngredientId,
                ingredientSpecId: request.IngredientSpecId,
                quantity: request.Quantity,
                measureUnitId: request.MeasureUnitId,
                isOptional: request.IsOptional,
                preparationNote: request.PreparationNote,
                utcNow: utcNow);

            await _markersRecalculator.RecalculateAsync(dish, utcNow, cancellationToken);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return new AddCatalogIngredientToRecipeResult(newRecipeIngredientId);
        }
    }
}
