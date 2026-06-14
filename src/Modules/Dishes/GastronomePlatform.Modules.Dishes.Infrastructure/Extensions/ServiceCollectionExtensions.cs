using FluentValidation;
using GastronomePlatform.Modules.Dishes.Application.Services;
using GastronomePlatform.Modules.Dishes.Application.Snapshots;
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
            services.AddScoped<IIngredientRepository, IngredientRepository>();
            services.AddScoped<IMeasureUnitRepository, MeasureUnitRepository>();
            services.AddScoped<IIngredientSpecRepository, IngredientSpecRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ITagRepository, TagRepository>();

            // TODO: остальные репозитории (INutritionRepository) —
            // по мере появления UC
            // TODO: специфичные сервисы модуля (например, для проверки POL-001 Dish Ownership)

            // Сборщик jsonb-снепшота для UC-DSH-004 Publish. Stateless, без I/O — Singleton.
            services.AddSingleton<IPublishedDishSnapshotBuilder, PublishedDishSnapshotBuilder>();

            // Парсер jsonb-снепшота для UC-DSH-050 (snapshot-ветка карточки) и
            // UC-DSH-052 (GetDishRecipe). Симметричен Builder, stateless — Singleton.
            services.AddSingleton<IPublishedDishSnapshotReader, PublishedDishSnapshotReader>();

            // Сервис пересчёта маркеров блюда после изменения состава рецепта.
            // Используется хендлерами UC-DSH-030..032; зависит от IIngredientRepository (Scoped).
            services.AddScoped<IDishMarkersRecalculator, DishMarkersRecalculator>();

            return services;
        }
    }
}
