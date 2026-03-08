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
            return services;
        }
    }
}
