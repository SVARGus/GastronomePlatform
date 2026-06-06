using FluentValidation;
using GastronomePlatform.Modules.Media.Application.Abstractions;
using GastronomePlatform.Modules.Media.Application.Configuration;
using GastronomePlatform.Modules.Media.Application.Contracts;
using GastronomePlatform.Modules.Media.Application.Services;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using GastronomePlatform.Modules.Media.Infrastructure.Persistence;
using GastronomePlatform.Modules.Media.Infrastructure.Processing;
using GastronomePlatform.Modules.Media.Infrastructure.Repositories;
using GastronomePlatform.Modules.Media.Infrastructure.Storage;
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

            // Конфигурация модуля.
            services.Configure<MediaOptions>(
                configuration.GetSection(MediaOptions.SECTION_NAME));

            // Репозитории.
            services.AddScoped<IMediaFileRepository, MediaFileRepository>();

            // Хранилище файлов: LocalFileStorage на Этапе 2.
            // На Этапе 8+ заменяется на S3FileStorage через условную регистрацию по Media:Storage:Provider.
            services.AddSingleton<IFileStorage, LocalFileStorage>();

            // Генератор ключей хранилища (без состояния — Singleton).
            services.AddSingleton<IStorageKeyGenerator, StorageKeyGenerator>();

            // Обработчик изображений на базе SixLabors.ImageSharp v2.1.x (Singleton, thread-safe).
            services.AddSingleton<IImageProcessor, ImageProcessor>();

            // Санитайзер SVG на базе HtmlSanitizer (Singleton, thread-safe через static поле).
            services.AddSingleton<ISvgSanitizer, SvgSanitizer>();

            // Межмодульный контракт IMediaService (Scoped: зависит от ICurrentUserService и IMediaFileRepository).
            services.AddScoped<IMediaService, MediaService>();

            return services;
        }
    }
}
