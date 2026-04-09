using Microsoft.Maui.Networking;

namespace food_market_narrator.Services;

public interface INetworkAccessService
{
    NetworkAccess CurrentNetworkAccess { get; }
}
