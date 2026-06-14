using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetYield
{
    /// <summary>
    /// Обработчик команды <see cref="SetYieldCommand"/> (UC-DSH-041).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с подгруженным <see cref="Dish.Recipe"/> через
    ///         <see cref="IDishRepository.GetByIdWithRecipeAsync"/> — нужен 1:1
    ///         <see cref="Yield"/>.</item>
    ///   <item>POL-001: автор блюда или Admin.</item>
    ///   <item>Делегирование Domain: <see cref="Dish.UpdateYield"/>. При нарушении
    ///         инвариантов — <see cref="DishesErrors.InvalidYield"/>.</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
    /// </list>
    /// </remarks>
    public sealed class SetYieldCommandHandler : ICommandHandler<SetYieldCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetYieldCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public SetYieldCommandHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IDomainEventDispatcher eventDispatcher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(SetYieldCommand request, CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdWithRecipeAsync(request.DishId, cancellationToken);
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

            Result updateResult = dish.UpdateYield(
                quantityTotal: request.QuantityTotal,
                yieldUnit: request.YieldUnit,
                servingsCount: request.ServingsCount,
                gramsPerServing: request.GramsPerServing,
                utcNow: _clock.UtcNow);

            if (updateResult.IsFailure)
            {
                return updateResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
