using GastronomePlatform.Modules.Media.Domain.Entities;
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
