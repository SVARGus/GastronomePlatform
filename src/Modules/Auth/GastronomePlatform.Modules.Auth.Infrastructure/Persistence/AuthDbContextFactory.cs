using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Persistence
{
    /// <summary>
    /// Фабрика DbContext для EF Core CLI (dotnet ef migrations, dotnet ef database update).
    /// При запуске приложения не используется — только для инструментов разработки.
    /// </summary>
    public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        /// <inheritdoc/>
        public AuthDbContext CreateDbContext(string[] args)
        {
            // При запуске через dotnet ef --startup-project
            // текущая директория = папка startup-проекта (WebAPI)
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

            DbContextOptionsBuilder<AuthDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString);

            return new AuthDbContext(optionsBuilder.Options);
        }
    }
}
