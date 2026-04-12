namespace food_market_narrator.Services;

// Service xử lý deep link QR và chỉ chấp nhận đúng schema/host mà app hỗ trợ.
public class QrAccessService : IQrAccessService
{
    // Nhận deep link từ dispatcher và áp dụng vào trạng thái app nếu hợp lệ.
    public void ApplyDeepLink(string deepLinkUrl)
    {
        if (!TryParseDeepLink(deepLinkUrl))
        {
            return;
        }
    }

    // Validate định dạng deep link theo contract: foodmarketnarrator://open
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