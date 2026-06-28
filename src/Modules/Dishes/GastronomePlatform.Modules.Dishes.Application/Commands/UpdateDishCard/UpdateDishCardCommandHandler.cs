using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Helpers;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateDishCard
{
    /// <summary>
    /// Обработчик команды <see cref="UpdateDishCardCommand"/> (UC-DSH-002).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда по <c>DishId</c> без рецепта (карточные поля не трогают рецепт).</item>
    ///   <item>Проверка владения (POL-001 DishOwnership): автор блюда совпадает
    ///         с идентификатором текущего пользователя либо актор — Admin.</item>
    ///   <item>Резолв актуального <see cref="Domain.Enums.OwnerType"/> из ролей пользователя.</item>
    ///   <item>Применение изменений через <see cref="Dish.UpdateCard"/>.</item>
    ///   <item>Сохранение (один транзакционный коммит).</item>
    ///   <item>Публикация доменных событий из агрегата через <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// Гарантия валидного <c>UserId</c> — на уровне политики
    /// <c>AuthorizationPolicies.VALID_ACTOR</c>, применённой на эндпоинте,
    /// поэтому <c>_currentUser.UserId!.Value</c> — корректно.
    /// </remarks>
    public sealed class UpdateDishCardCommandHandler : ICommandHandler<UpdateDishCardCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateDishCardCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public UpdateDishCardCommandHandler(
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
        public async Task<Result> Handle(UpdateDishCardCommand request, CancellationToken cancellationToken)
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

            var ownerType = OwnerTypeResolver.ResolveFromRoles(_currentUser.Roles);
            var utcNow = _clock.UtcNow;

            dish.UpdateCard(
                name: request.Name,
                shortDescription: request.ShortDescription,
                description: request.Description,
                difficultyLevel: request.DifficultyLevel,
                costEstimate: request.CostEstimate,
                ownerType: ownerType,
                utcNow: utcNow);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
