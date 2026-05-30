using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById
{
    /// <summary>
    /// Обработчик запроса <see cref="GetDishByIdQuery"/> (UC-DSH-050).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Анонимный запрос — <see cref="ICurrentUserService.UserId"/> может быть
    /// <see langword="null"/> для гостей. Решение о видимости принимается в Handler-е
    /// по комбинации <c>Dish.Status</c>, наличия <c>PublishedVersionData</c>
    /// и принадлежности текущего пользователя к автору/admin.
    /// </para>
    /// <para>
    /// Поток:
    /// </para>
    /// <list type="number">
    ///   <item>Загрузка блюда по Id (без <c>Recipe</c> — он отдаётся через UC-DSH-052).</item>
    ///   <item>404, если блюдо не найдено или <c>Status = Archived</c>.</item>
    ///   <item>Если есть <c>PublishedVersionData</c> — отдаём публичную версию;
    ///         для автора/admin добавляем флаг <c>HasUnsavedChanges</c>.</item>
    ///   <item>Если снепшота нет — доступ только для автора/admin (иначе 404);
    ///         отдаём рабочие поля с <c>IsPublishedVersion = false</c>.</item>
    /// </list>
    /// </remarks>
    public sealed class GetDishByIdQueryHandler : IQueryHandler<GetDishByIdQuery, DishDetailDto>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishByIdQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        public GetDishByIdQueryHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<DishDetailDto>> Handle(
            GetDishByIdQuery request,
            CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetByIdAsync(request.DishId, cancellationToken);
            if (dish is null || dish.Status == DishStatus.Archived)
            {
                return DishesErrors.DishNotFound;
            }

            Guid? currentUserId = _currentUser.UserId;
            bool isOwner = currentUserId.HasValue && currentUserId.Value == dish.AuthorUserId;
            bool isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN);
            bool isOwnerOrAdmin = isOwner || isAdmin;

            if (dish.PublishedVersionData is not null)
            {
                // TODO (UC-DSH-004 PublishDish): после реализации публикации источник
                // данных для этой ветки должен быть переключён на парсинг
                // dish.PublishedVersionData (jsonb-снепшот публичной версии).
                // Сейчас на Этапе 2 ветка недостижима (никакое блюдо не публикуется),
                // поэтому маппинг ведётся из текущих полей агрегата.
                bool? hasUnsavedChanges = isOwnerOrAdmin
                    ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value)
                    : null;

                return Map(dish, isPublishedVersion: true, hasUnsavedChanges: hasUnsavedChanges);
            }

            // Снепшота нет — это Draft или Unpublished. Видеть может только автор/admin.
            if (!isOwnerOrAdmin)
            {
                return DishesErrors.DishNotFound;
            }

            return Map(dish, isPublishedVersion: false, hasUnsavedChanges: false);
        }

        private static DishDetailDto Map(Dish dish, bool isPublishedVersion, bool? hasUnsavedChanges) => new(
            Id: dish.Id,
            AuthorUserId: dish.AuthorUserId,
            Name: dish.Name,
            Slug: dish.Slug,
            ShortDescription: dish.ShortDescription,
            Description: dish.Description,
            HistoryText: dish.HistoryText,
            MainImageId: dish.MainImageId,
            Status: dish.Status,
            DifficultyLevel: dish.DifficultyLevel,
            CostEstimate: dish.CostEstimate,
            OwnerType: dish.OwnerType,
            DietLabelsMask: dish.DietLabelsMask,
            AllergensMask: dish.AllergensMask,
            HasUnverifiedAllergens: dish.HasUnverifiedAllergens,
            RatingAvg: dish.RatingAvg,
            RatingCount: dish.RatingCount,
            ViewsCount: dish.ViewsCount,
            FavoritesCount: dish.FavoritesCount,
            PublishedAt: dish.PublishedAt,
            CreatedAt: dish.CreatedAt,
            UpdatedAt: dish.UpdatedAt,
            IsPublishedVersion: isPublishedVersion,
            HasUnsavedChanges: hasUnsavedChanges);
    }
}
