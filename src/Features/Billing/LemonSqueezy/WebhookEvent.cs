using System.Text.Json.Nodes;

namespace Aptabase.Features.Billing.LemonSqueezy;

public class WebhookEvent
{
    public WebhookEventMeta Meta { get; set; } = new WebhookEventMeta();
    public WebhookEventData Data { get; set; } = new WebhookEventData();
}

public class WebhookEventData
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public JsonObject Attributes { get; set; } = new JsonObject();
}

public class WebhookEventMeta
{
    public string EventName { get; set; } = "";
    public Dictionary<string, string> CustomData { get; set; } = new Dictionary<string, string>();
}

public class SubscriptionDataEvent
{
    public int ProductID { get; set; }
    public int VariantID { get; set; }
    public int CustomerID { get; set; }
    public string Status { get; set; } = "";
    public DateTime? EndsAt { get; set; }
}