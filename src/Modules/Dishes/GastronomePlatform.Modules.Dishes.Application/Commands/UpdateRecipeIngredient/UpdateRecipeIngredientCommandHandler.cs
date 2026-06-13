using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using MediatR;

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
    ///   <item>Сбор словаря маркеров по текущему составу (catalog-позиции) и
    ///         <see cref="Dish.RecalculateDishMarkers"/>.</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
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
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeIngredientCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="ingredientRepository">Репозиторий справочника ингредиентов.</param>
        /// <param name="specRepository">Репозиторий справочника спецификаций.</param>
        /// <param name="measureUnitRepository">Репозиторий справочника единиц измерения.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public UpdateRecipeIngredientCommandHandler(
            IDishRepository dishRepository,
            IIngredientRepository ingredientRepository,
            IIngredientSpecRepository specRepository,
            IMeasureUnitRepository measureUnitRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IPublisher publisher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _specRepository = specRepository ?? throw new ArgumentNullException(nameof(specRepository));
            _measureUnitRepository = measureUnitRepository ?? throw new ArgumentNullException(nameof(measureUnitRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
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

            await RecalculateMarkersAsync(dish, utcNow, cancellationToken);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await PublishDomainEventsAsync(dish, cancellationToken);

            return Result.Success();
        }

        private async Task RecalculateMarkersAsync(Dish dish, DateTimeOffset utcNow, CancellationToken ct)
        {
            List<Guid> catalogIds = dish.Recipe.Ingredients
                .Where(ri => ri.IngredientId.HasValue)
                .Select(ri => ri.IngredientId!.Value)
                .Distinct()
                .ToList();

            IReadOnlyDictionary<Guid, IngredientMarkers> markers = catalogIds.Count == 0
                ? new Dictionary<Guid, IngredientMarkers>(capacity: 0)
                : await _ingredientRepository.GetMarkersByIdsAsync(catalogIds, ct);

            dish.RecalculateDishMarkers(markers, utcNow);
        }

        private async Task PublishDomainEventsAsync(Dish dish, CancellationToken ct)
        {
            List<Common.Domain.Events.IDomainEvent> events = dish.DomainEvents.ToList();
            dish.ClearDomainEvents();

            foreach (Common.Domain.Events.IDomainEvent domainEvent in events)
            {
                await _publisher.Publish(domainEvent, ct);
            }
        }
    }
}
