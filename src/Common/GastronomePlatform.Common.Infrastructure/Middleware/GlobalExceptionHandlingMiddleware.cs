using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GastronomePlatform.Common.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware для глобальной обработки необработанных исключений.
    /// Перехватывает исключения, логирует их и возвращает клиенту
    /// ответ в формате Problem Details (RFC 7807).
    /// Не раскрывает детали исключения клиенту (безопасность).
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly RequestDelegate _next;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // 1. Получаем Correlation ID (добавлен CorrelationIdMiddleware ранее в конвейере)
            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                ?? "unknown";

            // 2. Логируем полную информацию об исключении (только в логи, не клиенту!)
            _logger.LogError(exception,
                "Необработанное исключение при обработке запроса {Method} {Path}. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            // 3. Формируем безопасный ответ клиенту (без деталей исключения)
            var problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Внутренняя ошибка сервера",
                status = StatusCodes.Status500InternalServerError,
                detail = "Произошла непредвиденная ошибка. Обратитесь в поддержку с requestId.",
                errorCode = "SYSTEM.INTERNAL_ERROR",
                requestId = correlationId
            };

            // 4. Устанавливаем параметры ответа
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            // 5. Записываем JSON в тело ответа
            await context.Response.WriteAsJsonAsync(problemDetails, JsonOptions);
        }
    }
}
