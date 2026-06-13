using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.UpdateRecipe
{
    /// <summary>
    /// Обработчик команды <see cref="UpdateRecipeCommand"/> (UC-DSH-003).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с подгруженным <see cref="Dish.Recipe"/> через
    ///         <see cref="IDishRepository.GetByIdWithRecipeAsync"/>.</item>
    ///   <item>Проверка владения (POL-001 DishOwnership): автор блюда совпадает
    ///         с идентификатором текущего пользователя.</item>
    ///   <item>Атомарное обновление полей рецепта через <see cref="Dish.UpdateRecipe"/>.</item>
    ///   <item>Сохранение и публикация доменных событий.</item>
    /// </list>
    /// Гарантия валидного <c>UserId</c> — на уровне политики
    /// <c>AuthorizationPolicies.VALID_ACTOR</c>, применённой на эндпоинте.
    /// </remarks>
    public sealed class UpdateRecipeCommandHandler : ICommandHandler<UpdateRecipeCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UpdateRecipeCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public UpdateRecipeCommandHandler(
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
        public async Task<Result> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdWithRecipeAsync(request.DishId, cancellationToken);
            if (dish is null)
            {
                return DishesErrors.DishNotFound;
            }

            var actorUserId = _currentUser.UserId!.Value;
            bool isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN);
            if (dish.AuthorUserId != actorUserId && !isAdmin)
            {
                return DishesErrors.NotDishOwner;
            }

            var utcNow = _clock.UtcNow;

            Result updateResult = dish.UpdateRecipe(
                introductionText: request.IntroductionText,
                servingsDefault: request.ServingsDefault,
                isAlcoholic: request.IsAlcoholic,
                authorTips: request.AuthorTips,
                servingSuggestions: request.ServingSuggestions,
                notes: request.Notes,
                utcNow: utcNow);

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
