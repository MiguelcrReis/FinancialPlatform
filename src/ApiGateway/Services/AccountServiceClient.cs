namespace ApiGateway.Services;

public class AccountServiceClient
{
    private readonly HttpClient _httpClient;

    public AccountServiceClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("AccountService");
    }

    public async Task<bool> ValidateAsync(object request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/accounts/validate",
            request
        );

        return response.IsSuccessStatusCode;
    }
}
