using GastronomePlatform.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Users.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext модуля Users.
    /// Работает со схемой <c>users</c> базы данных PostgreSQL.
    /// </summary>
    public sealed class UsersDbContext : DbContext
    {
        /// <summary>
        /// Таблица профилей пользователей.
        /// </summary>
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UsersDbContext"/>.
        /// </summary>
        /// <param name="options">Параметры конфигурации DbContext.</param>
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);

            modelBuilder.HasDefaultSchema("users");
        }
    }
}
