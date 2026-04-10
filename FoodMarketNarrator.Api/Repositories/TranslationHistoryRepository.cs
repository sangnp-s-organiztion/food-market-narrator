using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;

namespace food_market_narrator_api.Repositories;

public class TranslationHistoryRepository
{
    private readonly IMongoCollection<BsonDocument> _translationJobs;
    private readonly IMongoCollection<BsonDocument> _usageLedger;
    private readonly IMongoCollection<BsonDocument> _audioUsageLedger;
    private readonly IMongoCollection<BsonDocument> _translationVersions;
    private readonly IMongoCollection<BsonDocument> _monthlyBilling;

    public TranslationHistoryRepository(IMongoDatabase mongoDatabase)
    {
        _translationJobs = mongoDatabase.GetCollection<BsonDocument>("TranslationJobs");
        _usageLedger = mongoDatabase.GetCollection<BsonDocument>("TranslationUsageLedger");
        _audioUsageLedger = mongoDatabase.GetCollection<BsonDocument>("AudioUsageLedger");
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
            { "billing_month", record.BillingMonth },
            { "created_at", record.CreatedAtUtc }
        };

        await _usageLedger.InsertOneAsync(doc);
    }

    public async Task InsertAudioUsageLedgerAsync(AudioUsageLedgerRecord record)
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
            { "billing_month", record.BillingMonth },
            { "created_at", record.CreatedAtUtc }
        };

        await _audioUsageLedger.InsertOneAsync(doc);
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

    public async Task<(List<MonthlyBillingSnapshotDocument> Items, long TotalCount)> GetMonthlyBillingAsync(
        string? billingMonth,
        int? sellerUserId,
        int page,
        int pageSize)
    {
        var filter = BuildMonthlyBillingFilter(billingMonth, sellerUserId);
        var totalCount = await _monthlyBilling.CountDocumentsAsync(filter);

        var docs = await _monthlyBilling
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort
                .Descending("billing_month")
                .Descending("total_amount"))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var items = docs.Select(doc => new MonthlyBillingSnapshotDocument
        {
            SellerUserId = ReadInt32(doc, "seller_user_id"),
            BillingMonth = ReadString(doc, "billing_month"),
            TotalRequests = ReadInt32(doc, "total_requests"),
            SuccessRequests = ReadInt32(doc, "success_requests"),
            FailedRequests = ReadInt32(doc, "failed_requests"),
            TotalBillableUnits = ReadDecimal(doc, "total_billable_units"),
            TotalAmount = ReadDecimal(doc, "total_amount"),
            Currency = ReadString(doc, "currency", "USD"),
            LastRecomputedAtUtc = ReadDateTime(doc, "last_recomputed_at")
        }).ToList();

        return (items, totalCount);
    }

    public async Task<MonthlyBillingAggregateSummary> GetMonthlyBillingSummaryAsync(string? billingMonth, int? sellerUserId)
    {
        var filter = BuildMonthlyBillingFilter(billingMonth, sellerUserId);

        var summaryDoc = await _monthlyBilling.Aggregate()
            .Match(filter)
            .Group(new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "total_requests", new BsonDocument("$sum", "$total_requests") },
                { "success_requests", new BsonDocument("$sum", "$success_requests") },
                { "failed_requests", new BsonDocument("$sum", "$failed_requests") },
                { "total_billable_units", new BsonDocument("$sum", "$total_billable_units") },
                { "total_amount", new BsonDocument("$sum", "$total_amount") }
            })
            .FirstOrDefaultAsync();

        var sellerCount = await _monthlyBilling.CountDocumentsAsync(filter);

        if (summaryDoc == null)
        {
            return new MonthlyBillingAggregateSummary { SellerCount = sellerCount };
        }

        return new MonthlyBillingAggregateSummary
        {
            SellerCount = sellerCount,
            TotalRequests = ReadInt32(summaryDoc, "total_requests"),
            SuccessRequests = ReadInt32(summaryDoc, "success_requests"),
            FailedRequests = ReadInt32(summaryDoc, "failed_requests"),
            TotalBillableUnits = ReadDecimal(summaryDoc, "total_billable_units"),
            TotalAmount = ReadDecimal(summaryDoc, "total_amount")
        };
    }

    public async Task<(List<TranslationUsageLedgerDocument> Items, long TotalCount)> GetUsageLedgerAsync(
        string? billingMonth,
        int? sellerUserId,
        int page,
        int pageSize)
    {
        var filter = BuildUsageLedgerFilter(billingMonth, sellerUserId);
        var totalCount = await _usageLedger.CountDocumentsAsync(filter);

        var docs = await _usageLedger
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("created_at"))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var items = docs.Select(doc => new TranslationUsageLedgerDocument
        {
            UsageEventId = ReadString(doc, "usage_event_id"),
            RequestId = ReadString(doc, "request_id"),
            SellerUserId = ReadInt32(doc, "seller_user_id"),
            RestaurantId = ReadString(doc, "restaurant_id"),
            AudioId = ReadNullableInt32(doc, "audio_id"),
            Provider = ReadString(doc, "provider"),
            ActionType = ReadString(doc, "action_type"),
            UnitType = ReadString(doc, "unit_type", "chars"),
            InputChars = ReadInt32(doc, "input_chars"),
            OutputChars = ReadInt32(doc, "output_chars"),
            BillableUnits = ReadDecimal(doc, "billable_units"),
            CostAmount = ReadDecimal(doc, "cost_amount"),
            TaxAmount = ReadDecimal(doc, "tax_amount"),
            TotalAmount = ReadDecimal(doc, "total_amount"),
            BillingMonth = ReadString(doc, "billing_month"),
            Currency = ReadNestedString(doc, "pricing_snapshot", "currency", "USD"),
            CreatedAtUtc = ReadDateTime(doc, "created_at")
        }).ToList();

        return (items, totalCount);
    }

    public async Task<(List<AudioUsageLedgerDocument> Items, long TotalCount)> GetAudioUsageLedgerAsync(
        string? billingMonth,
        int? sellerUserId,
        int page,
        int pageSize)
    {
        var filter = BuildAudioUsageLedgerFilter(billingMonth, sellerUserId);
        var totalCount = await _audioUsageLedger.CountDocumentsAsync(filter);

        var docs = await _audioUsageLedger
            .Find(filter)
            .Sort(Builders<BsonDocument>.Sort
                .Descending("created_at")
                .Descending("usage_event_id"))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var items = docs.Select(doc => new AudioUsageLedgerDocument
        {
            UsageEventId = ReadString(doc, "usage_event_id"),
            RequestId = ReadString(doc, "request_id"),
            SellerUserId = ReadInt32(doc, "seller_user_id"),
            RestaurantId = ReadString(doc, "restaurant_id"),
            AudioId = ReadNullableInt32(doc, "audio_id"),
            Provider = ReadString(doc, "provider"),
            ActionType = ReadString(doc, "action_type"),
            UnitType = ReadString(doc, "unit_type", "chars"),
            InputChars = ReadInt32(doc, "input_chars"),
            OutputChars = ReadInt32(doc, "output_chars"),
            BillableUnits = ReadDecimal(doc, "billable_units"),
            BillingMonth = ReadString(doc, "billing_month"),
            CreatedAtUtc = ReadDateTime(doc, "created_at")
        }).ToList();

        return (items, totalCount);
    }

    public async Task<AudioUsageLedgerAggregateSummary> GetAudioUsageLedgerSummaryAsync(
        string? billingMonth,
        int? sellerUserId)
    {
        var filter = BuildAudioUsageLedgerFilter(billingMonth, sellerUserId);

        var summaryDoc = await _audioUsageLedger.Aggregate()
            .Match(filter)
            .Group(new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "event_count", new BsonDocument("$sum", 1) },
                { "total_billable_units", new BsonDocument("$sum", "$billable_units") }
            })
            .FirstOrDefaultAsync();

        if (summaryDoc == null)
        {
            return new AudioUsageLedgerAggregateSummary();
        }

        return new AudioUsageLedgerAggregateSummary
        {
            EventCount = ReadInt32(summaryDoc, "event_count"),
            TotalBillableUnits = ReadDecimal(summaryDoc, "total_billable_units")
        };
    }

    public async Task<UsageLedgerAggregateSummary> GetUsageLedgerSummaryAsync(string? billingMonth, int? sellerUserId)
    {
        var filter = BuildUsageLedgerFilter(billingMonth, sellerUserId);

        var summaryDoc = await _usageLedger.Aggregate()
            .Match(filter)
            .Group(new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "event_count", new BsonDocument("$sum", 1) },
                { "total_billable_units", new BsonDocument("$sum", "$billable_units") },
                { "total_amount", new BsonDocument("$sum", "$total_amount") }
            })
            .FirstOrDefaultAsync();

        if (summaryDoc == null)
        {
            return new UsageLedgerAggregateSummary();
        }

        return new UsageLedgerAggregateSummary
        {
            EventCount = ReadInt32(summaryDoc, "event_count"),
            TotalBillableUnits = ReadDecimal(summaryDoc, "total_billable_units"),
            TotalAmount = ReadDecimal(summaryDoc, "total_amount")
        };
    }

    private static FilterDefinition<BsonDocument> BuildMonthlyBillingFilter(string? billingMonth, int? sellerUserId)
    {
        var filters = new List<FilterDefinition<BsonDocument>>();

        if (!string.IsNullOrWhiteSpace(billingMonth))
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("billing_month", billingMonth.Trim()));
        }

        if (sellerUserId.HasValue)
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("seller_user_id", sellerUserId.Value));
        }

        return filters.Count == 0
            ? Builders<BsonDocument>.Filter.Empty
            : Builders<BsonDocument>.Filter.And(filters);
    }

    private static FilterDefinition<BsonDocument> BuildAudioUsageLedgerFilter(string? billingMonth, int? sellerUserId)
    {
        var filters = new List<FilterDefinition<BsonDocument>>();

        if (!string.IsNullOrWhiteSpace(billingMonth))
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("billing_month", billingMonth));
        }

        if (sellerUserId.HasValue)
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("seller_user_id", sellerUserId.Value));
        }

        return filters.Count == 0
            ? Builders<BsonDocument>.Filter.Empty
            : Builders<BsonDocument>.Filter.And(filters);
    }

    private static FilterDefinition<BsonDocument> BuildUsageLedgerFilter(string? billingMonth, int? sellerUserId)
    {
        var filters = new List<FilterDefinition<BsonDocument>>();

        if (!string.IsNullOrWhiteSpace(billingMonth))
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("billing_month", billingMonth.Trim()));
        }

        if (sellerUserId.HasValue)
        {
            filters.Add(Builders<BsonDocument>.Filter.Eq("seller_user_id", sellerUserId.Value));
        }

        return filters.Count == 0
            ? Builders<BsonDocument>.Filter.Empty
            : Builders<BsonDocument>.Filter.And(filters);
    }

    private static string ReadString(BsonDocument doc, string field, string defaultValue = "")
    {
        if (!doc.TryGetValue(field, out var value) || value.IsBsonNull)
        {
            return defaultValue;
        }

        return value.ToString() ?? defaultValue;
    }

    private static string ReadNestedString(BsonDocument doc, string parentField, string childField, string defaultValue = "")
    {
        if (!doc.TryGetValue(parentField, out var parent) || !parent.IsBsonDocument)
        {
            return defaultValue;
        }

        return ReadString(parent.AsBsonDocument, childField, defaultValue);
    }

    private static int ReadInt32(BsonDocument doc, string field)
    {
        if (!doc.TryGetValue(field, out var value) || value.IsBsonNull)
        {
            return 0;
        }

        return value.BsonType switch
        {
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => (int)value.AsInt64,
            BsonType.Double => (int)value.AsDouble,
            BsonType.Decimal128 => (int)value.AsDecimal128,
            _ => int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0
        };
    }

    private static int? ReadNullableInt32(BsonDocument doc, string field)
    {
        if (!doc.TryGetValue(field, out var value) || value.IsBsonNull)
        {
            return null;
        }

        return ReadInt32(doc, field);
    }

    private static decimal ReadDecimal(BsonDocument doc, string field)
    {
        if (!doc.TryGetValue(field, out var value) || value.IsBsonNull)
        {
            return 0m;
        }

        return value.BsonType switch
        {
            BsonType.Decimal128 => Decimal128.ToDecimal(value.AsDecimal128),
            BsonType.Double => (decimal)value.AsDouble,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            _ => decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m
        };
    }

    private static DateTime ReadDateTime(BsonDocument doc, string field)
    {
        if (!doc.TryGetValue(field, out var value) || value.IsBsonNull)
        {
            return DateTime.MinValue;
        }

        return value.BsonType switch
        {
            BsonType.DateTime => value.ToUniversalTime(),
            _ => DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsed)
                ? parsed
                : DateTime.MinValue
        };
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
    public string BillingMonth { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class AudioUsageLedgerRecord
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

public class MonthlyBillingSnapshotDocument
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

public class MonthlyBillingAggregateSummary
{
    public long SellerCount { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessRequests { get; set; }
    public int FailedRequests { get; set; }
    public decimal TotalBillableUnits { get; set; }
    public decimal TotalAmount { get; set; }
}

public class TranslationUsageLedgerDocument
{
    public string UsageEventId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int SellerUserId { get; set; }
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
    public string BillingMonth { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; }
}

public class AudioUsageLedgerDocument
{
    public string UsageEventId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public int SellerUserId { get; set; }
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

public class AudioUsageLedgerAggregateSummary
{
    public int EventCount { get; set; }
    public decimal TotalBillableUnits { get; set; }
}

public class UsageLedgerAggregateSummary
{
    public int EventCount { get; set; }
    public decimal TotalBillableUnits { get; set; }
    public decimal TotalAmount { get; set; }
}
