using FluentValidation;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GastronomePlatform.Modules.Dishes.Infrastructure.Extensions
{
    /// <summary>
    /// Расширение для регистрации модуля Dishes в DI-контейнере.
    /// Вызывается один раз из WebAPI/Program.cs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует все зависимости модуля Dishes.
        /// </summary>
        /// <param name="services">Коллекция сервисов DI.</param>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
        public static IServiceCollection AddDishesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(GastronomePlatform.Modules.Dishes.Application.AssemblyReference).Assembly));

            // Регистрация Валидаторов
            services.AddValidatorsFromAssembly(typeof(GastronomePlatform.Modules.Dishes.Application.AssemblyReference).Assembly);

            // Регистрация DbContext
            services.AddDbContext<DishesDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Database")));

            // TODO: services.AddScoped<IDishRepository, DishRepository>() и другие репозитории — по мере появления сущностей
            // TODO: специфичные сервисы модуля (например, для проверки POL-001 Dish Ownership)

            return services;
        }
    }
}
