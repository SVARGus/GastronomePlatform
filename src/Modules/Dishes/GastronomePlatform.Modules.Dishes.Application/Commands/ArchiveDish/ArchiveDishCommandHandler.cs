using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ArchiveDish
{
    /// <summary>
    /// Обработчик команды <see cref="ArchiveDishCommand"/> (UC-DSH-006).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Поток выполнения:
    /// </para>
    /// <list type="number">
    ///   <item>Загрузка корневого <see cref="Dish"/> через
    ///         <c>IDishRepository.GetByIdAsync</c> — <c>Recipe</c> не требуется,
    ///         операция меняет только статус и очищает <c>*Published</c>-связки.</item>
    ///   <item>Проверка владения (POL-001 DishOwnership): автор блюда или Admin —
    ///         иначе <see cref="DishesErrors.NotDishOwner"/>.</item>
    ///   <item>Делегирование Domain: <c>dish.Archive(utcNow)</c> — содержит инвариант
    ///         <c>Status != Archived</c> и при повторном вызове возвращает
    ///         <see cref="DishesErrors.DishAlreadyArchived"/>.</item>
    ///   <item>Сохранение и публикация доменных событий. На Этапе 5+ ожидаются
    ///         подписчики <c>DishArchivedEvent</c> (модуль Social, Notifications).</item>
    /// </list>
    /// <para>
    /// Связанные медиафайлы (<c>Dish.MainImageId</c>, <c>RecipeStep.ImageMediaId</c>)
    /// намеренно не отвязываются (UC-DSH-006 §«Связанные медиа НЕ отвязываются»):
    /// они нужны для целостности снепшотов модуля Orders (Этап 6+) и для возможного
    /// восстановления блюда. <c>IMediaService.DeleteByEntityAsync</c> не вызывается —
    /// hard-delete медиа произойдёт только при hard-delete блюда (Этап 8+).
    /// </para>
    /// </remarks>
    public sealed class ArchiveDishCommandHandler : ICommandHandler<ArchiveDishCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ArchiveDishCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public ArchiveDishCommandHandler(
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
        public async Task<Result> Handle(ArchiveDishCommand request, CancellationToken cancellationToken)
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

            Result archiveResult = dish.Archive(_clock.UtcNow);
            if (archiveResult.IsFailure)
            {
                return archiveResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
