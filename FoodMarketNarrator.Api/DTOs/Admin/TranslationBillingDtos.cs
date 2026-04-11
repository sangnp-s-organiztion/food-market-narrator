namespace food_market_narrator_api.DTOs.Admin;

public class TranslationMonthlyBillingItemResponse
{
    public int SellerUserId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;
    public string BillingMonth { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessRequests { get; set; }
    public int FailedRequests { get; set; }
    public decimal TotalBillableUnits { get; set; }
    public decimal TranslationBillableUnits { get; set; }
    public decimal AudioBillableUnits { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime LastRecomputedAtUtc { get; set; }
}

public class TranslationMonthlyBillingSummaryResponse
{
    public string BillingMonth { get; set; } = string.Empty;
    public long SellerCount { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessRequests { get; set; }
    public int FailedRequests { get; set; }
    public decimal TotalBillableUnits { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
}

public class TranslationMonthlyBillingListResponse
{
    public List<TranslationMonthlyBillingItemResponse> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public TranslationMonthlyBillingSummaryResponse Summary { get; set; } = new();
}

public class TranslationUsageLedgerItemResponse
{
    public string UsageEventId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int SellerUserId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;
    public string RestaurantId { get; set; } = string.Empty;
    public int? AudioId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string UnitType { get; set; } = "chars";
    public int InputChars { get; set; }
    public int OutputChars { get; set; }
    public decimal BillableUnits { get; set; }
    public decimal CostAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string BillingMonth { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class TranslationUsageLedgerSummaryResponse
{
    public string BillingMonth { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public decimal TotalBillableUnits { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
}

public class TranslationUsageLedgerListResponse
{
    public List<TranslationUsageLedgerItemResponse> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public TranslationUsageLedgerSummaryResponse Summary { get; set; } = new();
}

public class AudioUsageLedgerItemResponse
{
    public string UsageEventId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int SellerUserId { get; set; }
    public string SellerUsername { get; set; } = string.Empty;
    public string RestaurantId { get; set; } = string.Empty;
    public int? AudioId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string UnitType { get; set; } = "chars";
    public int InputChars { get; set; }
    public int OutputChars { get; set; }
    public decimal BillableUnits { get; set; }
    public string BillingMonth { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class AudioUsageLedgerSummaryResponse
{
    public string BillingMonth { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public decimal TotalBillableUnits { get; set; }
}

public class AudioUsageLedgerListResponse
{
    public List<AudioUsageLedgerItemResponse> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public AudioUsageLedgerSummaryResponse Summary { get; set; } = new();
}
