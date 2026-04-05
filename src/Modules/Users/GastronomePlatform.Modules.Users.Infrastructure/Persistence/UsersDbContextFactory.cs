using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GastronomePlatform.Modules.Users.Infrastructure.Persistence
{
    /// <summary>
    /// Фабрика DbContext для EF Core CLI (dotnet ef migrations, dotnet ef database update).
    /// При запуске приложения не используется — только для инструментов разработки.
    /// </summary>
    public sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
    {
        /// <inheritdoc/>
        public UsersDbContext CreateDbContext(string[] args)
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

            DbContextOptionsBuilder<UsersDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString);

            return new UsersDbContext(optionsBuilder.Options);
        }
    }
}
