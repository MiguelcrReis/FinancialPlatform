using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace BuildingBlocks.Correlation
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            RequestDelegate next,
            ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            ICorrelationContext correlationContext)
        {
            var receivedCorrelationId = httpContext.Request.Headers[CorrelationIdConstants.HttpHeaderName]
                .FirstOrDefault();

            var correlationId = CorrelationIdHeaders.NormalizeOrNull(receivedCorrelationId);
            if (correlationId is null)
            {
                correlationId = Guid.NewGuid().ToString("D");

                if (!string.IsNullOrWhiteSpace(receivedCorrelationId))
                {
                    _logger.LogWarning(
                        "Invalid correlation id received in {HeaderName}: {ReceivedCorrelationId}. A new correlation id was generated.",
                        CorrelationIdConstants.HttpHeaderName,
                        receivedCorrelationId);
                }
            }

            correlationContext.CorrelationId = correlationId;
            httpContext.Items[CorrelationIdConstants.LogPropertyName] = correlationId;
            httpContext.Response.Headers[CorrelationIdConstants.HttpHeaderName] = correlationId;
            Activity.Current?.SetTag(CorrelationIdConstants.ActivityTagName, correlationId);

            using (LogContext.PushProperty(CorrelationIdConstants.LogPropertyName, correlationId))
            {
                await _next(httpContext);
            }
        }
    }
}
