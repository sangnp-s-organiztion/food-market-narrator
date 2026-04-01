using food_market_narrator_api.DTOs.Analytics;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsService _analyticsService;

    public AnalyticsController(AnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// KPI Dashboard: Total Users, Avg Listening Time, Total POI Plays.
    /// All valid listens (duration >= 5 seconds) are included.
    /// </summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis()
    {
        var kpis = await _analyticsService.GetKpisAsync();
        return Ok(kpis);
    }

    /// <summary>
    /// GeoJSON heatmap points from LocationLogs (last 24h by default).
    /// Query param: hours (int, default 24, max 720 / 30 days).
    /// </summary>
    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap([FromQuery] int hours = 24)
    {
        var clamped = Math.Clamp(hours, 1, 720);
        var heatmap = await _analyticsService.GetHeatmapAsync(clamped);
        return Ok(heatmap);
    }

    /// <summary>
    /// Top N most-listened audios (valid plays only, duration >= 5s).
    /// MongoDB aggregation group by audio_id, joined with MSSQL Audio table.
    /// Query param: limit (int, default 10, max 100).
    /// </summary>
    [HttpGet("top-audios")]
    public async Task<IActionResult> GetTopAudios([FromQuery] int limit = 10)
    {
        var clamped = Math.Clamp(limit, 1, 100);
        var result = await _analyticsService.GetTopAudiosAsync(clamped);
        return Ok(result);
    }

    /// <summary>
    /// Average listening time per audio (full list, all audios with plays).
    /// Useful for admin export / data tables.
    /// </summary>
    [HttpGet("audio-stats")]
    public async Task<IActionResult> GetAudioStats()
    {
        var stats = await _analyticsService.GetAllAudioStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Top N restaurants by play count (valid plays only, duration >= 5s).
    /// MongoDB aggregation group by restaurant_id, joined with MSSQL Restaurant table.
    /// Query param: limit (int, default 10, max 100).
    /// </summary>
    [HttpGet("top-restaurants")]
    public async Task<IActionResult> GetTopRestaurants([FromQuery] int limit = 10)
    {
        var clamped = Math.Clamp(limit, 1, 100);
        var result = await _analyticsService.GetTopRestaurantsAsync(clamped);
        return Ok(result);
    }

    /// <summary>
    /// Anonymous movement paths: ordered GPS coordinates per session.
    /// Returns last N sessions ordered by most recent activity.
    /// Query params: sessionLimit (int, default 100, max 500).
    /// </summary>
    [HttpGet("movement-paths")]
    public async Task<IActionResult> GetMovementPaths([FromQuery] int sessionLimit = 100)
    {
        var clamped = Math.Clamp(sessionLimit, 1, 500);
        var result = await _analyticsService.GetMovementPathsAsync(clamped);
        return Ok(result);
    }

    /// <summary>
    /// Recent activity feed from AudioLogs (valid plays only, duration >= 5s).
    /// Sorted by timestamp DESC. Returns restaurant names via MSSQL join.
    /// Query param: limit (int, default 20, max 100).
    /// </summary>
    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 20)
    {
        var clamped = Math.Clamp(limit, 1, 100);
        var result = await _analyticsService.GetRecentActivityAsync(clamped);
        return Ok(result);
    }
}
