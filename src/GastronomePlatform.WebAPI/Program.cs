using GastronomePlatform.Common.Application;
using GastronomePlatform.Common.Infrastructure.Extensions;
using MediatR.NotificationPublishers;
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
    .CreateLogger();

try
{
    Log.Information("Запуск GastronomePlatform WebAPI");

    var builder = WebApplication.CreateBuilder(args);

    // === 2. Замена стандартного логгера на Serilog ===
    builder.Host.UseSerilog();

    // === 3. Регистрация сервисов ===
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddCommonInfrastructure();
    builder.Services.AddHealthChecks();
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblies(        
            typeof(Program).Assembly,                   // WebAPI
            typeof(AssemblyReference).Assembly);        // Common.Application
        cfg.NotificationPublisher = new TaskWhenAllPublisher();
    });

    var app = builder.Build();

    // === 4. Middleware конвейер (ПОРЯДОК ВАЖЕН!) ===
    app.UseCommonInfrastructure();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

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

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой при старте");
}
finally
{
    Log.CloseAndFlush();
}
