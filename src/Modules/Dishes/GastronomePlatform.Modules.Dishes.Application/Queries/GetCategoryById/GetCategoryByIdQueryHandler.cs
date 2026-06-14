using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryById
{
    /// <summary>
    /// Обработчик запроса <see cref="GetCategoryByIdQuery"/> (UC-DSH-058).
    /// </summary>
    /// <remarks>
    /// Два SQL-запроса: один по PK (родительская категория), один для непосредственных
    /// детей. Если родитель неактивен — возвращается <see cref="DishesErrors.CategoryNotFound"/>
    /// (публично: «нет такой категории»).
    /// </remarks>
    public sealed class GetCategoryByIdQueryHandler
        : IQueryHandler<GetCategoryByIdQuery, CategoryDetailDto>
    {
        private readonly ICategoryRepository _categoryRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetCategoryByIdQueryHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        public GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<CategoryDetailDto>> Handle(
            GetCategoryByIdQuery request,
            CancellationToken cancellationToken)
        {
            Category? category = await _categoryRepository
                .GetByIdAsync(request.CategoryId, cancellationToken);

            if (category is null || !category.IsActive)
            {
                return DishesErrors.CategoryNotFound;
            }

            // Все активные категории грузим одним запросом — детей фильтруем
            // в памяти. На Этапе 2 категорий немного, отдельный children-запрос
            // здесь нецелесообразен.
            IReadOnlyList<Category> all =
                await _categoryRepository.ListActiveAsync(cancellationToken);

            IReadOnlyList<CategoryDto> children = all
                .Where(c => c.ParentId == category.Id)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name)
                .Select(MapToDto)
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

        private static CategoryDto MapToDto(Category category) => new(
            Id: category.Id,
            Name: category.Name,
            Slug: category.Slug,
            ParentId: category.ParentId,
            Order: category.Order,
            IconMediaId: category.IconMediaId);
    }
}
