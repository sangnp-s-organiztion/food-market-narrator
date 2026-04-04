namespace food_market_narrator.Services;

public interface IQrAccessService
{
    bool IsQrTimeRestricted { get; }
    DateTime? QrAccessExpiresAtUtc { get; }
    string LastBlockReason { get; }

    void ApplyDeepLink(string deepLinkUrl);
    Task<bool> CanContinueNarrationAsync(string sessionId, CancellationToken cancellationToken = default);
}