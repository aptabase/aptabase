using System.Text.Json;
using Aptabase.Features.Authentication;
using Yoh.Text.Json.NamingPolicies;

namespace Aptabase.Features.Billing.LemonSqueezy;

public class LemonSqueezyClient
{
    private HttpClient _httpClient;
    private EnvSettings _env;
    private ILogger _logger;

    public static readonly JsonSerializerOptions JsonSettings = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower
    };

    public LemonSqueezyClient(IHttpClientFactory factory, EnvSettings env, ILogger<LemonSqueezyClient> logger)
    {
        _httpClient = factory.CreateClient("LemonSqueezy");
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string?> CreateCheckout(UserIdentity user, CancellationToken cancellationToken)
    {
        var body = new {
            data = new {
                type = "checkouts",
                attributes = new {
                    checkout_data = new {
                        email = user.Email,
                        custom = new {
                            user_id = user.Id,
                            region = _env.Region
                        }
                    },
                    product_options = new {
                        redirect_url = $"{_env.SelfBaseUrl}/billing",
                    },
                    checkout_options = new {
                        embed = false,
                        dark = false
                    }
                },
                relationships = new {
                    store = new {
                        data = new {
                            type = "stores",
                            id = "30508"
                        }
                    },
                    variant = new {
                        data = new {
                            type = "variants",
                            id = _env.IsProduction ? "103474" : "85183"
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/checkouts", body, JsonSettings, cancellationToken);

        await response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<GetResponse<Resource<CheckoutAttributes>>>(JsonSettings);
        return result?.Data.Attributes.Url ?? "";
    }

    public async Task<string> GetBillingPortalUrl(long subscriptionId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/v1/subscriptions/{subscriptionId}", cancellationToken);

        await response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<GetResponse<Resource<SubscriptionAttributes>>>(JsonSettings);
        return result?.Data.Attributes.Urls.CustomerPortal ?? "";
    }
}