namespace BuildingBlocks.Correlation
{
    public interface ICorrelationContext
    {
        string? CorrelationId { get; set; }
    }
}
