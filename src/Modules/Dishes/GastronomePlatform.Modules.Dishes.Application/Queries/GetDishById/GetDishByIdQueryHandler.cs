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
        private readonly ITagRepository _tagRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IPublishedDishSnapshotReader _snapshotReader;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishByIdQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="tagRepository">Репозиторий тегов (резолв имён для DTO).</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="snapshotReader">Парсер jsonb-снепшота публичной версии (UC-DSH-052).</param>
        public GetDishByIdQueryHandler(
            IDishRepository dishRepository,
            ITagRepository tagRepository,
            ICurrentUserService currentUser,
            IPublishedDishSnapshotReader snapshotReader)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _snapshotReader = snapshotReader ?? throw new ArgumentNullException(nameof(snapshotReader));
        }

        /// <inheritdoc/>
        public async Task<Result<DishDetailDto>> Handle(
            GetDishByIdQuery request,
            CancellationToken cancellationToken)
        {
            // Связки Categories/Tags загружаются сразу: working-ветка отдаёт их
            // редактору карточки (replace-семантика UC-DSH-007/008 требует видеть
            // текущий набор перед сохранением).
            Dish? dish = await _dishRepository.GetByIdWithLinksAsync(request.DishId, cancellationToken);
            if (dish is null || dish.Status == DishStatus.Archived)
            {
                return DishesErrors.DishNotFound;
            }

            Guid? currentUserId = _currentUser.UserId;
            bool isOwner = currentUserId.HasValue && currentUserId.Value == dish.AuthorUserId;
            bool isAdmin = _currentUser.IsInRole(PlatformRoles.ADMIN);
            bool isOwnerOrAdmin = isOwner || isAdmin;

            // Явный запрос рабочей версии (?version=working) учитывается только
            // для автора/admin — источник данных редакторов при опубликованном
            // блюде (частичная реализация отложенного UC-DSH-083).
            bool useWorking = dish.PublishedVersionData is null
                || (request.PreferWorkingVersion && isOwnerOrAdmin);

            if (!useWorking)
            {
                PublishedDishSnapshot snapshot = _snapshotReader.Read(dish.PublishedVersionData!);

                bool? hasUnsavedChanges = isOwnerOrAdmin
                    ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value)
                    : null;

                IReadOnlyList<Guid> snapshotTagIds = snapshot.Tags.Select(t => t.Id).ToList();
                IReadOnlyList<string> tagNames = await ResolveTagNamesAsync(snapshotTagIds, cancellationToken);

                return MapFromSnapshot(dish, snapshot, hasUnsavedChanges, tagNames);
            }

            // Рабочий слой видит только автор/admin (для Draft/Unpublished — единственный слой).
            if (!isOwnerOrAdmin)
            {
                return DishesErrors.DishNotFound;
            }

            IReadOnlyList<Guid> workingTagIds = dish.Tags.Select(t => t.TagId).ToList();
            IReadOnlyList<string> workingTagNames = await ResolveTagNamesAsync(workingTagIds, cancellationToken);

            bool workingHasUnsavedChanges =
                dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value;

            return MapFromWorking(dish, workingTagNames, workingHasUnsavedChanges);
        }

        // Имена тегов по идентификаторам связок: UC-DSH-008 принимает имена,
        // поэтому DTO отдаёт их же — редактор шлёт набор обратно без пересборки.
        private async Task<IReadOnlyList<string>> ResolveTagNamesAsync(
            IReadOnlyList<Guid> tagIds,
            CancellationToken cancellationToken)
        {
            if (tagIds.Count == 0)
            {
                return Array.Empty<string>();
            }

            var tags = await _tagRepository.ListByIdsAsync(tagIds, cancellationToken);
            return tags.Select(t => t.Name).ToList();
        }

        // Snapshot-ветка: публичные поля карточки берутся из jsonb-снепшота, а
        // lifecycle-метаданные и runtime-счётчики (Status, *At, *Count) — из самой
        // записи Dish (в снепшот они не входят по дизайну, см. PublishedDishSnapshot).
        private static DishDetailDto MapFromSnapshot(
            Dish dish,
            PublishedDishSnapshot snapshot,
            bool? hasUnsavedChanges,
            IReadOnlyList<string> tagNames) => new(
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
                CategoryIds: snapshot.Categories.Select(c => c.Id).ToList(),
                TagNames: tagNames,
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
        // только автору/admin — при отсутствии снепшота либо по явному запросу
        // рабочей версии (?version=working).
        private static DishDetailDto MapFromWorking(
            Dish dish,
            IReadOnlyList<string> tagNames,
            bool hasUnsavedChanges) => new(
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
            CategoryIds: dish.Categories.Select(c => c.CategoryId).ToList(),
            TagNames: tagNames,
            RatingAvg: dish.RatingAvg,
            RatingCount: dish.RatingCount,
            ViewsCount: dish.ViewsCount,
            FavoritesCount: dish.FavoritesCount,
            PublishedAt: dish.PublishedAt,
            CreatedAt: dish.CreatedAt,
            UpdatedAt: dish.UpdatedAt,
            IsPublishedVersion: false,
            HasUnsavedChanges: hasUnsavedChanges);
    }
}
