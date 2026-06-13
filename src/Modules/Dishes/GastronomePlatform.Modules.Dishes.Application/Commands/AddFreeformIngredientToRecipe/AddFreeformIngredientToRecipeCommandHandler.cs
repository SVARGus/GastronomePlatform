using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddFreeformIngredientToRecipe
{
    /// <summary>
    /// Обработчик команды <see cref="AddFreeformIngredientToRecipeCommand"/> (UC-DSH-030, freeform-ветка).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с полным рецептом — нужен текущий состав для последующего
    ///         <see cref="Dish.RecalculateDishMarkers"/>.</item>
    ///   <item>POL-001: автор блюда или Admin. Иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Проверка существования <see cref="MeasureUnit"/>.</item>
    ///   <item>Вызов <see cref="Dish.AddRecipeIngredientFreeform"/> — позиция добавляется
    ///         с <c>Order = max+1</c>, <c>IngredientId = null</c>, <c>FreeformText</c> сохраняется.</item>
    ///   <item>Сбор словаря маркеров для catalog-позиций текущего состава (freeform-позиция
    ///         в словарь не попадает — у неё нет справочных маркеров) и вызов
    ///         <see cref="Dish.RecalculateDishMarkers"/>. Последний поднимет
    ///         <see cref="Dish.HasUnverifiedAllergens"/> = <see langword="true"/>
    ///         за счёт нового freeform-элемента.</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
    /// </list>
    /// </remarks>
    public sealed class AddFreeformIngredientToRecipeCommandHandler
        : ICommandHandler<AddFreeformIngredientToRecipeCommand, AddFreeformIngredientToRecipeResult>
    {
        private readonly IDishRepository _dishRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly IMeasureUnitRepository _measureUnitRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AddFreeformIngredientToRecipeCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="ingredientRepository">Репозиторий справочника ингредиентов (нужен для маркеров catalog-позиций).</param>
        /// <param name="measureUnitRepository">Репозиторий справочника единиц измерения.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public AddFreeformIngredientToRecipeCommandHandler(
            IDishRepository dishRepository,
            IIngredientRepository ingredientRepository,
            IMeasureUnitRepository measureUnitRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IPublisher publisher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _measureUnitRepository = measureUnitRepository ?? throw new ArgumentNullException(nameof(measureUnitRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result<AddFreeformIngredientToRecipeResult>> Handle(
            AddFreeformIngredientToRecipeCommand request,
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

            MeasureUnit? unit = await _measureUnitRepository.GetByIdAsync(request.MeasureUnitId, cancellationToken);
            if (unit is null)
            {
                return DishesErrors.MeasureUnitNotFound;
            }

            DateTimeOffset utcNow = _clock.UtcNow;

            Guid newRecipeIngredientId = dish.AddRecipeIngredientFreeform(
                freeformText: request.FreeformText,
                quantity: request.Quantity,
                measureUnitId: request.MeasureUnitId,
                isOptional: request.IsOptional,
                preparationNote: request.PreparationNote,
                utcNow: utcNow);

            await RecalculateMarkersAsync(dish, utcNow, cancellationToken);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await PublishDomainEventsAsync(dish, cancellationToken);

            return new AddFreeformIngredientToRecipeResult(newRecipeIngredientId);
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
