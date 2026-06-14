using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetCategories
{
    /// <summary>
    /// Обработчик команды <see cref="SetCategoriesCommand"/> (UC-DSH-007).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Загрузка блюда с подгруженной коллекцией <see cref="Dish.Categories"/>
    ///         через <see cref="IDishRepository.GetByIdWithCategoriesAsync"/>. Без явной
    ///         подгрузки EF Core не отследит удаление существующих связок
    ///         <c>DishCategory</c> при <c>_categories.Clear()</c> в Domain.</item>
    ///   <item>POL-001: автор блюда или Admin.</item>
    ///   <item>Если список категорий непустой — batch-проверка существования и активности
    ///         через <see cref="ICategoryRepository.ListByIdsAsync"/>. Если количество
    ///         найденных категорий не совпадает с входным набором —
    ///         <see cref="DishesErrors.CategoryNotFound"/>.</item>
    ///   <item>Делегирование Domain: <see cref="Dish.SetCategories"/>. Domain проверяет
    ///         лимит и отсутствие дубликатов как defense-in-depth; при нарушении —
    ///         <see cref="DishesErrors.CategoryLimitExceeded"/> или
    ///         <see cref="DishesErrors.DuplicateCategoryId"/>.</item>
    ///   <item>Сохранение и публикация доменных событий
    ///         (<c>DishUpdatedEvent</c> поднимается из <see cref="Dish.MarkAsUpdated"/>).</item>
    /// </list>
    /// </remarks>
    public sealed class SetCategoriesCommandHandler : ICommandHandler<SetCategoriesCommand>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetCategoriesCommandHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public SetCategoriesCommandHandler(
            IDishRepository dishRepository,
            ICategoryRepository categoryRepository,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IDomainEventDispatcher eventDispatcher)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(SetCategoriesCommand request, CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdWithCategoriesAsync(request.DishId, cancellationToken);
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

            // Дедупликация уже гарантирована валидатором, но повторяем для надёжности
            // перед обращением к БД. Пустой набор → пропускаем проверку существования.
            IReadOnlyCollection<Guid> uniqueIds = request.CategoryIds.Distinct().ToArray();
            if (uniqueIds.Count > 0)
            {
                IReadOnlyList<Category> foundCategories =
                    await _categoryRepository.ListByIdsAsync(uniqueIds, cancellationToken);

                if (foundCategories.Count != uniqueIds.Count)
                {
                    return DishesErrors.CategoryNotFound;
                }
            }

            Result setResult = dish.SetCategories(request.CategoryIds, _clock.UtcNow);
            if (setResult.IsFailure)
            {
                return setResult;
            }

            await _dishRepository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(dish, cancellationToken);

            return Result.Success();
        }
    }
}
