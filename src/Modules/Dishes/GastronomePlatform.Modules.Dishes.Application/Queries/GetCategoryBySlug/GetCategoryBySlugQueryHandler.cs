using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryBySlug
{
    /// <summary>
    /// Обработчик запроса <see cref="GetCategoryBySlugQuery"/> (UC-DSH-059).
    /// </summary>
    /// <remarks>
    /// Симметричен <c>GetCategoryByIdQueryHandler</c>; разрешение происходит по
    /// уникальному индексу <c>Slug</c>. Неактивная категория — <c>404</c>.
    /// </remarks>
    public sealed class GetCategoryBySlugQueryHandler
        : IQueryHandler<GetCategoryBySlugQuery, CategoryDetailDto>
    {
        private readonly ICategoryRepository _categoryRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetCategoryBySlugQueryHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        public GetCategoryBySlugQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<CategoryDetailDto>> Handle(
            GetCategoryBySlugQuery request,
            CancellationToken cancellationToken)
        {
            Category? category = await _categoryRepository
                .GetBySlugAsync(request.Slug, cancellationToken);

            if (category is null || !category.IsActive)
            {
                return DishesErrors.CategoryNotFound;
            }

            IReadOnlyList<Category> all =
                await _categoryRepository.ListActiveAsync(cancellationToken);

            IReadOnlyList<CategoryDto> children = all
                .Where(c => c.ParentId == category.Id)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryDto(
                    Id: c.Id,
                    Name: c.Name,
                    Slug: c.Slug,
                    ParentId: c.ParentId,
                    Order: c.Order,
                    IconMediaId: c.IconMediaId))
                .ToList();

            return new CategoryDetailDto(
                Id: category.Id,
                Name: category.Name,
                Slug: category.Slug,
                ParentId: category.ParentId,
                Order: category.Order,
                IconMediaId: category.IconMediaId,
                Children: children);
        }
    }
}
