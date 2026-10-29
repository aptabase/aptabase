using System.Net;
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
                            id = StoreId
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

    /// <summary>
    /// The current state of a subscription as LemonSqueezy sees it, or null when LemonSqueezy no longer knows it.
    /// </summary>
    public async Task<SubscriptionAttributes?> GetSubscription(long subscriptionId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/v1/subscriptions/{subscriptionId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await response.EnsureSuccessWithLog(_logger);
        var result = await response.Content.ReadFromJsonAsync<GetResponse<Resource<SubscriptionAttributes>>>(JsonSettings, cancellationToken);
        return result?.Data.Attributes;
    }

    public const string StoreId = "30508";

    // LemonSqueezy serves at most 100 resources per page; asking for more is silently capped, and the
    // loop trusts meta.page.lastPage rather than the requested size so a lower cap still terminates.
    private const int PageSize = 100;
    private const int MaxPages = 200;

    /// <summary>
    /// Every subscription in the store, across all regions, a page at a time. One call per 100
    /// subscriptions, which is what makes a periodic full reconciliation affordable.
    /// </summary>
    public async Task<IReadOnlyList<(long Id, SubscriptionAttributes Attributes)>> ListStoreSubscriptions(CancellationToken cancellationToken)
    {
        var result = new List<(long, SubscriptionAttributes)>();

        for (var page = 1; page <= MaxPages; page++)
        {
            var url = $"/v1/subscriptions?filter[store_id]={StoreId}&page[number]={page}&page[size]={PageSize}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            await response.EnsureSuccessWithLog(_logger);

            var body = await response.Content.ReadFromJsonAsync<PagedList<SubscriptionAttributes>>(JsonSettings, cancellationToken);
            if (body is null)
                break;

            foreach (var item in body.Data)
            {
                if (long.TryParse(item.Id, out var id))
                    result.Add((id, item.Attributes));
            }

            if (page >= body.Meta.Page.LastPage)
                break;
        }

        return result;
    }

    public async Task<string> GetBillingPortalUrl(long subscriptionId, CancellationToken cancellationToken)
    {
        var sub = await GetSubscription(subscriptionId, cancellationToken);
        return sub?.Urls.CustomerPortal ?? "";
    }
}
