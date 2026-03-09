using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace GastronomePlatform.Common.Infrastructure.Middleware
{
    /// <summary>
    /// Middleware для работы с Correlation ID (сквозной идентификатор запроса).
    /// Читает X-Correlation-ID из заголовка входящего запроса или генерирует новый.
    /// Пробрасывает идентификатор в HttpContext.Items и заголовок ответа.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemKey = "CorrelationId";

        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId = GetOrGenerateCorrelationId(context);

            context.Items[ItemKey] = correlationId;

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderName))
                {
                    context.Response.Headers[HeaderName] = correlationId;
                }
                return Task.CompletedTask;
            });

            // Serilog: добавляем CorrelationId во ВСЕ логи в рамках этого запроса
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                _logger.LogDebug("Запрос {Method} {Path} — CorrelationId: {CorrelationId}",
                    context.Request.Method, context.Request.Path, correlationId);

                await _next(context);
            }
            // После выхода из using — свойство автоматически убирается
        }

        private static string GetOrGenerateCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out StringValues correlationId) &&
                !StringValues.IsNullOrEmpty(correlationId))
            {
                return correlationId.ToString();
            }

            return Guid.NewGuid().ToString();
        }
    }
}
