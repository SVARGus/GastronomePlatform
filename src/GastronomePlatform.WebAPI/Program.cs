using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GastronomePlatform.Common.Application.Constants;
using GastronomePlatform.Common.Infrastructure.Extensions;
using GastronomePlatform.Modules.Auth.Infrastructure.Extensions;
using GastronomePlatform.Modules.Auth.Infrastructure.Persistence;
using GastronomePlatform.Modules.Dishes.Infrastructure.Extensions;
using GastronomePlatform.Modules.Dishes.Infrastructure.Persistence;
using GastronomePlatform.Modules.Media.Infrastructure.Extensions;
using GastronomePlatform.Modules.Media.Infrastructure.Persistence;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Extensions;
using GastronomePlatform.Modules.Subscriptions.Infrastructure.Persistence;
using GastronomePlatform.Modules.Users.Infrastructure.Extensions;
using GastronomePlatform.Modules.Users.Infrastructure.Persistence;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// === 1. Конфигурация Serilog (до создания builder) ===
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt", 
        rollingInterval: RollingInterval.Day, 
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    // Bootstrap-логгер создаётся до чтения конфигурации — адрес Seq берём из
    // переменной окружения (в docker-compose.prod.yml это http://seq:5341);
    // локальная разработка работает по прежнему адресу без настройки.
    .WriteTo.Seq(Environment.GetEnvironmentVariable("Seq__ServerUrl") ?? "http://localhost:5341")
    .CreateLogger();

try
{
    Log.Information("Запуск GastronomePlatform WebAPI");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // === 2. Замена стандартного логгера на Serilog ===
    builder.Host.UseSerilog();

    // === 3. Регистрация сервисов ===
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // camelCase — отраслевой стандарт REST/JSON и согласован с
            // GlobalExceptionHandlingMiddleware (ответы 500 уже camelCase).
            // SnapshotJsonOptions для jsonb-снепшота Dish.PublishedVersionData
            // оставлен на PascalCase — это внутренний формат БД, не виден клиенту.
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            // Enum-ы сериализуются и десериализуются как строки ("Medium" вместо 1).
            // Это удобнее для клиентов, читаемее в логах, и стабильно к рефакторингу enum-ов
            // (добавление значения в середину enum не сдвигает индексы существующих значений).
            // Swagger автоматически подхватит конвертер и отрисует enum как dropdown со строками.
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Описание схемы безопасности — JWT Bearer.
        OpenApiSecurityScheme bearerScheme = new()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Введите JWT access token БЕЗ префикса \"Bearer \" — Swagger UI добавит его автоматически. " +
                "Получить токен можно через POST /api/auth/login."
        };

        options.AddSecurityDefinition("Bearer", bearerScheme);

        // Глобальное требование безопасности — применяется ко всем эндпоинтам.
        // Эндпоинты без [Authorize] остаются публичными (Swagger UI показывает замок,
        // но фактическая проверка JWT происходит только на [Authorize]-эндпоинтах).
        OpenApiSecurityRequirement securityRequirement = new()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        };

        options.AddSecurityRequirement(securityRequirement);

        // Подключение XML-документации всех модулей.
        // <GenerateDocumentationFile>true</GenerateDocumentationFile> включён в Directory.Build.props,
        // поэтому после сборки рядом с DLL лежат XML-файлы. Фильтр "GastronomePlatform.*.xml"
        // отсекает XML-файлы сторонних пакетов и берёт только наши сборки (WebAPI, Common.*,
        // Modules.<Module>.{Domain,Application,Infrastructure}).
        foreach (string xmlFile in Directory.EnumerateFiles(AppContext.BaseDirectory, "GastronomePlatform.*.xml"))
        {
            options.IncludeXmlComments(xmlFile, includeControllerXmlComments: true);
        }
    });
    builder.Services.AddCommonInfrastructure();
    builder.Services.AddHealthChecks();
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly);
        cfg.NotificationPublisher = new TaskWhenAllPublisher();
    });

    // === 3.1. Регистрация модулей ===
    builder.Services.AddAuthModule(builder.Configuration);
    builder.Services.AddUsersModule(builder.Configuration);
    builder.Services.AddDishesModule(builder.Configuration);
    builder.Services.AddMediaModule(builder.Configuration);
    builder.Services.AddSubscriptionsModule(builder.Configuration);

    // === 3.2. Настройка JWT Authentication pipeline ===
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        IConfiguration config = builder.Configuration;

        // Отключаем легаси-маппинг входящих claim-ов: "sub" остаётся "sub",
        // не превращается в ClaimTypes.NameIdentifier (long Microsoft URI).
        // Это синхронизирует чтение claim-ов с тем, как они кладутся в JwtService.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = config["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = config["JwtSettings:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["JwtSettings:Secret"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,  // без допуска на расхождение времени

            // Согласовано с короткими именами claim-ов из JwtService:
            // "sub" — идентификатор пользователя (User.Identity.Name),
            // "role" — роли (User.IsInRole(...)).
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

    // === 3.3. Регистрация политик авторизации ===
    builder.Services.AddAuthorization(options =>
    {
        // Политика ValidActor — требует, чтобы JWT содержал claim "sub",
        // парсящийся как Guid. Эндпоинты с [Authorize(Policy = VALID_ACTOR)]
        // получают гарантию валидного идентификатора пользователя на уровне
        // инфраструктуры, без defense-in-depth проверок в Handler-ах.
        options.AddPolicy(AuthorizationPolicies.VALID_ACTOR, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                string? sub = context.User.FindFirstValue("sub");
                return Guid.TryParse(sub, out _);
            });
        });
    });

    WebApplication app = builder.Build();

    // === 3.4. Применение миграций на старте (только по явному флагу) ===
    // Включается конфигурацией Database:MigrateOnStartup = true
    // (appsettings.Production.json): на VPS нет .NET SDK и dotnet-ef,
    // а инстанс приложения один — Migrate() идемпотентен и безопасен.
    // В Development флаг не задан — миграции применяются вручную через CLI,
    // как и раньше (см. 08_Разработка-(Development-Guide)).
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        Log.Information("Database:MigrateOnStartup включён — применение миграций всех модулей");

        using IServiceScope scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
        scope.ServiceProvider.GetRequiredService<UsersDbContext>().Database.Migrate();
        scope.ServiceProvider.GetRequiredService<DishesDbContext>().Database.Migrate();
        scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.Migrate();
        scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>().Database.Migrate();

        Log.Information("Миграции применены");
    }

    // === 4. Middleware конвейер (ПОРЯДОК ВАЖЕН!) ===

    // За reverse-proxy (Caddy) TLS терминируется до приложения: восстанавливаем
    // исходную схему/IP из заголовков X-Forwarded-* и не делаем собственный
    // https-редирект — этим занимается Caddy.
    if (app.Environment.IsProduction())
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
    }

    app.UseCommonInfrastructure();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    // === 4.1. Статика SPA (production-сборка Vite в wwwroot) ===
    // Хэшированные ассеты (/assets/*.js|css) кэшируются агрессивно — имя файла
    // меняется при каждой сборке; index.html не кэшируется, чтобы клиент сразу
    // видел новую версию. До первого npm run build wwwroot пуст — конвейер
    // работает как раньше (чистый API).
    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = context =>
        {
            bool isHashedAsset = context.Context.Request.Path.StartsWithSegments("/assets");
            context.Context.Response.Headers.CacheControl = isHashedAsset
                ? "public,max-age=31536000,immutable"
                : "no-cache";
        }
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false  // Никаких проверок — просто "я жив"
    });

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => true   // Все зарегистрированные проверки
    });

    // === 4.2. SPA-fallback ===
    // Неизвестные /api-маршруты отвечают 404, а не index.html: catch-all ниже
    // по специфичности любых контроллерных маршрутов и срабатывает только когда
    // ни один из них не подошёл.
    app.Map("/api/{**path}", () => Results.NotFound());

    // Всё остальное (deep-link /dishes/plov, /account?tab=…) — маршруты SPA:
    // отдаём index.html, дальше разбирается react-router.
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex) when (ex is not Microsoft.Extensions.Hosting.HostAbortedException)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой при старте");
}
finally
{
    Log.CloseAndFlush();
}
