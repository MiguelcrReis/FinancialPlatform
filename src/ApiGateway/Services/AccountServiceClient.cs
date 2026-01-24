namespace ApiGateway.Services;

public class AccountServiceClient
{
    private readonly HttpClient _httpClient;

    public AccountServiceClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("AccountService");
    }

    public async Task<bool> ValidateAsync(object request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/accounts/validate",
            request,
            cancellationToken
        );

        return response.IsSuccessStatusCode;
    }
}
