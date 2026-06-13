using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.RemoveRecipeIngredient
{
    /// <summary>
    /// Обработчик команды <see cref="RemoveRecipeIngredientCommand"/> (UC-DSH-032).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с полным рецептом.</item>
    ///   <item>POL-001: автор блюда или Admin.</item>
    ///   <item>Вызов <see cref="Dish.RemoveRecipeIngredient"/> — Domain удаляет позицию
    ///         и переупорядочивает оставшиеся. <see cref="DishesErrors.RecipeIngredientNotFound"/>
    ///         при отсутствии позиции.</item>
    ///   <item>Сбор словаря маркеров по оставшимся catalog-позициям и
    ///         <see cref="Dish.RecalculateDishMarkers"/>.</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
    /// </list>
    /// </remarks>
    public sealed class RemoveRecipeIngredientCommandHandler : ICommandHandler<RemoveRecipeIngredientCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RemoveRecipeIngredientCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="ingredientRepository">Репозиторий справочника ингредиентов (для маркеров).</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public RemoveRecipeIngredientCommandHandler(
            IDishRepository dishRepository,
            IIngredientRepository ingredientRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IPublisher publisher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _ingredientRepository = ingredientRepository ?? throw new ArgumentNullException(nameof(ingredientRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            RemoveRecipeIngredientCommand request,
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

            DateTimeOffset utcNow = _clock.UtcNow;

            Result removeResult = dish.RemoveRecipeIngredient(request.RecipeIngredientId, utcNow);
            if (removeResult.IsFailure)
            {
                return removeResult;
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
