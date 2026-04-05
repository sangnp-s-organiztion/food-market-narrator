import type {
  KpiResponse,
  HeatmapResponse,
  TopAudiosResponse,
  TopRestaurantsResponse,
  MovementPathsResponse,
  RecentActivityResponse,
  TopAudio,
} from "@/types/analytics";

// ─── Base ───────────────────────────────────────────────────────────────────

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5044";

async function analyticsFetch<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: "include", // send cookie auth
  });

  if (!res.ok) {
    throw new Error(`Analytics API error ${res.status}: ${res.statusText}`);
  }

  return res.json() as Promise<T>;
}

// ─── KPIs ────────────────────────────────────────────────────────────────────

export const analyticsApi = {
  /**
   * GET /api/analytics/kpis
   * Returns: total sessions, avg listening time, total valid plays
   */
  async getKpis(): Promise<KpiResponse> {
    return analyticsFetch<KpiResponse>("/api/analytics/kpis");
  },

  /**
   * GET /api/analytics/heatmap
   * @param hoursOrAll - lookback window in hours, or "all" for no time limit
   */
  async getHeatmap(hoursOrAll: number | "all" = 24): Promise<HeatmapResponse> {
    const query =
      hoursOrAll === "all"
        ? "/api/analytics/heatmap?all=true"
        : `/api/analytics/heatmap?hours=${hoursOrAll}`;
    return analyticsFetch<HeatmapResponse>(query);
  },

  /**
   * GET /api/analytics/top-audios?limit=10
   * Returns top N most-listened audios with play count, avg duration,
   * restaurant name, and language name (enriched from MSSQL).
   */
  async getTopAudios(limit = 10): Promise<TopAudiosResponse> {
    return analyticsFetch<TopAudiosResponse>(
      `/api/analytics/top-audios?limit=${limit}`,
    );
  },

  /**
   * GET /api/analytics/top-restaurants?limit=10
   * Returns top N restaurants by play count with avg duration.
   */
  async getTopRestaurants(limit = 10): Promise<TopRestaurantsResponse> {
    return analyticsFetch<TopRestaurantsResponse>(
      `/api/analytics/top-restaurants?limit=${limit}`,
    );
  },

  /**
   * GET /api/analytics/movement-paths?sessionLimit=100
   * Returns anonymous GPS paths (ordered per session).
   * @param sessionLimit - max sessions to return (default 100, max 500, "all" = no limit)
   */
  async getMovementPaths(
    sessionLimit: number | "all" = 100,
  ): Promise<MovementPathsResponse> {
    const normalizedLimit = sessionLimit === "all" ? 0 : sessionLimit;
    return analyticsFetch<MovementPathsResponse>(
      `/api/analytics/movement-paths?sessionLimit=${normalizedLimit}`,
    );
  },

  /**
   * GET /api/analytics/recent-activity?page=1&pageSize=10
   * Returns paginated recent valid audio plays with restaurant names.
   */
  async getRecentActivity(
    page = 1,
    pageSize = 10,
  ): Promise<RecentActivityResponse> {
    return analyticsFetch<RecentActivityResponse>(
      `/api/analytics/recent-activity?page=${page}&pageSize=${pageSize}`,
    );
  },

  /**
   * GET /api/analytics/audio-stats
   * Full audio stats table (all audios with plays, no top-N limit).
   */
  async getAudioStats(): Promise<TopAudio[]> {
    return analyticsFetch<TopAudio[]>("/api/analytics/audio-stats");
  },
};
