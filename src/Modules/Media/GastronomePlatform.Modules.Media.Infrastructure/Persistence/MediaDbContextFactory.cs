using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GastronomePlatform.Modules.Media.Infrastructure.Persistence
{
    /// <summary>
    /// Фабрика DbContext для EF Core CLI (dotnet ef migrations, dotnet ef database update).
    /// При запуске приложения не используется — только для инструментов разработки.
    /// </summary>
    public sealed class MediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
    {
        /// <inheritdoc/>
        public MediaDbContext CreateDbContext(string[] args)
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

            DbContextOptionsBuilder<MediaDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString);

            return new MediaDbContext(optionsBuilder.Options);
        }
    }
}
