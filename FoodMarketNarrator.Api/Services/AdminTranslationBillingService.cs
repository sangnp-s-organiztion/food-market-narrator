using System.Text.RegularExpressions;
using food_market_narrator_api.DTOs.Admin;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services;

public class AdminTranslationBillingService
{
    private static readonly Regex BillingMonthRegex = new("^\\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    private readonly TranslationHistoryRepository _translationHistoryRepository;
    private readonly UserRepository _userRepository;

    public AdminTranslationBillingService(
        TranslationHistoryRepository translationHistoryRepository,
        UserRepository userRepository)
    {
        _translationHistoryRepository = translationHistoryRepository;
        _userRepository = userRepository;
    }

    public async Task<TranslationMonthlyBillingListResponse> GetMonthlyBillingAsync(
        string? billingMonth,
        int? sellerUserId,
        int page,
        int pageSize)
    {
        var normalizedMonth = NormalizeBillingMonthOrThrow(billingMonth);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _translationHistoryRepository.GetMonthlyBillingAsync(
            normalizedMonth,
            sellerUserId,
            normalizedPage,
            normalizedPageSize);

        var summary = await _translationHistoryRepository.GetMonthlyBillingSummaryAsync(normalizedMonth, sellerUserId);
        var usernames = await ResolveUsernamesAsync(items.Select(x => x.SellerUserId));

        var currency = items.FirstOrDefault()?.Currency ?? "USD";

        return new TranslationMonthlyBillingListResponse
        {
            Items = items.Select(x => new TranslationMonthlyBillingItemResponse
            {
                SellerUserId = x.SellerUserId,
                SellerUsername = usernames.TryGetValue(x.SellerUserId, out var name) ? name : string.Empty,
                BillingMonth = x.BillingMonth,
                TotalRequests = x.TotalRequests,
                SuccessRequests = x.SuccessRequests,
                FailedRequests = x.FailedRequests,
                TotalBillableUnits = x.TotalBillableUnits,
                TotalAmount = x.TotalAmount,
                Currency = x.Currency,
                LastRecomputedAtUtc = x.LastRecomputedAtUtc
            }).ToList(),
            TotalCount = totalCount,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Summary = new TranslationMonthlyBillingSummaryResponse
            {
                BillingMonth = normalizedMonth ?? "all",
                SellerCount = summary.SellerCount,
                TotalRequests = summary.TotalRequests,
                SuccessRequests = summary.SuccessRequests,
                FailedRequests = summary.FailedRequests,
                TotalBillableUnits = summary.TotalBillableUnits,
                TotalAmount = summary.TotalAmount,
                Currency = currency
            }
        };
    }

    public async Task<TranslationUsageLedgerListResponse> GetUsageLedgerAsync(
        string? billingMonth,
        int? sellerUserId,
        string? status,
        int page,
        int pageSize)
    {
        var normalizedMonth = NormalizeBillingMonthOrThrow(billingMonth);
        var normalizedStatus = NormalizeStatus(status);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _translationHistoryRepository.GetUsageLedgerAsync(
            normalizedMonth,
            sellerUserId,
            normalizedStatus,
            normalizedPage,
            normalizedPageSize);

        var summary = await _translationHistoryRepository.GetUsageLedgerSummaryAsync(normalizedMonth, sellerUserId, normalizedStatus);
        var usernames = await ResolveUsernamesAsync(items.Select(x => x.SellerUserId));

        var currency = items.FirstOrDefault()?.Currency ?? "USD";

        return new TranslationUsageLedgerListResponse
        {
            Items = items.Select(x => new TranslationUsageLedgerItemResponse
            {
                UsageEventId = x.UsageEventId,
                RequestId = x.RequestId,
                SellerUserId = x.SellerUserId,
                SellerUsername = usernames.TryGetValue(x.SellerUserId, out var name) ? name : string.Empty,
                RestaurantId = x.RestaurantId,
                AudioId = x.AudioId,
                Provider = x.Provider,
                ActionType = x.ActionType,
                UnitType = x.UnitType,
                InputChars = x.InputChars,
                OutputChars = x.OutputChars,
                BillableUnits = x.BillableUnits,
                CostAmount = x.CostAmount,
                TaxAmount = x.TaxAmount,
                TotalAmount = x.TotalAmount,
                Currency = x.Currency,
                Status = x.Status,
                BillingMonth = x.BillingMonth,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList(),
            TotalCount = totalCount,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Summary = new TranslationUsageLedgerSummaryResponse
            {
                BillingMonth = normalizedMonth ?? "all",
                Status = normalizedStatus ?? "all",
                EventCount = summary.EventCount,
                TotalBillableUnits = summary.TotalBillableUnits,
                TotalAmount = summary.TotalAmount,
                Currency = currency
            }
        };
    }

    private async Task<Dictionary<int, string>> ResolveUsernamesAsync(IEnumerable<int> sellerUserIds)
    {
        var ids = sellerUserIds.Where(x => x > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var users = await _userRepository.GetByIdsAsync(ids);
        return users.ToDictionary(x => x.UserId, x => x.Username);
    }

    private static string? NormalizeBillingMonthOrThrow(string? billingMonth)
    {
        if (string.IsNullOrWhiteSpace(billingMonth))
        {
            return null;
        }

        var normalized = billingMonth.Trim();
        if (!BillingMonthRegex.IsMatch(normalized))
        {
            throw new ArgumentException("billingMonth must follow yyyy-MM format.");
        }

        return normalized;
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "billable" => "billable",
            "failed" => "failed",
            _ => throw new ArgumentException("status must be 'billable' or 'failed'.")
        };
    }
}
