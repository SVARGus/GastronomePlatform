using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.AddRecipeStep
{
    /// <summary>
    /// Обработчик команды <see cref="AddRecipeStepCommand"/> (UC-DSH-020).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с полным рецептом — нужен текущий список шагов для
    ///         назначения <c>Order = max+1</c> через <c>Recipe.AddStep</c>.</item>
    ///   <item>POL-001: автор блюда или Admin. Иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Вызов <see cref="Dish.AddRecipeStep"/> — Domain создаёт <see cref="RecipeStep"/>
    ///         с очередным <c>Order</c> и поднимает <c>DishUpdatedEvent</c>.</item>
    ///   <item>Сохранение и публикация доменных событий через <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// <para>
    /// На текущем этапе attach иллюстрации шага через межмодульный <c>IMediaService</c> не выполняется
    /// (сервис ещё не реализован). <c>ImageMediaId</c> сохраняется как «логическая ссылка»;
    /// orphan-проблема для шага решается в отдельной сессии вместе с реализацией <c>IMediaService</c>.
    /// </para>
    /// </remarks>
    public sealed class AddRecipeStepCommandHandler
        : ICommandHandler<AddRecipeStepCommand, AddRecipeStepResult>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AddRecipeStepCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public AddRecipeStepCommandHandler(
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
        public async Task<Result<AddRecipeStepResult>> Handle(
            AddRecipeStepCommand request,
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

            Guid newStepId = dish.AddRecipeStep(
                description: request.Description,
                title: request.Title,
                imageMediaId: request.ImageMediaId,
                videoUrl: request.VideoUrl,
                temperatureCelsius: request.TemperatureCelsius,
                timerMinutes: request.TimerMinutes,
                utcNow: utcNow);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return new AddRecipeStepResult(newStepId);
        }
    }
}
