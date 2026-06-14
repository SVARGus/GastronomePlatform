using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.GetDishById;
using GastronomePlatform.Modules.Dishes.Application.Snapshots;
using GastronomePlatform.Modules.Dishes.Application.Snapshots.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishBySlug
{
    /// <summary>
    /// Обработчик запроса <see cref="GetDishBySlugQuery"/> (UC-DSH-051).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Параллель <c>GetDishByIdQueryHandler</c> с одним ключевым отличием: рабочая
    /// копия по slug не отдаётся. Если у блюда нет <c>PublishedVersionData</c> —
    /// возвращается <c>404</c> даже автору. Slug — это публичный URL, и он имеет смысл
    /// только когда есть публичный снепшот.
    /// </para>
    /// <para>
    /// Поток:
    /// </para>
    /// <list type="number">
    ///   <item>Загрузка блюда по slug.</item>
    ///   <item>404, если блюдо отсутствует, <c>Archived</c> или
    ///         <c>PublishedVersionData IS NULL</c>.</item>
    ///   <item>Парсинг jsonb-снепшота через <see cref="IPublishedDishSnapshotReader"/>.</item>
    ///   <item>Сборка <see cref="DishDetailDto"/>: публичные поля карточки — из снепшота,
    ///         lifecycle-метаданные и runtime-счётчики — из <see cref="Dish"/>.
    ///         Для автора/admin — флаг <c>HasUnsavedChanges</c>.</item>
    /// </list>
    /// </remarks>
    public sealed class GetDishBySlugQueryHandler : IQueryHandler<GetDishBySlugQuery, DishDetailDto>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IPublishedDishSnapshotReader _snapshotReader;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishBySlugQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="snapshotReader">Парсер jsonb-снепшота публичной версии.</param>
        public GetDishBySlugQueryHandler(
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
            GetDishBySlugQuery request,
            CancellationToken cancellationToken)
        {
            Dish? dish = await _dishRepository.GetBySlugAsync(request.Slug, cancellationToken);
            if (dish is null
                || dish.Status == DishStatus.Archived
                || dish.PublishedVersionData is null)
            {
                return DishesErrors.DishNotFound;
            }

            Guid? currentUserId = _currentUser.UserId;
            bool isOwner = currentUserId.HasValue && currentUserId.Value == dish.AuthorUserId;
            bool isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN);
            bool isOwnerOrAdmin = isOwner || isAdmin;

            PublishedDishSnapshot snapshot = _snapshotReader.Read(dish.PublishedVersionData);

            bool? hasUnsavedChanges = isOwnerOrAdmin
                ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value)
                : null;

            return MapFromSnapshot(dish, snapshot, hasUnsavedChanges);
        }

        // Snapshot-ветка — идентично UC-DSH-050.MapFromSnapshot. Дублирование
        // намеренно: маппинг проще читать рядом с Handler-ом, чем выносить
        // в общий хелпер ради одного места переиспользования.
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
    }
}
