using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using MediatR;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ReorderRecipeIngredients
{
    /// <summary>
    /// Обработчик команды <see cref="ReorderRecipeIngredientsCommand"/> (UC-DSH-033).
    /// </summary>
    /// <remarks>
    /// Состав не меняется — <see cref="Dish.RecalculateDishMarkers"/> не вызывается.
    /// </remarks>
    public sealed class ReorderRecipeIngredientsCommandHandler
        : ICommandHandler<ReorderRecipeIngredientsCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ReorderRecipeIngredientsCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public ReorderRecipeIngredientsCommandHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IPublisher publisher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            ReorderRecipeIngredientsCommand request,
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

            Result reorderResult = dish.ReorderRecipeIngredients(request.OrderedIngredientIds, _clock.UtcNow);
            if (reorderResult.IsFailure)
            {
                return reorderResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await PublishDomainEventsAsync(dish, cancellationToken);

            return Result.Success();
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
