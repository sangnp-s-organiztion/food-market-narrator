using food_market_narrator_api.DTOs.Analytics;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Services;

public class AnalyticsService
{
    private readonly AnalyticsRepository _analyticsRepository;
    private readonly AppDbContext _dbContext;

    public AnalyticsService(AnalyticsRepository analyticsRepository, AppDbContext dbContext)
    {
        _analyticsRepository = analyticsRepository;
        _dbContext = dbContext;
    }

    // ─── KPI Dashboard ─────────────────────────────────────────────────────────
    public async Task<KpiResponse> GetKpisAsync()
    {
        var totalUsers = await _analyticsRepository.GetTotalSessionCountAsync();
        var avgDuration = await _analyticsRepository.GetAverageListeningTimeAsync();
        var totalPlays = await _analyticsRepository.GetTotalPlayCountAsync();

        return new KpiResponse
        {
            TotalUsers = (int)totalUsers,
            AverageListeningTimeSeconds = Math.Round(avgDuration, 2),
            AverageListeningTimeFormatted = FormatDuration(avgDuration),
            TotalPoiPlays = (int)totalPlays
        };
    }

    public async Task<KpiResponse> GetRestaurantKpisAsync(string restaurantId)
    {
        var totalUsers = await _analyticsRepository.GetTotalSessionCountByRestaurantAsync(restaurantId);
        var avgDuration = await _analyticsRepository.GetAverageListeningTimeByRestaurantAsync(restaurantId);
        var totalPlays = await _analyticsRepository.GetTotalPlayCountByRestaurantAsync(restaurantId);

        return new KpiResponse
        {
            TotalUsers = (int)totalUsers,
            AverageListeningTimeSeconds = Math.Round(avgDuration, 2),
            AverageListeningTimeFormatted = FormatDuration(avgDuration),
            TotalPoiPlays = (int)totalPlays
        };
    }

    // ─── Heatmap Points (hours window or all-time when null) ──────────────────
    public async Task<HeatmapResponse> GetHeatmapAsync(int? hours = 24)
    {
        var points = await _analyticsRepository.GetHeatmapPointsAsync(hours);

        return new HeatmapResponse
        {
            Points = points.Select(p => new HeatmapPointDto
            {
                Longitude = p.Longitude,
                Latitude = p.Latitude
            }).ToList(),
            Count = points.Count
        };
    }

    // ─── Top Audios Ranking ────────────────────────────────────────────────────
    public async Task<TopAudiosResponse> GetTopAudiosAsync(int limit = 10)
    {
        var audioStats = await _analyticsRepository.GetAudioStatsAsync();

        if (!audioStats.Any())
            return new TopAudiosResponse { Items = [], TotalCount = 0 };

        // Fetch audio details from MSSQL
        var audioIds = audioStats.Select(a => a.AudioId).ToList();
        var audioRecords = await _dbContext.Audio
            .Include(a => a.Restaurant)
            .Include(a => a.Language)
            .Where(a => audioIds.Contains(a.AudioId))
            .ToListAsync();

        var restaurantMap = audioRecords.ToDictionary(
            a => a.AudioId,
            a => new { a.Restaurant?.Name, a.AudioUrl, LanguageName = a.Language?.LanguageName }
        );

        var items = audioStats.Take(limit).Select(stats =>
        {
            restaurantMap.TryGetValue(stats.AudioId, out var meta);
            return new TopAudioDto
            {
                AudioId = stats.AudioId,
                AudioUrl = meta?.AudioUrl,
                RestaurantId = audioRecords.FirstOrDefault(a => a.AudioId == stats.AudioId)?.RestaurantId,
                RestaurantName = meta?.Name,
                LanguageName = meta?.LanguageName,
                PlayCount = stats.PlayCount,
                AverageDurationSeconds = stats.AverageDurationSeconds,
                AverageDurationFormatted = FormatDuration(stats.AverageDurationSeconds)
            };
        }).ToList();

        return new TopAudiosResponse
        {
            Items = items,
            TotalCount = items.Count
        };
    }

    // ─── Top Restaurants by Plays ──────────────────────────────────────────────
    public async Task<TopRestaurantsResponse> GetTopRestaurantsAsync(int limit = 10)
    {
        var restaurantStats = await _analyticsRepository.GetRestaurantStatsAsync();

        if (!restaurantStats.Any())
            return new TopRestaurantsResponse { Items = [], TotalCount = 0 };

        // Fetch restaurant names from MSSQL
        var restaurantIds = restaurantStats.Select(r => r.RestaurantId).ToList();
        var restaurantList = await _dbContext.Restaurant
            .Where(r => restaurantIds.Contains(r.RestaurantId))
            .ToListAsync();
        var restaurants = restaurantList.ToDictionary(r => r.RestaurantId, r => r.Name);

        var items = restaurantStats.Take(limit).Select(stats => new TopRestaurantDto
        {
            RestaurantId = stats.RestaurantId,
            RestaurantName = restaurants.GetValueOrDefault(stats.RestaurantId, "Unknown"),
            PlayCount = stats.PlayCount,
            AverageDurationSeconds = stats.AverageDurationSeconds,
            AverageDurationFormatted = FormatDuration(stats.AverageDurationSeconds)
        }).ToList();

        return new TopRestaurantsResponse
        {
            Items = items,
            TotalCount = items.Count
        };
    }

    // ─── Anonymous Movement Paths ──────────────────────────────────────────────
    public async Task<MovementPathsResponse> GetMovementPathsAsync(int? sessionLimit = 100)
    {
        var paths = await _analyticsRepository.GetMovementPathsAsync(sessionLimit);

        return new MovementPathsResponse
        {
            Sessions = paths.Select(p => new MovementPathDto
            {
                SessionId = p.SessionId,
                Points = p.Points.Select(pt => new MovementPointDto
                {
                    Longitude = pt.Longitude,
                    Latitude = pt.Latitude,
                    Timestamp = pt.Timestamp
                }).ToList()
            }).ToList(),
            TotalSessions = paths.Count
        };
    }

    // ─── Recent Activity Feed ──────────────────────────────────────────────────
    public async Task<RecentActivityResponse> GetRecentActivityAsync(int page = 1, int pageSize = 10)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);

        var paged = await _analyticsRepository.GetRecentActivityAsync(safePage, safePageSize);
        var activities = paged.Items;
        var totalCount = paged.TotalCount;

        if (!activities.Any())
            return new RecentActivityResponse
            {
                Items = [],
                Count = 0,
                Page = safePage,
                PageSize = safePageSize,
                TotalCount = totalCount,
                TotalPages = 0
            };

        // Enrich with restaurant names from MSSQL
        var restaurantIds = activities.Select(a => a.RestaurantId).Distinct().ToList();
        var restaurantList = await _dbContext.Restaurant
            .Where(r => restaurantIds.Contains(r.RestaurantId))
            .ToListAsync();
        var restaurants = restaurantList.ToDictionary(r => r.RestaurantId, r => r.Name);

        var items = activities.Select(a => new RecentActivityDto
        {
            AudioId = a.AudioId,
            RestaurantId = a.RestaurantId,
            RestaurantName = restaurants.GetValueOrDefault(a.RestaurantId),
            Duration = a.Duration,
            Timestamp = a.Timestamp
        }).ToList();

        return new RecentActivityResponse
        {
            Items = items,
            Count = items.Count,
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = totalCount,
            TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)safePageSize) : 0
        };
    }

    // ─── Avg Listening Time per Audio (full list, not top-N) ───────────────────
    public async Task<List<TopAudioDto>> GetAllAudioStatsAsync()
    {
        var audioStats = await _analyticsRepository.GetAudioStatsAsync();

        if (!audioStats.Any())
            return [];

        var audioIds = audioStats.Select(a => a.AudioId).ToList();
        var audioRecords = await _dbContext.Audio
            .Include(a => a.Restaurant)
            .Include(a => a.Language)
            .Where(a => audioIds.Contains(a.AudioId))
            .ToListAsync();

        var restaurantMap = audioRecords.ToDictionary(
            a => a.AudioId,
            a => new { a.Restaurant?.Name, a.AudioUrl, LanguageName = a.Language?.LanguageName, a.RestaurantId }
        );

        return audioStats.Select(stats =>
        {
            restaurantMap.TryGetValue(stats.AudioId, out var meta);
            return new TopAudioDto
            {
                AudioId = stats.AudioId,
                AudioUrl = meta?.AudioUrl,
                RestaurantId = meta?.RestaurantId,
                RestaurantName = meta?.Name,
                LanguageName = meta?.LanguageName,
                PlayCount = stats.PlayCount,
                AverageDurationSeconds = stats.AverageDurationSeconds,
                AverageDurationFormatted = FormatDuration(stats.AverageDurationSeconds)
            };
        }).ToList();
    }

    // ─── Entity Counts ─────────────────────────────────────────────────────────
    public async Task<EntityCountsResponse> GetEntityCountsAsync()
    {
        var totalRestaurants = await _dbContext.Restaurant.CountAsync();
        var totalAudios = await _dbContext.Audio.CountAsync();
        var totalUsers = await _dbContext.User.CountAsync();
        var totalDishes = await _dbContext.Dish.CountAsync();

        return new EntityCountsResponse
        {
            TotalRestaurants = totalRestaurants,
            TotalAudios = totalAudios,
            TotalUsers = totalUsers,
            TotalDishes = totalDishes
        };
    }

    // ─── Listens Timeseries ────────────────────────────────────────────────────
    public async Task<ListensTimeseriesResponse> GetListensTimeseriesAsync(int days = 14)
    {
        var clampedDays = Math.Clamp(days, 1, 90);
        var dailyCounts = await _analyticsRepository.GetDailyListenCountsAsync(clampedDays);

        var result = new List<ListenCountItem>();
        var today = DateTime.UtcNow.Date;
        for (int i = clampedDays - 1; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dateStr = date.ToString("yyyy-MM-dd");
            var found = dailyCounts.FirstOrDefault(d => d.Date == dateStr);
            result.Add(new ListenCountItem
            {
                Date = dateStr,
                Listens = found?.Count ?? 0
            });
        }

        return new ListensTimeseriesResponse { Items = result };
    }

    // ─── Helper ───────────────────────────────────────────────────────────────
    private static string FormatDuration(double seconds)
    {
        if (seconds <= 0) return "0s";

        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}
