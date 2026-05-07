using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext модуля Dishes.
    /// Работает со схемой <c>dishes</c> базы данных PostgreSQL.
    /// </summary>
    public sealed class DishesDbContext : DbContext
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DishesDbContext"/>.
        /// </summary>
        /// <param name="options">Параметры конфигурации DbContext.</param>
        public DishesDbContext(DbContextOptions<DishesDbContext> options) : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DishesDbContext).Assembly);

            modelBuilder.HasDefaultSchema("dishes");
        }
    }
}
