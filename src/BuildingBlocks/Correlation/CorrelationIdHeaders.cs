using System.Text;

namespace BuildingBlocks.Correlation
{
    public static class CorrelationIdHeaders
    {
        public static string? NormalizeOrNull(string? value)
        {
            return Guid.TryParse(value, out var guid)
                ? guid.ToString("D")
                : null;
        }

        public static string? ReadRabbitMqCorrelationId(IDictionary<string, object>? headers)
        {
            if (headers is null ||
                !headers.TryGetValue(CorrelationIdConstants.RabbitMqHeaderName, out var value))
            {
                return null;
            }

            return NormalizeOrNull(ConvertHeaderValue(value));
        }

        public static string? ConvertHeaderValue(object? value)
        {
            return value switch
            {
                null => null,
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                ReadOnlyMemory<byte> bytes => Encoding.UTF8.GetString(bytes.Span),
                string text => text,
                Guid guid => guid.ToString("D"),
                _ => Convert.ToString(value)
            };
        }

        public static Dictionary<string, object> CreateRabbitMqHeaders(string correlationId)
        {
            return new Dictionary<string, object>
            {
                [CorrelationIdConstants.RabbitMqHeaderName] = correlationId
            };
        }

        public static void EnsureRabbitMqCorrelationId(
            IDictionary<string, object> headers,
            string correlationId)
        {
            if (ReadRabbitMqCorrelationId(headers) is null)
            {
                headers[CorrelationIdConstants.RabbitMqHeaderName] = correlationId;
            }
        }
    }
}
