namespace food_market_narrator.Services;

public static class AppLinkDispatcher
{
    public static event Action<string>? DeepLinkReceived;

    public static void Dispatch(string deepLinkUrl)
    {
        if (string.IsNullOrWhiteSpace(deepLinkUrl))
        {
            return;
        }

        DeepLinkReceived?.Invoke(deepLinkUrl);
    }
}