using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GastronomePlatform.Common.Infrastructure.Extensions
{
    /// <summary>
    /// Методы расширения для регистрации сервисов общего слоя.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommonInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddHttpContextAccessor();

            // Pipeline Behaviors и handlers Common.Application регистрируются здесь
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(Common.Application.AssemblyReference).Assembly));

            return services;
        }
    }
}
