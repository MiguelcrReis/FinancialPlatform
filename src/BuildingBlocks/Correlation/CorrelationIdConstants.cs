namespace BuildingBlocks.Correlation
{
    public static class CorrelationIdConstants
    {
        public const string HttpHeaderName = "X-Correlation-Id";
        public const string RabbitMqHeaderName = "x-correlation-id";
        public const string LogPropertyName = "CorrelationId";
        public const string ActivityTagName = "correlation.id";
    }
}
