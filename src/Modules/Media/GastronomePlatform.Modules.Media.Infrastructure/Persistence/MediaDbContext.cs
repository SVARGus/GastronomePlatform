using GastronomePlatform.Modules.Media.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Media.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext модуля Media.
    /// Работает со схемой <c>media</c> базы данных PostgreSQL.
    /// </summary>
    public sealed class MediaDbContext : DbContext
    {
        /// <summary>
        /// Таблица метаданных медиафайлов — корней агрегата.
        /// </summary>
        public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

        /// <summary>
        /// Таблица миниатюр — частей агрегата <see cref="MediaFile"/>.
        /// </summary>
        public DbSet<MediaThumbnail> MediaThumbnails => Set<MediaThumbnail>();

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MediaDbContext"/>.
        /// </summary>
        /// <param name="options">Параметры конфигурации DbContext.</param>
        public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);

            modelBuilder.HasDefaultSchema("media");
        }
    }
}
