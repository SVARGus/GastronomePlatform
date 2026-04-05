using GastronomePlatform.Modules.Auth.Application.Abstractions;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using GastronomePlatform.Modules.Auth.Infrastructure.Identity;
using GastronomePlatform.Modules.Auth.Infrastructure.Persistence;
using GastronomePlatform.Modules.Auth.Infrastructure.Repositories;
using GastronomePlatform.Modules.Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            // Регистрация MediatR
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(
                    typeof(GastronomePlatform.Modules.Auth.Application.AssemblyReference).Assembly));

            // Регистрация DbContext
            services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Database")));

            // Регистрация ASP.NET Core Identity
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                // Настройки пароля
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // Блокировка при неудачных попытках
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Настройки пользователя
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

            // Регистрация IOptions<JwtSettings>
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SECTION_NAME));

            // Регистрация Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Регистрация JWT-сервиса
            services.AddScoped<IJwtService, JwtService>();

            // Регистрация межмодульных сервисов
            services.AddScoped<IAuthUserService, AuthUserService>();

            return services;
        }
    }
}
