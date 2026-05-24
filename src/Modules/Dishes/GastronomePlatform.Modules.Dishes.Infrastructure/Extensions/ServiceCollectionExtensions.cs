using FluentValidation;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence.Interceptors;
using GastronomePlatform.Modules.Dishes.Infrastructure.Repositories;
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

            // SaveChanges-интерсептор автообновления Dish.UpdatedAt. Singleton —
            // без per-request состояния, зависит только от IDateTimeProvider (тоже Singleton).
            services.AddSingleton<UpdatedAtInterceptor>();

            // Регистрация DbContext с подключением интерсептора
            services.AddDbContext<DishesDbContext>((sp, options) =>
            {
                options.UseNpgsql(configuration.GetConnectionString("Database"));
                options.AddInterceptors(sp.GetRequiredService<UpdatedAtInterceptor>());
            });

            // Регистрация Repositories — добавляются по мере появления UC-потребителей
            services.AddScoped<IDishRepository, DishRepository>();

            // TODO: остальные репозитории (ICategoryRepository, ITagRepository, IIngredientRepository,
            // IIngredientSpecRepository, IMeasureUnitRepository, INutritionRepository) — по мере появления UC
            // TODO: специфичные сервисы модуля (например, для проверки POL-001 Dish Ownership)

            return services;
        }
    }
}
