using Microsoft.Maui.Networking;

namespace food_market_narrator.Services;

public class MauiNetworkAccessService : INetworkAccessService
{
    public NetworkAccess CurrentNetworkAccess => Connectivity.Current.NetworkAccess;
}
