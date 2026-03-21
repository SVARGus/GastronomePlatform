using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Extensions
{
    /// <summary>
    /// Расширение для регистрации модуля Auth в DI-контейнере.
    /// Вызывается один раз из WebAPI/Program.cs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует все зависимости модуля аутентификации.
        /// </summary>
        /// <param name="services">Коллекция сервисов DI.</param>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
        public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(Auth.Application.AssemblyReference).Assembly));

            // TODO: Этап 1 — DbContext (Auth schema)
            // TODO: Этап 1 — Repositories
            // TODO: Этап 1 — JWT-сервис
            // TODO: Этап 1 — ASP.NET Core Identity

            return services;
        }
    }
}
