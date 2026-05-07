using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Persistence
{
    /// <summary>
    /// Фабрика DbContext для EF Core CLI (dotnet ef migrations, dotnet ef database update).
    /// При запуске приложения не используется — только для инструментов разработки.
    /// </summary>
    public sealed class DishesDbContextFactory : IDesignTimeDbContextFactory<DishesDbContext>
    {
        /// <inheritdoc/>
        public DishesDbContext CreateDbContext(string[] args)
        {
            string basePath = Directory.GetCurrentDirectory();

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            string connectionString = configuration.GetConnectionString("Database")
                ?? throw new InvalidOperationException(
                    "Строка подключения 'Database' не найдена. " +
                    "Проверьте appsettings.Development.json в проекте WebAPI.");

            DbContextOptionsBuilder<DishesDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString);

            return new DishesDbContext(optionsBuilder.Options);
        }
    }
}
