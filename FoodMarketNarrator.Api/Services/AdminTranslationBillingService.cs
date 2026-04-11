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
        string? sellerUsername,
        int page,
        int pageSize)
    {
        var normalizedMonth = NormalizeBillingMonthOrThrow(billingMonth);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var resolvedSellerUserId = await ResolveSellerUserIdByUsernameAsync(sellerUsername);

        var (items, totalCount) = await _translationHistoryRepository.GetMonthlyBillingAsync(
            normalizedMonth,
            resolvedSellerUserId,
            normalizedPage,
            normalizedPageSize);

        var sellerIdsInPage = items
            .Select(x => x.SellerUserId)
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var audioBillableBySellerMonth = await _translationHistoryRepository.GetAudioBillableUnitsBySellerMonthAsync(
            normalizedMonth,
            resolvedSellerUserId,
            sellerIdsInPage);

        var summary = await _translationHistoryRepository.GetMonthlyBillingSummaryAsync(normalizedMonth, resolvedSellerUserId);
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
                TranslationBillableUnits = x.TotalBillableUnits,
                AudioBillableUnits = audioBillableBySellerMonth.TryGetValue((x.SellerUserId, x.BillingMonth), out var audioUnits)
                    ? audioUnits
                    : 0m,
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
        string? sellerUsername,
        int page,
        int pageSize)
    {
        var normalizedMonth = NormalizeBillingMonthOrThrow(billingMonth);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var resolvedSellerUserId = await ResolveSellerUserIdByUsernameAsync(sellerUsername);

        return await BuildUsageLedgerResponseAsync(
            normalizedMonth,
            resolvedSellerUserId,
            normalizedPage,
            normalizedPageSize);
    }

    public async Task<TranslationUsageLedgerListResponse> GetUsageLedgerBySellerUserIdAsync(
        string? billingMonth,
        int sellerUserId,
        int page,
        int pageSize)
    {
        if (sellerUserId <= 0)
        {
            throw new ArgumentException("sellerUserId must be greater than zero.");
        }

        var normalizedMonth = NormalizeBillingMonthOrThrow(billingMonth);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        return await BuildUsageLedgerResponseAsync(
            normalizedMonth,
            sellerUserId,
            normalizedPage,
            normalizedPageSize);
    }

    private async Task<TranslationUsageLedgerListResponse> BuildUsageLedgerResponseAsync(
        string? normalizedMonth,
        int? resolvedSellerUserId,
        int normalizedPage,
        int normalizedPageSize)
    {

        var (items, totalCount) = await _translationHistoryRepository.GetUsageLedgerAsync(
            normalizedMonth,
            resolvedSellerUserId,
            normalizedPage,
            normalizedPageSize);

        var summary = await _translationHistoryRepository.GetUsageLedgerSummaryAsync(normalizedMonth, resolvedSellerUserId);
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
                BillingMonth = x.BillingMonth,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList(),
            TotalCount = totalCount,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Summary = new TranslationUsageLedgerSummaryResponse
            {
                BillingMonth = normalizedMonth ?? "all",
                EventCount = summary.EventCount,
                TotalBillableUnits = summary.TotalBillableUnits,
                TotalAmount = summary.TotalAmount,
                Currency = currency
            }
        };
    }

    public async Task<AudioUsageLedgerListResponse> GetAudioUsageLedgerAsync(
        string? billingMonth,
        string? sellerUsername,
        int page,
        int pageSize)
    {
        var normalizedMonth = NormalizeBillingMonthOrThrow(billingMonth);
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var resolvedSellerUserId = await ResolveSellerUserIdByUsernameAsync(sellerUsername);

        var (items, totalCount) = await _translationHistoryRepository.GetAudioUsageLedgerAsync(
            normalizedMonth,
            resolvedSellerUserId,
            normalizedPage,
            normalizedPageSize);

        var summary = await _translationHistoryRepository.GetAudioUsageLedgerSummaryAsync(normalizedMonth, resolvedSellerUserId);
        var usernames = await ResolveUsernamesAsync(items.Select(x => x.SellerUserId));

        return new AudioUsageLedgerListResponse
        {
            Items = items.Select(x => new AudioUsageLedgerItemResponse
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
                BillingMonth = x.BillingMonth,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList(),
            TotalCount = totalCount,
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            Summary = new AudioUsageLedgerSummaryResponse
            {
                BillingMonth = normalizedMonth ?? "all",
                EventCount = summary.EventCount,
                TotalBillableUnits = summary.TotalBillableUnits
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

    private async Task<int?> ResolveSellerUserIdByUsernameAsync(string? sellerUsername)
    {
        if (string.IsNullOrWhiteSpace(sellerUsername))
        {
            return null;
        }

        var normalizedUsername = sellerUsername.Trim();
        var user = await _userRepository.GetByUsernameAsync(normalizedUsername);
        return user?.UserId ?? -1;
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

}
