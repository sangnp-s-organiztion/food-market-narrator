namespace food_market_narrator.Services;

public static class AppLinkDispatcher
{
    public static event Action<string>? DeepLinkReceived;


    // Khi app nhận được deep link URL, nó sẽ phát (dispatch) sự kiện DeepLinkReceived để các phần khác của app xử lý.
    public static void Dispatch(string deepLinkUrl)
    {
        if (string.IsNullOrWhiteSpace(deepLinkUrl))
        {
            return;
        }

        DeepLinkReceived?.Invoke(deepLinkUrl);
    }
}