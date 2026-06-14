using GastronomePlatform.Common.Application.Messaging;
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

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetDishesByAuthorQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        public GetDishesByAuthorQueryHandler(IDishRepository dishRepository)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
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

            IReadOnlyList<DishCardListItemDto> dtos = items
                .Select(ToDto)
                .ToList();

            return new GetDishesByAuthorResult(
                Items: dtos,
                TotalCount: totalCount,
                Page: request.Page,
                PageSize: request.PageSize);
        }

        private static DishCardListItemDto ToDto(Dish dish) => new(
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
            CreatedAt: dish.CreatedAt);
    }
}
