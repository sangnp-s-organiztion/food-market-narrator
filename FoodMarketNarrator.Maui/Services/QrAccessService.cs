namespace food_market_narrator.Services;

public class QrAccessService : IQrAccessService
{
    public void ApplyDeepLink(string deepLinkUrl)
    {
        if (!TryParseDeepLink(deepLinkUrl))
        {
            return;
        }
    }

    private static bool TryParseDeepLink(string deepLinkUrl)
    {
        if (!Uri.TryCreate(deepLinkUrl, UriKind.Absolute, out var parsedUri))
        {
            return false;
        }

        if (!string.Equals(parsedUri.Scheme, "foodmarketnarrator", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(parsedUri.Host, "open", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}