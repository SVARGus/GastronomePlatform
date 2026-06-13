using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Services;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

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
    ///   <item>Пересчёт маркеров через <see cref="IDishMarkersRecalculator"/>
    ///         по оставшимся catalog-позициям.</item>
    ///   <item>Сохранение и публикация доменных событий через <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// </remarks>
    public sealed class RemoveRecipeIngredientCommandHandler : ICommandHandler<RemoveRecipeIngredientCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDishMarkersRecalculator _markersRecalculator;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RemoveRecipeIngredientCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="markersRecalculator">Сервис пересчёта маркеров блюда.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public RemoveRecipeIngredientCommandHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IDishMarkersRecalculator markersRecalculator,
            IDomainEventDispatcher eventDispatcher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _markersRecalculator = markersRecalculator ?? throw new ArgumentNullException(nameof(markersRecalculator));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
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

            await _markersRecalculator.RecalculateAsync(dish, utcNow, cancellationToken);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
