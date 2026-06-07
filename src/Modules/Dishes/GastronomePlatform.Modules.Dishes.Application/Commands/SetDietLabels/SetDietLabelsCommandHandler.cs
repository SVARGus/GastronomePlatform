using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetDietLabels
{
    /// <summary>
    /// Обработчик команды <see cref="SetDietLabelsCommand"/> (UC-DSH-009).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с полным рецептом (нужен состав ингредиентов
    ///         для сбора словаря конфликтов).</item>
    ///   <item>POL-001: автор блюда или Admin. Иначе <c>NotDishOwner</c>.</item>
    ///   <item>Сбор словаря <c>IngredientId → DietConflictsMask</c> через
    ///         <see cref="IIngredientRepository.GetMarkersByIdsAsync"/>.</item>
    ///   <item>Вызов <see cref="Dish.SetDietLabels"/> — при конфликте возвращается
    ///         <c>DishesErrors.DietLabelsConflictWithComposition</c> (409).</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
    /// </list>
    /// </remarks>
    public sealed class SetDietLabelsCommandHandler : ICommandHandler<SetDietLabelsCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly IIngredientRepository _ingredientRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetDietLabelsCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="ingredientRepository">Репозиторий справочника ингредиентов.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public SetDietLabelsCommandHandler(
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
        public async Task<Result> Handle(SetDietLabelsCommand request, CancellationToken cancellationToken)
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

            IReadOnlyDictionary<Guid, DietLabels> conflictsByIngredient =
                await BuildConflictsMapAsync(dish, cancellationToken);

            Result result = dish.SetDietLabels(
                dietLabelsMask: request.DietLabelsMask,
                ingredientConflicts: conflictsByIngredient,
                utcNow: _clock.UtcNow);

            if (result.IsFailure)
            {
                return result;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await PublishDomainEventsAsync(dish, cancellationToken);

            return Result.Success();
        }

        // Собираем словарь только для catalog-позиций — freeform-позиции не имеют
        // справочной маски конфликтов и в проверке не участвуют (ADR-0016).
        private async Task<IReadOnlyDictionary<Guid, DietLabels>> BuildConflictsMapAsync(
            Dish dish,
            CancellationToken cancellationToken)
        {
            List<Guid> catalogIngredientIds = dish.Recipe.Ingredients
                .Where(ri => ri.IngredientId.HasValue)
                .Select(ri => ri.IngredientId!.Value)
                .Distinct()
                .ToList();

            if (catalogIngredientIds.Count == 0)
            {
                return new Dictionary<Guid, DietLabels>(capacity: 0);
            }

            IReadOnlyDictionary<Guid, IngredientMarkers> markers =
                await _ingredientRepository.GetMarkersByIdsAsync(catalogIngredientIds, cancellationToken);

            Dictionary<Guid, DietLabels> conflicts = new(capacity: markers.Count);
            foreach (KeyValuePair<Guid, IngredientMarkers> pair in markers)
            {
                conflicts[pair.Key] = pair.Value.DietConflicts;
            }

            return conflicts;
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
