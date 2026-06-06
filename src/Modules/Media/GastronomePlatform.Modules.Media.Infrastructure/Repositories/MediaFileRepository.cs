using GastronomePlatform.Modules.Media.Domain.Entities;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using GastronomePlatform.Modules.Media.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Media.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация <see cref="IMediaFileRepository"/> через EF Core.
    /// </summary>
    /// <remarks>
    /// На bootstrap-этапе модуля содержит минимальный набор операций. Специализированные
    /// запросы (загрузка с миниатюрами, выборка по владельцу, выборка по сущности
    /// для каскадного удаления) добавляются по мере появления UC-потребителей.
    /// </remarks>
    public sealed class MediaFileRepository : IMediaFileRepository
    {
        private readonly MediaDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MediaFileRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Media.</param>
        public MediaFileRepository(MediaDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<MediaFile?> GetByIdWithThumbnailsAsync(Guid id, CancellationToken cancellationToken = default)
            => await _context.MediaFiles
                .Include(m => m.Thumbnails)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MediaFile>> ListByOwnerAsync(
            Guid ownerUserId,
            MediaStatus? status,
            string? entityType,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.MediaFiles
                .Where(m => m.OwnerUserId == ownerUserId);

            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(m => m.EntityType == entityType);
            }

            return await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyDictionary<Guid, MediaFile>> GetBatchByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var files = await _context.MediaFiles
                .Where(m => ids.Contains(m.Id))
                .ToListAsync(cancellationToken);

            return files.ToDictionary(m => m.Id);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<MediaFile>> ListByEntityAsync(
            string entityType,
            Guid entityId,
            CancellationToken cancellationToken = default)
            => await _context.MediaFiles
                .Where(m => m.EntityType == entityType && m.EntityId == entityId
                            && m.Status != MediaStatus.Deleted)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(MediaFile mediaFile, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mediaFile);
            await _context.MediaFiles.AddAsync(mediaFile, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);
    }
}
