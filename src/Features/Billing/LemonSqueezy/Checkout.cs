namespace Aptabase.Features.Billing.LemonSqueezy;

public class Checkout
{
    public CheckoutData Data { get; set; } = new CheckoutData();
}

public class CheckoutData
{
    public string Id { get; set; } = "";
    public CheckoutAttributes Attributes { get; set; } = new CheckoutAttributes();
}

public class CheckoutAttributes
{
    public string Url { get; set; } = "";
}
