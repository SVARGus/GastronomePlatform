using FluentValidation;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;
using GastronomePlatform.Modules.Subscriptions.Domain.Repositories;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Repositories;
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

            // Репозитории (Phase A: минимальный набор под UC-SUB-001/004/007 и авторизацию POL-004).
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<IPlanPriceRepository, PlanPriceRepository>();
            services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();

            // Авторизация POL-004 (Application/Authorization/).
            services.AddScoped<ISubscriptionAccessService, SubscriptionAccessService>();
            services.AddScoped<ISubscriptionAccessPolicy, SubscriptionAccessPolicy>();

            // Заглушка покупочного роль-гейта — реальная реализация на Этапе 6 (KYC через Users).
            services.AddScoped<IRoleEligibilityService, RoleEligibilityService>();

            // IPaymentGateway (mock в Phase A → YooKassa в Phase B),
            // hosted-сервис UC-SUB-200 (scheduler-скелет) —
            // будут добавляться по мере появления UC-потребителей.

            return services;
        }
    }
}
