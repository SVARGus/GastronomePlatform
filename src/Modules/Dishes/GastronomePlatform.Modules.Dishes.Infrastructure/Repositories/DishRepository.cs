using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Enums;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="IDishRepository"/> через EF Core.
    /// </summary>
    /// <remarks>
    /// Создан вместе с первым UC-потребителем (UC-DSH-001 CreateDishDraft). По мере
    /// появления последующих UC методы могут оптимизироваться (например, добавляться
    /// специализированные запросы каталога). На Этапе 2 реализация прямолинейная.
    /// </remarks>
    public sealed class DishRepository : IDishRepository
    {
        private readonly DishesDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DishRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Dishes.</param>
        public DishRepository(DishesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<Dish?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Dishes.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Dish?> GetByIdWithRecipeAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Dishes
                .Include(d => d.Recipe).ThenInclude(r => r.Timing)
                .Include(d => d.Recipe).ThenInclude(r => r.Yield)
                .Include(d => d.Recipe).ThenInclude(r => r.Nutrition)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Dish?> GetByIdWithFullRecipeAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Dishes
                .Include(d => d.Recipe).ThenInclude(r => r.Timing)
                .Include(d => d.Recipe).ThenInclude(r => r.Yield)
                .Include(d => d.Recipe).ThenInclude(r => r.Nutrition)
                .Include(d => d.Recipe).ThenInclude(r => r.Steps)
                .Include(d => d.Recipe).ThenInclude(r => r.Ingredients)
                .Include(d => d.Categories)
                .Include(d => d.Tags)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Dish?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Dishes
                .Include(d => d.Categories)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Dish?> GetByIdWithTagsAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Dishes
                .Include(d => d.Tags)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Dish?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => await _context.Dishes.FirstOrDefaultAsync(d => d.Slug == slug, cancellationToken);

        /// <inheritdoc/>
        public async Task<(IReadOnlyList<Dish> Items, int TotalCount)> ListDraftsByAuthorAsync(
            Guid authorUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Базовый фильтр для двух запросов (Count + Items). Не материализуется
            // — это IQueryable.
            IQueryable<Dish> query = _context.Dishes
                .AsNoTracking()
                .Where(d => d.AuthorUserId == authorUserId && d.Status == DishStatus.Draft);

            int totalCount = await query.CountAsync(cancellationToken);

            List<Dish> items = await query
                .OrderByDescending(d => d.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<(IReadOnlyList<Dish> Items, int TotalCount)> ListPublishedByAuthorAsync(
            Guid authorUserId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Фильтр публичных блюд — наличие jsonb-снепшота. Сравнение с null
            // транслируется в "PublishedVersionData IS NOT NULL".
            IQueryable<Dish> query = _context.Dishes
                .AsNoTracking()
                .Where(d => d.AuthorUserId == authorUserId && d.PublishedVersionData != null);

            int totalCount = await query.CountAsync(cancellationToken);

            List<Dish> items = await query
                .OrderByDescending(d => d.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
            => await _context.Dishes.AnyAsync(d => d.Slug == slug, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(Dish dish, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dish);

            await _context.Dishes.AddAsync(dish, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> IncrementViewsAsync(Guid dishId, CancellationToken cancellationToken = default)
            => await _context.Dishes
                .Where(d => d.Id == dishId && d.Status == DishStatus.Published)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(d => d.ViewsCount, d => d.ViewsCount + 1),
                    cancellationToken);

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
