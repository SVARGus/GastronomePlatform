using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.SearchDishes
{
    /// <summary>
    /// Обработчик запроса <see cref="SearchDishesQuery"/> (UC-DSH-054).
    /// </summary>
    /// <remarks>
    /// Делегирует фильтрацию и пагинацию репозиторному методу
    /// <see cref="IDishRepository.SearchPublishedAsync"/> — Handler отвечает только
    /// за маппинг <see cref="Dish"/> → <see cref="DishCardListItemDto"/>. Поля карточек
    /// берутся из основных таблиц <see cref="Dish"/> (паттерн UC-DSH-055).
    /// </remarks>
    public sealed class SearchDishesQueryHandler
        : IQueryHandler<SearchDishesQuery, SearchDishesResult>
    {
        private readonly IDishRepository _dishRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SearchDishesQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        public SearchDishesQueryHandler(IDishRepository dishRepository)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<SearchDishesResult>> Handle(
            SearchDishesQuery request,
            CancellationToken cancellationToken)
        {
            (IReadOnlyList<Dish> items, int totalCount) = await _dishRepository.SearchPublishedAsync(
                text: request.Text,
                categoryIds: request.CategoryIds,
                tagIds: request.TagIds,
                dietLabelsMask: request.DietLabelsMask,
                difficulties: request.Difficulties,
                costs: request.Costs,
                minRating: request.MinRating,
                sortBy: request.SortBy,
                page: request.Page,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            IReadOnlyList<DishCardListItemDto> dtos = items.Select(ToDto).ToList();

            return new SearchDishesResult(
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
