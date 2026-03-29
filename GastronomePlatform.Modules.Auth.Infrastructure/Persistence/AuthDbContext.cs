using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Persistence
{
    /// <summary>
    /// DbContext модуля аутентификации.
    /// Работает со схемой <c>auth</c> базы данных PostgreSQL.
    /// Наследует <see cref="IdentityDbContext{TUser, TRole, TKey}"/> для интеграции
    /// с ASP.NET Core Identity.
    /// </summary>
    public sealed class AuthDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        /// <summary>
        /// Таблица refresh-токенов.
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AuthDbContext"/>.
        /// </summary>
        /// <param name="options">Параметры конфигурации DbContext.</param>
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Применяем все конфигурации из текущей сборки
            builder.ApplyConfigurationsFromAssembly(
                typeof(AuthDbContext).Assembly);

            // Переносим все таблицы Identity в схему auth
            builder.HasDefaultSchema("auth");

            SeedRoles(builder);
        }

        /// <summary>
        /// Заполняет таблицу ролей начальными данными.
        /// Роли создаются один раз при первой миграции.
        /// </summary>
        /// <remarks>
        /// Guid-идентификаторы ролей фиксированы намеренно —
        /// EF Core сравнивает seed-данные при каждой миграции,
        /// и изменяющиеся Id приводят к лишним миграциям.
        /// </remarks>
        /// <param name="builder">Построитель модели EF Core.</param>
        private static void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole<Guid>>().HasData(
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                    Name = PlatformRoles.USER,
                    NormalizedName = PlatformRoles.USER.ToUpperInvariant()
                },
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"),
                    Name = PlatformRoles.PREMIUM,
                    NormalizedName = PlatformRoles.PREMIUM.ToUpperInvariant()
                },
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"),
                    Name = PlatformRoles.CHEF,
                    NormalizedName = PlatformRoles.CHEF.ToUpperInvariant()
                },
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123"),
                    Name = PlatformRoles.RESTAURANT,
                    NormalizedName = PlatformRoles.RESTAURANT.ToUpperInvariant()
                },
                new IdentityRole<Guid>
                {
                    Id = Guid.Parse("e5f6a7b8-c9d0-1234-efab-345678901234"),
                    Name = PlatformRoles.ADMIN,
                    NormalizedName = PlatformRoles.ADMIN.ToUpperInvariant()
                }
            );
        }
    }
}
