namespace BuildingBlocks.Correlation
{
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly ICorrelationContext _correlationContext;

        public CorrelationIdDelegatingHandler(ICorrelationContext correlationContext)
        {
            _correlationContext = correlationContext;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_correlationContext.CorrelationId))
            {
                request.Headers.Remove(CorrelationIdConstants.HttpHeaderName);
                request.Headers.TryAddWithoutValidation(
                    CorrelationIdConstants.HttpHeaderName,
                    _correlationContext.CorrelationId);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
