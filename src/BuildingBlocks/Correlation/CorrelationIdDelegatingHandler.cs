using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Correlation
{
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CorrelationIdDelegatingHandler(
            ICorrelationContext correlationContext,
            IHttpContextAccessor httpContextAccessor)
        {
            _correlationContext = correlationContext;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var correlationId = _httpContextAccessor.HttpContext?.Items[CorrelationIdConstants.LogPropertyName] as string
                ?? _correlationContext.CorrelationId;

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                request.Headers.Remove(CorrelationIdConstants.HttpHeaderName);
                request.Headers.TryAddWithoutValidation(
                    CorrelationIdConstants.HttpHeaderName,
                    correlationId);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
