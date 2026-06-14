using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Media.Application.Contracts;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.RemoveRecipeStep
{
    /// <summary>
    /// Обработчик команды <see cref="RemoveRecipeStepCommand"/> (UC-DSH-022).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с полным рецептом.</item>
    ///   <item>POL-001: автор блюда или Admin.</item>
    ///   <item>Поиск шага по <c>StepId</c> + запоминаем <c>ImageMediaId</c> (для detach Media).</item>
    ///   <item>Вызов <see cref="Dish.RemoveRecipeStep"/> — Domain удаляет шаг и
    ///         переупорядочивает оставшиеся (<c>Order = 1..N</c>).
    ///         <see cref="DishesErrors.StepNotFound"/> при отсутствии шага.</item>
    ///   <item>Если у удалённого шага был привязан медиафайл — detach через
    ///         <see cref="IMediaService"/>. Медиа становится orphan и подлежит
    ///         фоновой очистке (UC-MED-210). Soft-delete на стороне Media не делаем —
    ///         это пользовательское решение через UC-MED-005.</item>
    ///   <item>Сохранение Dishes и публикация доменных событий через
    ///         <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// </remarks>
    public sealed class RemoveRecipeStepCommandHandler : ICommandHandler<RemoveRecipeStepCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly IMediaService _mediaService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RemoveRecipeStepCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        /// <param name="mediaService">Межмодульный сервис модуля Media.</param>
        public RemoveRecipeStepCommandHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IDomainEventDispatcher eventDispatcher,
            IMediaService mediaService)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(
            RemoveRecipeStepCommand request,
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

            // Запоминаем ImageMediaId до удаления — нужно для последующего detach.
            // Если шаг не найден — Domain вернёт StepNotFound на следующем шаге.
            RecipeStep? existingStep = dish.Recipe.Steps.FirstOrDefault(s => s.Id == request.StepId);
            Guid? imageMediaId = existingStep?.ImageMediaId;

            Result removeResult = dish.RemoveRecipeStep(request.StepId, _clock.UtcNow);
            if (removeResult.IsFailure)
            {
                return removeResult;
            }

            if (imageMediaId.HasValue)
            {
                Result detachResult = await _mediaService.DetachFromEntityAsync(
                    imageMediaId.Value, cancellationToken);
                if (detachResult.IsFailure)
                {
                    return detachResult.Error;
                }
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
