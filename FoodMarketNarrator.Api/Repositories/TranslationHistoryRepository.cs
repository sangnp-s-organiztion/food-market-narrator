using MongoDB.Bson;
using MongoDB.Driver;

namespace food_market_narrator_api.Repositories;

public class TranslationHistoryRepository
{
    private readonly IMongoCollection<BsonDocument> _translationJobs;
    private readonly IMongoCollection<BsonDocument> _usageLedger;
    private readonly IMongoCollection<BsonDocument> _translationVersions;
    private readonly IMongoCollection<BsonDocument> _monthlyBilling;

    public TranslationHistoryRepository(IMongoDatabase mongoDatabase)
    {
        _translationJobs = mongoDatabase.GetCollection<BsonDocument>("TranslationJobs");
        _usageLedger = mongoDatabase.GetCollection<BsonDocument>("TranslationUsageLedger");
        _translationVersions = mongoDatabase.GetCollection<BsonDocument>("AudioTranslationVersions");
        _monthlyBilling = mongoDatabase.GetCollection<BsonDocument>("TranslationBillingMonthly");
    }

    public async Task InsertTranslationJobAsync(TranslationJobRecord record)
    {
        var doc = new BsonDocument
        {
            { "request_id", record.RequestId },
            { "seller_user_id", record.SellerUserId },
            { "restaurant_id", record.RestaurantId },
            { "audio_id", record.AudioId.HasValue ? (BsonValue)record.AudioId.Value : BsonNull.Value },
            { "source_language_code", record.SourceLanguageCode },
            { "target_language_code", record.TargetLanguageCode },
            { "source_text_hash", record.SourceTextHash },
            { "source_char_count", record.SourceCharCount },
            { "provider", record.Provider },
            { "provider_endpoint", record.ProviderEndpoint },
            { "status", record.Status },
            { "started_at", record.StartedAtUtc },
            { "finished_at", record.FinishedAtUtc.HasValue ? (BsonValue)record.FinishedAtUtc.Value : BsonNull.Value },
            { "latency_ms", record.LatencyMs.HasValue ? (BsonValue)record.LatencyMs.Value : BsonNull.Value },
            { "error_code", string.IsNullOrWhiteSpace(record.ErrorCode) ? BsonNull.Value : record.ErrorCode },
            { "error_message", string.IsNullOrWhiteSpace(record.ErrorMessage) ? BsonNull.Value : record.ErrorMessage },
            { "created_at", record.CreatedAtUtc }
        };

        await _translationJobs.InsertOneAsync(doc);
    }

    public async Task InsertUsageLedgerAsync(TranslationUsageLedgerRecord record)
    {
        var doc = new BsonDocument
        {
            { "usage_event_id", record.UsageEventId },
            { "request_id", record.RequestId },
            { "job_id", string.IsNullOrWhiteSpace(record.JobId) ? BsonNull.Value : record.JobId },
            { "seller_user_id", record.SellerUserId },
            { "restaurant_id", record.RestaurantId },
            { "audio_id", record.AudioId.HasValue ? (BsonValue)record.AudioId.Value : BsonNull.Value },
            { "provider", record.Provider },
            { "action_type", record.ActionType },
            { "unit_type", record.UnitType },
            { "input_chars", record.InputChars },
            { "output_chars", record.OutputChars },
            { "billable_units", record.BillableUnits },
            { "pricing_snapshot", new BsonDocument
                {
                    { "rate_version", record.RateVersion },
                    { "price_per_1k_units", record.PricePer1KUnits },
                    { "currency", record.Currency }
                }
            },
            { "cost_amount", record.CostAmount },
            { "tax_amount", record.TaxAmount },
            { "total_amount", record.TotalAmount },
            { "status", record.Status },
            { "billing_month", record.BillingMonth },
            { "created_at", record.CreatedAtUtc }
        };

        await _usageLedger.InsertOneAsync(doc);
    }

    public async Task InsertTranslationVersionAsync(AudioTranslationVersionRecord record)
    {
        var doc = new BsonDocument
        {
            { "seller_user_id", record.SellerUserId },
            { "restaurant_id", record.RestaurantId },
            { "audio_id", record.AudioId },
            { "source_language_code", record.SourceLanguageCode },
            { "target_language_code", record.TargetLanguageCode },
            { "source_text", record.SourceText },
            { "translated_text", record.TranslatedText },
            { "translated_text_hash", record.TranslatedTextHash },
            { "version_no", record.VersionNo },
            { "is_active", record.IsActive },
            { "generation_method", record.GenerationMethod },
            { "job_id", string.IsNullOrWhiteSpace(record.JobId) ? BsonNull.Value : record.JobId },
            { "usage_event_id", string.IsNullOrWhiteSpace(record.UsageEventId) ? BsonNull.Value : record.UsageEventId },
            { "created_at", record.CreatedAtUtc },
            { "activated_at", record.ActivatedAtUtc.HasValue ? (BsonValue)record.ActivatedAtUtc.Value : BsonNull.Value },
            { "superseded_at", BsonNull.Value }
        };

        await _translationVersions.InsertOneAsync(doc);
    }

    public async Task UpsertMonthlyBillingAsync(MonthlyBillingSnapshotRecord record)
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("seller_user_id", record.SellerUserId),
            Builders<BsonDocument>.Filter.Eq("billing_month", record.BillingMonth));

        var update = Builders<BsonDocument>.Update
            .Inc("total_requests", record.TotalRequests)
            .Inc("success_requests", record.SuccessRequests)
            .Inc("failed_requests", record.FailedRequests)
            .Inc("total_billable_units", record.TotalBillableUnits)
            .Inc("total_amount", record.TotalAmount)
            .Set("currency", record.Currency)
            .Set("last_recomputed_at", record.LastRecomputedAtUtc)
            .SetOnInsert("locked_at", BsonNull.Value);

        await _monthlyBilling.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }
}

public class TranslationJobRecord
{
    public string RequestId { get; set; } = string.Empty;
    public int SellerUserId { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public int? AudioId { get; set; }
    public string SourceLanguageCode { get; set; } = string.Empty;
    public string TargetLanguageCode { get; set; } = string.Empty;
    public string SourceTextHash { get; set; } = string.Empty;
    public int SourceCharCount { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderEndpoint { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public int? LatencyMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class TranslationUsageLedgerRecord
{
    public string UsageEventId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public int SellerUserId { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public int? AudioId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string UnitType { get; set; } = "chars";
    public int InputChars { get; set; }
    public int OutputChars { get; set; }
    public decimal BillableUnits { get; set; }
    public string RateVersion { get; set; } = "v1";
    public decimal PricePer1KUnits { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal CostAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "billable";
    public string BillingMonth { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class AudioTranslationVersionRecord
{
    public int SellerUserId { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public int AudioId { get; set; }
    public string SourceLanguageCode { get; set; } = string.Empty;
    public string TargetLanguageCode { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string TranslatedTextHash { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public bool IsActive { get; set; }
    public string GenerationMethod { get; set; } = string.Empty;
    public string? JobId { get; set; }
    public string? UsageEventId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
}

public class MonthlyBillingSnapshotRecord
{
    public int SellerUserId { get; set; }
    public string BillingMonth { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessRequests { get; set; }
    public int FailedRequests { get; set; }
    public decimal TotalBillableUnits { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime LastRecomputedAtUtc { get; set; }
}
