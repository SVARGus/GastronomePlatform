using FluentValidation;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using GastronomePlatform.Modules.Media.Infrastructure.Persistence;
using GastronomePlatform.Modules.Media.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GastronomePlatform.Modules.Media.Infrastructure.Extensions
{
    /// <summary>
    /// Расширение для регистрации модуля Media в DI-контейнере.
    /// Вызывается один раз из WebAPI/Program.cs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует все зависимости модуля Media.
        /// </summary>
        /// <param name="services">Коллекция сервисов DI.</param>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
        public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
        {
            // MediatR: сканирует Application-сборку — там лежат command/query/event handlers.
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(GastronomePlatform.Modules.Media.Application.AssemblyReference).Assembly));

            // FluentValidation: валидаторы лежат рядом с командами в Application.
            services.AddValidatorsFromAssembly(
                typeof(GastronomePlatform.Modules.Media.Application.AssemblyReference).Assembly);

            // DbContext: схема "media" задана в OnModelCreating через HasDefaultSchema.
            services.AddDbContext<MediaDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Database")));

            // Репозитории.
            services.AddScoped<IMediaFileRepository, MediaFileRepository>();

            // TODO: специфичные сервисы модуля — добавятся при реализации UC-MED:
            //   IFileStorage / LocalFileStorage (UC-MED-001),
            //   IStorageKeyGenerator (UC-MED-001),
            //   IMediaService / MediaService (UC-MED-200..204),
            //   IMediaAccessPolicy (POL-002, для UC-MED-002/003/004),
            //   IMediaOwnershipPolicy (POL-003, для UC-MED-005).

            return services;
        }
    }
}
