namespace BuildingBlocks.Correlation
{
    public class CorrelationContext : ICorrelationContext
    {
        public string? CorrelationId { get; set; }
    }
}
