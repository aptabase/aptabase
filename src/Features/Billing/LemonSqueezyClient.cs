namespace Aptabase.Features.Billing;

public class LemonSqueezyClient
{
    private HttpClient _httpClient;
    private ILogger _logger;

    public LemonSqueezyClient(IHttpClientFactory factory, ILogger<LemonSqueezyClient> logger)
    {
        _httpClient = factory.CreateClient("LemonSqueezy");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> CreateCheckout(string name, string email, CancellationToken cancellationToken)
    {
        var requestBody = new Checkout {
            Attributes = new CheckoutAttributes{
                CheckoutOptions = new CheckoutOptions {
                    Embed = true,
                    Dark = true,
                },
                CheckoutData = new CheckoutData {
                    Email = email,
                    Name = name,
                },
            }
        };
        var response = await _httpClient.PostAsJsonAsync("/v1/checkouts", new { data = requestBody }, cancellationToken);
        
        var responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseBody);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Checkout>();
        return result?.Attributes.Url;
    }
}