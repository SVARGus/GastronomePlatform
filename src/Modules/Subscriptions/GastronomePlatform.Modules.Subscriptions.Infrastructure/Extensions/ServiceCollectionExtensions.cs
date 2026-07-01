using FluentValidation;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GastronomePlatform.Modules.Subscriptions.Infrastructure.Extensions
{
    /// <summary>
    /// Расширение для регистрации модуля Subscriptions в DI-контейнере.
    /// Вызывается один раз из WebAPI/Program.cs.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Регистрирует все зависимости модуля Subscriptions.
        /// </summary>
        /// <param name="services">Коллекция сервисов DI.</param>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
        public static IServiceCollection AddSubscriptionsModule(this IServiceCollection services, IConfiguration configuration)
        {
            // MediatR: сканирует Application-сборку — там лежат command/query/event handlers.
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(GastronomePlatform.Modules.Subscriptions.Application.AssemblyReference).Assembly));

            // FluentValidation: валидаторы лежат рядом с командами в Application.
            services.AddValidatorsFromAssembly(
                typeof(GastronomePlatform.Modules.Subscriptions.Application.AssemblyReference).Assembly);

            // DbContext: схема "subscriptions" задана в OnModelCreating через HasDefaultSchema.
            services.AddDbContext<SubscriptionsDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Database")));

            // Репозитории, конфигурация, IPaymentGateway (mock в Phase A → YooKassa в Phase B),
            // ISubscriptionAccessPolicy, ISubscriptionAccessService, IRoleEligibilityService,
            // hosted-сервис UC-SUB-200 (scheduler-скелет) — будут добавляться по мере появления UC-потребителей.

            return services;
        }
    }
}
