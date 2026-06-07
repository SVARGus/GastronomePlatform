using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Snapshots;
using GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos;
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
    ///   <item>Если есть <c>PublishedVersionData</c> — парсится jsonb-снепшот через
    ///         <see cref="IPublishedDishSnapshotReader"/>; публичные поля карточки
    ///         берутся из снепшота, lifecycle-метаданные и runtime-счётчики —
    ///         из записи <c>Dish</c>. Для автора/admin добавляется флаг
    ///         <c>HasUnsavedChanges</c>.</item>
    ///   <item>Если снепшота нет — доступ только для автора/admin (иначе 404);
    ///         отдаём рабочие поля с <c>IsPublishedVersion = false</c>.</item>
    /// </list>
    /// </remarks>
    public sealed class GetDishByIdQueryHandler : IQueryHandler<GetDishByIdQuery, DishDetailDto>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IPublishedDishSnapshotReader _snapshotReader;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishByIdQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="snapshotReader">Парсер jsonb-снепшота публичной версии (UC-DSH-052).</param>
        public GetDishByIdQueryHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser,
            IPublishedDishSnapshotReader snapshotReader)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
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
                PublishedDishSnapshot snapshot = _snapshotReader.Read(dish.PublishedVersionData);

                bool? hasUnsavedChanges = isOwnerOrAdmin
                    ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value)
                    : null;

                return MapFromSnapshot(dish, snapshot, hasUnsavedChanges);
            }

            // Снепшота нет — это Draft или Unpublished. Видеть может только автор/admin.
            if (!isOwnerOrAdmin)
            {
                return DishesErrors.DishNotFound;
            }

            return MapFromWorking(dish);
        }

        // Snapshot-ветка: публичные поля карточки берутся из jsonb-снепшота, а
        // lifecycle-метаданные и runtime-счётчики (Status, *At, *Count) — из самой
        // записи Dish (в снепшот они не входят по дизайну, см. PublishedDishSnapshot).
        private static DishDetailDto MapFromSnapshot(
            Dish dish,
            PublishedDishSnapshot snapshot,
            bool? hasUnsavedChanges) => new(
                Id: dish.Id,
                AuthorUserId: dish.AuthorUserId,
                Name: snapshot.Name,
                Slug: snapshot.Slug,
                ShortDescription: snapshot.ShortDescription,
                Description: snapshot.Description,
                HistoryText: snapshot.HistoryText,
                MainImageId: snapshot.MainImageId,
                Status: dish.Status,
                DifficultyLevel: snapshot.DifficultyLevel,
                CostEstimate: snapshot.CostEstimate,
                OwnerType: snapshot.OwnerType,
                DietLabelsMask: snapshot.DietLabelsMask,
                AllergensMask: snapshot.AllergensMask,
                HasUnverifiedAllergens: snapshot.HasUnverifiedAllergens,
                RatingAvg: dish.RatingAvg,
                RatingCount: dish.RatingCount,
                ViewsCount: dish.ViewsCount,
                FavoritesCount: dish.FavoritesCount,
                PublishedAt: dish.PublishedAt,
                CreatedAt: dish.CreatedAt,
                UpdatedAt: dish.UpdatedAt,
                IsPublishedVersion: true,
                HasUnsavedChanges: hasUnsavedChanges);

        // Working-ветка: все поля карточки берутся напрямую из агрегата. Доступна
        // только автору/admin при отсутствии PublishedVersionData.
        private static DishDetailDto MapFromWorking(Dish dish) => new(
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
            IsPublishedVersion: false,
            HasUnsavedChanges: false);
    }
}
