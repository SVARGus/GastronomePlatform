using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Application.Queries.Lookups.Dtos;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Queries.GetCategoryTree
{
    /// <summary>
    /// Обработчик запроса <see cref="GetCategoryTreeQuery"/> (UC-DSH-057).
    /// </summary>
    /// <remarks>
    /// Один SQL-запрос — <see cref="ICategoryRepository.ListActiveAsync"/>; иерархия
    /// строится в памяти за O(n) через словарь <c>ParentId → List&lt;child&gt;</c>.
    /// На Этапе 2 категорий немного (десятки), full-load + in-memory сборка проще
    /// и быстрее, чем рекурсивный CTE.
    /// </remarks>
    public sealed class GetCategoryTreeQueryHandler
        : IQueryHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryNodeDto>>
    {
        private readonly ICategoryRepository _categoryRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetCategoryTreeQueryHandler"/>.
        /// </summary>
        /// <param name="categoryRepository">Репозиторий категорий.</param>
        public GetCategoryTreeQueryHandler(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository
                ?? throw new ArgumentNullException(nameof(categoryRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyList<CategoryNodeDto>>> Handle(
            GetCategoryTreeQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Category> all =
                await _categoryRepository.ListActiveAsync(cancellationToken);

            // Группируем по ParentId. null — корневые.
            Dictionary<Guid, List<Category>> childrenByParent = new();
            foreach (Category category in all)
            {
                if (!category.ParentId.HasValue)
                {
                    continue;
                }

                if (!childrenByParent.TryGetValue(category.ParentId.Value, out List<Category>? list))
                {
                    list = new List<Category>();
                    childrenByParent[category.ParentId.Value] = list;
                }

                list.Add(category);
            }

            // Рекурсивный спуск от корней.
            IReadOnlyList<CategoryNodeDto> roots = all
                .Where(c => c.ParentId is null)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name)
                .Select(c => BuildNode(c, childrenByParent))
                .ToList();

            return Result<IReadOnlyList<CategoryNodeDto>>.Success(roots);
        }

        private static CategoryNodeDto BuildNode(
            Category category,
            IReadOnlyDictionary<Guid, List<Category>> childrenByParent)
        {
            IReadOnlyList<CategoryNodeDto> childDtos;

            if (childrenByParent.TryGetValue(category.Id, out List<Category>? children))
            {
                childDtos = children
                    .OrderBy(c => c.Order)
                    .ThenBy(c => c.Name)
                    .Select(c => BuildNode(c, childrenByParent))
                    .ToList();
            }
            else
            {
                childDtos = Array.Empty<CategoryNodeDto>();
            }

            return new CategoryNodeDto(
                Id: category.Id,
                Name: category.Name,
                Slug: category.Slug,
                Order: category.Order,
                IconMediaId: category.IconMediaId,
                Children: childDtos);
        }
    }
}
