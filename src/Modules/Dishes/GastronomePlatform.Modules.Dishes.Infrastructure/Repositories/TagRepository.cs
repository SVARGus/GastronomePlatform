using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="ITagRepository"/> через EF Core.
    /// </summary>
    /// <remarks>
    /// Создан вместе с первым UC-потребителем (UC-DSH-008 SetTags — нужны
    /// <see cref="ITagRepository.ListByNormalizedNamesAsync"/>,
    /// <see cref="ITagRepository.ListByIdsAsync"/> и
    /// <see cref="ITagRepository.SlugExistsAsync"/>). Остальные методы интерфейса
    /// реализованы заодно — это простые EF-запросы; полноценно нагружены они будут
    /// admin-командами (UC-DSH-130, UC-DSH-131) и query (UC-DSH-060, UC-DSH-061).
    /// </remarks>
    public sealed class TagRepository : ITagRepository
    {
        private readonly DishesDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="TagRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Dishes.</param>
        public TagRepository(DishesDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.Tags.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<Tag?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
            => await _context.Tags.FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Tag>> ListByNormalizedNamesAsync(
            IReadOnlyCollection<string> normalizedNames,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(normalizedNames);

            if (normalizedNames.Count == 0)
            {
                return Array.Empty<Tag>();
            }

            return await _context.Tags
                .Where(x => normalizedNames.Contains(x.NormalizedName))
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Tag>> ListByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ids);

            if (ids.Count == 0)
            {
                return Array.Empty<Tag>();
            }

            return await _context.Tags
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Tag>> SearchByNormalizedNamePrefixAsync(
            string normalizedPrefix,
            int limit,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(normalizedPrefix);

            if (normalizedPrefix.Length == 0 || limit <= 0)
            {
                return Array.Empty<Tag>();
            }

            return await _context.Tags
                .AsNoTracking()
                .Where(x => x.NormalizedName.StartsWith(normalizedPrefix))
                .OrderByDescending(x => x.UsageCount)
                .ThenBy(x => x.NormalizedName)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Tag>> ListTopVerifiedByUsageAsync(
            int limit,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0)
            {
                return Array.Empty<Tag>();
            }

            return await _context.Tags
                .AsNoTracking()
                .Where(x => x.IsVerified)
                .OrderByDescending(x => x.UsageCount)
                .ThenBy(x => x.NormalizedName)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
            => await _context.Tags.AnyAsync(x => x.Slug == slug, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tag);
            await _context.Tags.AddAsync(tag, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
