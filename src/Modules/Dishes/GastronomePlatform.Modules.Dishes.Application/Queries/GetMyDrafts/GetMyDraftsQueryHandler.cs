using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetMyDrafts
{
    /// <summary>
    /// Обработчик запроса <see cref="GetMyDraftsQuery"/> (UC-DSH-053).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Получение идентификатора автора из <see cref="ICurrentUserService"/>
    ///         (гарантирован политикой <c>AuthorizationPolicies.VALID_ACTOR</c> на эндпоинте).</item>
    ///   <item>Запрос списка черновиков автора через репозиторий с пагинацией.</item>
    ///   <item>Маппинг доменных сущностей <see cref="Dish"/> в <see cref="DishDraftListItemDto"/>.</item>
    /// </list>
    /// </remarks>
    public sealed class GetMyDraftsQueryHandler : IQueryHandler<GetMyDraftsQuery, GetMyDraftsResult>
    {
        private readonly IDishRepository _dishRepository;
        private readonly ICurrentUserService _currentUser;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetMyDraftsQueryHandler"/>.
        /// </summary>
        /// <param name="dishRepository">Репозиторий блюд.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        public GetMyDraftsQueryHandler(
            IDishRepository dishRepository,
            ICurrentUserService currentUser)
        {
            _dishRepository = dishRepository ?? throw new ArgumentNullException(nameof(dishRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        /// <inheritdoc/>
        public async Task<Result<GetMyDraftsResult>> Handle(
            GetMyDraftsQuery request,
            CancellationToken cancellationToken)
        {
            // Гарантия валидного UserId — на уровне политики ValidActor.
            Guid authorUserId = _currentUser.UserId!.Value;

            // TODO: при появлении фильтров (поиск по Name, по DifficultyLevel и т.п.)
            // дополнительные параметры добавляются здесь — в Query, в Validator и
            // в сигнатуру репозиторного метода ListDraftsByAuthorAsync.
            (IReadOnlyList<Dish> items, int totalCount) = await _dishRepository.ListDraftsByAuthorAsync(
                authorUserId,
                request.Page,
                request.PageSize,
                cancellationToken);

            IReadOnlyList<DishDraftListItemDto> dtos = items
                .Select(ToDto)
                .ToList();

            return new GetMyDraftsResult(
                Items: dtos,
                TotalCount: totalCount,
                Page: request.Page,
                PageSize: request.PageSize);
        }

        private static DishDraftListItemDto ToDto(Dish dish) => new(
            Id: dish.Id,
            Slug: dish.Slug,
            Name: dish.Name,
            ShortDescription: dish.ShortDescription,
            MainImageId: dish.MainImageId,
            DifficultyLevel: dish.DifficultyLevel,
            CostEstimate: dish.CostEstimate,
            DietLabelsMask: dish.DietLabelsMask,
            AllergensMask: dish.AllergensMask,
            HasUnverifiedAllergens: dish.HasUnverifiedAllergens,
            CreatedAt: dish.CreatedAt,
            UpdatedAt: dish.UpdatedAt);
    }
}
