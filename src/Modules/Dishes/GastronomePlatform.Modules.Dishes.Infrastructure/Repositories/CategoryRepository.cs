using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="ICategoryRepository"/> через EF Core.
    /// </summary>
    /// <remarks>
    /// Создан вместе с первым UC-потребителем (UC-DSH-007 SetCategories — нужен
    /// <see cref="ICategoryRepository.ListByIdsAsync"/>). Остальные методы интерфейса
    /// реализованы заодно — это простые EF-запросы; полноценно нагружены они будут
    /// admin-командами UC-DSH-101..105 и query UC-DSH-057..059.
    /// </remarks>
    public sealed class CategoryRepository : ICategoryRepository
    {
        private readonly DishesDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CategoryRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Dishes.</param>
        public CategoryRepository(DishesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => await _context.Categories.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken = default)
            => await _context.Categories
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Category>> ListByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ids);

            if (ids.Count == 0)
            {
                return Array.Empty<Category>();
            }

            return await _context.Categories
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id) && x.IsActive)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(category);
            await _context.Categories.AddAsync(category, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
