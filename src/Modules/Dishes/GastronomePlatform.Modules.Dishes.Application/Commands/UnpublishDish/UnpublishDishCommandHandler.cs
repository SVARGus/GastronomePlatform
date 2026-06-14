using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UnpublishDish
{
    /// <summary>
    /// Обработчик команды <see cref="UnpublishDishCommand"/> (UC-DSH-005).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка корневого <see cref="Dish"/> через
    ///         <c>IDishRepository.GetByIdAsync</c> — <c>Recipe</c> и подколлекции
    ///         не нужны: операция меняет только статус и очищает <c>*Published</c>-связки,
    ///         которые EF Core загружает по факту обращения к коллекциям.</item>
    ///   <item>Проверка владения (POL-001 DishOwnership): автор блюда или Admin —
    ///         иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Делегирование Domain: <c>dish.Unpublish(utcNow)</c> — содержит инвариант
    ///         <c>Status == Published</c> и при нарушении возвращает
    ///         <see cref="DishesErrors.DishNotPublished"/>.</item>
    ///   <item>Сохранение (один транзакционный коммит) и публикация доменных событий
    ///         через <see cref="IDomainEventDispatcher"/> — на Этапе 2 подписчиков нет;
    ///         на Этапе 5+ ожидаются обработчики <c>DishUnpublishedEvent</c>.</item>
    /// </list>
    /// Гарантия валидного <c>UserId</c> — на уровне политики
    /// <c>AuthorizationPolicies.VALID_ACTOR</c>, применённой на эндпоинте.
    /// </remarks>
    public sealed class UnpublishDishCommandHandler : ICommandHandler<UnpublishDishCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UnpublishDishCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public UnpublishDishCommandHandler(
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
        public async Task<Result> Handle(UnpublishDishCommand request, CancellationToken cancellationToken)
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

            Result unpublishResult = dish.Unpublish(_clock.UtcNow);
            if (unpublishResult.IsFailure)
            {
                return unpublishResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
