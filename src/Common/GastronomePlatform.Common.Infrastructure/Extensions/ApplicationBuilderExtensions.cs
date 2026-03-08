using GastronomePlatform.Common.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace GastronomePlatform.Common.Infrastructure.Extensions
{
    /// <summary>
    /// Методы расширения для настройки middleware в конвейере обработки запросов.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCommonInfrastructure(this IApplicationBuilder app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
            return app;
        }
    }
}
