using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetTiming
{
    /// <summary>
    /// Обработчик команды <see cref="SetTimingCommand"/> (UC-DSH-040).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с подгруженным <see cref="Dish.Recipe"/> через
    ///         <see cref="IDishRepository.GetByIdWithRecipeAsync"/> — нужен 1:1
    ///         <see cref="Timing"/>.</item>
    ///   <item>POL-001: автор блюда или Admin. Иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Делегирование Domain: <see cref="Dish.UpdateTiming"/>. Domain
    ///         повторно проверяет неотрицательность как defense-in-depth и при
    ///         нарушении возвращает <see cref="DishesErrors.InvalidTiming"/>.</item>
    ///   <item>Сохранение и публикация доменных событий
    ///         (<c>DishUpdatedEvent</c> поднимается из
    ///         <see cref="Dish.MarkAsUpdated"/>).</item>
    /// </list>
    /// </remarks>
    public sealed class SetTimingCommandHandler : ICommandHandler<SetTimingCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetTimingCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public SetTimingCommandHandler(
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
        public async Task<Result> Handle(SetTimingCommand request, CancellationToken cancellationToken)
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

            Result updateResult = dish.UpdateTiming(
                prepTimeMinutes: request.PrepTimeMinutes,
                cookTimeMinutes: request.CookTimeMinutes,
                restTimeMinutes: request.RestTimeMinutes,
                activeTimeMinutes: request.ActiveTimeMinutes,
                totalTimeMinutes: request.TotalTimeMinutes,
                isTotalManual: request.IsTotalManual,
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
