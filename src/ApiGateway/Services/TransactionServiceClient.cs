namespace ApiGateway.Services
{
    public class TransactionServiceClient
    {
        private readonly HttpClient _httpClient;

        public TransactionServiceClient(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("TransactionService");
        }

        public async Task<HttpResponseMessage> CreateAsync(object request)
        {
            return await _httpClient.PostAsJsonAsync(
                "/api/transactions/process",
                request
            );
        }
    }
}
