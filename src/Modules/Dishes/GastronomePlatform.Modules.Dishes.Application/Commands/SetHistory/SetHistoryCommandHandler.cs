using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetHistory
{
    /// <summary>
    /// Обработчик команды <see cref="SetHistoryCommand"/> (UC-DSH-010).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка корневого <see cref="Dish"/> через
    ///         <see cref="IDishRepository.GetByIdAsync"/> — <see cref="Dish.Recipe"/>
    ///         и подколлекции не нужны.</item>
    ///   <item>POL-001: автор блюда или Admin. Иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Замена поля через <see cref="Dish.UpdateHistory"/> — Domain поднимает
    ///         <c>DishUpdatedEvent</c>.</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
    /// </list>
    /// </remarks>
    public sealed class SetHistoryCommandHandler : ICommandHandler<SetHistoryCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetHistoryCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public SetHistoryCommandHandler(
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
        public async Task<Result> Handle(SetHistoryCommand request, CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdAsync(request.DishId, cancellationToken);
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

            dish.UpdateHistory(request.HistoryText, _clock.UtcNow);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
