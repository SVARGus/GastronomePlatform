using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Media.Application.Contracts;
using GastronomePlatform.Modules.Media.Domain.Constants;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ChangeMainImage
{
    /// <summary>
    /// Обработчик команды <see cref="ChangeMainImageCommand"/> (UC-DSH-011).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда. <see cref="DishesErrors.DishNotFound"/> при отсутствии.</item>
    ///   <item>POL-001: автор блюда или Admin. Иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Запоминаем текущий <c>MainImageId</c> для последующего detach.</item>
    ///   <item>Вызов <see cref="Dish.ChangeMainImage"/> — Domain обновляет поле и
    ///         поднимает <c>DishUpdatedEvent</c>.</item>
    ///   <item>Синхронизация Media через <see cref="IMediaService"/> по разнице old/new
    ///         (4 ветки: без изменений / стало null / добавлено / заменено).</item>
    ///   <item>Сохранение Dishes и публикация доменных событий через
    ///         <see cref="IDomainEventDispatcher"/>.</item>
    /// </list>
    /// <para>
    /// <b>Consistency на Этапе 2:</b> между Dishes и Media — две разные БД-транзакции
    /// (см. <c>AddRecipeStepCommandHandler</c> и общий долг IMediaService).
    /// </para>
    /// </remarks>
    public sealed class ChangeMainImageCommandHandler : ICommandHandler<ChangeMainImageCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;
        private readonly IMediaService _mediaService;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ChangeMainImageCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        /// <param name="mediaService">Межмодульный сервис модуля Media.</param>
        public ChangeMainImageCommandHandler(
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
        public async Task<Result> Handle(ChangeMainImageCommand request, CancellationToken cancellationToken)
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

            Guid? oldMainImageId = dish.MainImageId;
            Guid? newMainImageId = request.MainImageId;

            dish.ChangeMainImage(newMainImageId, _clock.UtcNow);

            if (oldMainImageId != newMainImageId)
            {
                if (oldMainImageId.HasValue)
                {
                    Result detachResult = await _mediaService.DetachFromEntityAsync(
                        oldMainImageId.Value, cancellationToken);
                    if (detachResult.IsFailure)
                    {
                        return detachResult.Error;
                    }
                }

                if (newMainImageId.HasValue)
                {
                    Result attachResult = await _mediaService.AttachToEntityAsync(
                        mediaId: newMainImageId.Value,
                        actorUserId: actorUserId,
                        entityType: MediaEntityTypes.DISH,
                        entityId: request.DishId,
                        ct: cancellationToken);
                    if (attachResult.IsFailure)
                    {
                        return attachResult.Error;
                    }
                }
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
