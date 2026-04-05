using GastronomePlatform.Modules.Users.Domain.Repositories;
using GastronomePlatform.Modules.Users.Infrastructure.Persistence;
using GastronomePlatform.Modules.Users.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GastronomePlatform.Modules.Users.Infrastructure.Extensions
{
    /// <summary>
    /// Расширение для регистрации модуля Users в DI-контейнере.
    /// Вызывается один раз из WebAPI/Program.cs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует все зависимости модуля Users.
        /// </summary>
        /// <param name="services">Коллекция сервисов DI.</param>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
        public static IServiceCollection AddUsersModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(GastronomePlatform.Modules.Users.Application.AssemblyReference).Assembly));

            // Регистрация DbContext
            services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Database")));

            // Регистрация Repositories
            services.AddScoped<IUserProfileRepository, UserProfileRepository>();

            // IAuthUserService уже зарегистрирован в AddAuthModule()
            // и доступен для внедрения в Users.Application

            return services;
        }
    }
}
