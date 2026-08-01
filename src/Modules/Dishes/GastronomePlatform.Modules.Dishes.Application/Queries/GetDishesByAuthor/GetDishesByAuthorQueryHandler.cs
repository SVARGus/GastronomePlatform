using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetDishesByAuthor
{
    /// <summary>
    /// Обработчик запроса <see cref="GetDishesByAuthorQuery"/> (UC-DSH-055).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Поток выполнения:
    /// </para>
    /// <list type="number">
    ///   <item>Запрос репозитория <see cref="IDishRepository.ListPublishedByAuthorAsync"/> —
    ///         фильтр <c>PublishedVersionData IS NOT NULL</c>, сортировка по
    ///         <c>PublishedAt DESC</c>, пагинация.</item>
    ///   <item>Маппинг <see cref="Dish"/> → <see cref="DishCardListItemDto"/>. Поля
    ///         берутся из основных таблиц <c>Dish</c> — это согласуется с шаблоном
    ///         UC-DSH-053 GetMyDrafts. На Этапе 2 расхождение между основными полями
    ///         и snapshot для опубликованного блюда (если автор начал править)
    ///         считается приемлемым: в каталоге автор обычно показывает свежее.
    ///         Если потребуется строгая snapshot-семантика — переходим на
    ///         <c>IPublishedDishSnapshotReader</c>.</item>
    /// </list>
    /// </remarks>
    public sealed class GetDishesByAuthorQueryHandler
        : IQueryHandler<GetDishesByAuthorQuery, GetDishesByAuthorResult>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishesByAuthorQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя (для флага правок владельцу).</param>
        public GetDishesByAuthorQueryHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<GetDishesByAuthorResult>> Handle(
            GetDishesByAuthorQuery request,
            CancellationToken cancellationToken)
        {
            (IReadOnlyList<Dish> items, int totalCount) =
                await _dishRepository.ListPublishedByAuthorAsync(
                    request.AuthorUserId,
                    request.Page,
                    request.PageSize,
                    cancellationToken);

            // Флаг «есть неопубликованные правки» — только владельцу списка и admin:
            // «Мои блюда» показывают бейдж, посетителям страницы автора флаг не отдаётся.
            Guid? currentUserId = _currentUser.UserId;
            bool exposeUnsavedChanges =
                (currentUserId.HasValue && currentUserId.Value == request.AuthorUserId)
                || _currentUser.IsInRole(PlatformRoles.ADMIN);

            IReadOnlyList<DishCardListItemDto> dtos = items
                .Select(dish => ToDto(dish, exposeUnsavedChanges))
                .ToList();

            return new GetDishesByAuthorResult(
                Items: dtos,
                TotalCount: totalCount,
                Page: request.Page,
                PageSize: request.PageSize);
        }

        private static DishCardListItemDto ToDto(Dish dish, bool exposeUnsavedChanges) => new(
            Id: dish.Id,
            AuthorUserId: dish.AuthorUserId,
            Slug: dish.Slug,
            Name: dish.Name,
            ShortDescription: dish.ShortDescription,
            MainImageId: dish.MainImageId,
            DifficultyLevel: dish.DifficultyLevel,
            CostEstimate: dish.CostEstimate,
            DietLabelsMask: dish.DietLabelsMask,
            AllergensMask: dish.AllergensMask,
            HasUnverifiedAllergens: dish.HasUnverifiedAllergens,
            RatingAvg: dish.RatingAvg,
            RatingCount: dish.RatingCount,
            ViewsCount: dish.ViewsCount,
            FavoritesCount: dish.FavoritesCount,
            PublishedAt: dish.PublishedAt,
            CreatedAt: dish.CreatedAt,
            HasUnsavedChanges: exposeUnsavedChanges
                ? (dish.PublishedAt.HasValue && dish.UpdatedAt > dish.PublishedAt.Value)
                : null);
    }
}
