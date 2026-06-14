using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetNutrition
{
    /// <summary>
    /// Обработчик команды <see cref="SetNutritionCommand"/> (UC-DSH-042).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с подгруженным <see cref="Dish.Recipe"/> через
    ///         <see cref="IDishRepository.GetByIdWithRecipeAsync"/> — нужен 1:1
    ///         <see cref="Nutrition"/> (если уже создан).</item>
    ///   <item>POL-001: автор блюда или Admin.</item>
    ///   <item>Делегирование Domain: <see cref="Dish.UpdateNutrition"/>. Если у рецепта
    ///         ещё нет записи КБЖУ — Domain создаёт новую через <c>Nutrition.Create</c>;
    ///         иначе перезаписывает существующую. Возврат void: валидация значений
    ///         выполнена на уровне команды.</item>
    ///   <item>Сохранение и публикация доменных событий
    ///         (<c>DishUpdatedEvent</c> поднимается из
    ///         <see cref="Dish.MarkAsUpdated"/>).</item>
    /// </list>
    /// </remarks>
    public sealed class SetNutritionCommandHandler : ICommandHandler<SetNutritionCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetNutritionCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public SetNutritionCommandHandler(
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
        public async Task<Result> Handle(SetNutritionCommand request, CancellationToken cancellationToken)
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

            dish.UpdateNutrition(
                calcMethod: request.CalcMethod,
                calories: request.Calories,
                proteins: request.Proteins,
                fats: request.Fats,
                saturatedFats: request.SaturatedFats,
                carbs: request.Carbs,
                sugar: request.Sugar,
                fiber: request.Fiber,
                salt: request.Salt,
                utcNow: _clock.UtcNow);

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
