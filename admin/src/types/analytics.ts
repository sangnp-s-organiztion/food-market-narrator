// ─── Analytics Response Types ─────────────────────────────────────────────────

export interface KpiResponse {
  totalUsers: number;
  averageListeningTimeSeconds: number;
  averageListeningTimeFormatted: string;
  totalPoiPlays: number;
}

export interface HeatmapPoint {
  longitude: number;
  latitude: number;
}

export interface HeatmapResponse {
  points: HeatmapPoint[];
  count: number;
}

export interface TopAudio {
  audioId: number;
  audioUrl: string | null;
  restaurantId: string | null;
  restaurantName: string | null;
  languageName: string | null;
  playCount: number;
  averageDurationSeconds: number;
  averageDurationFormatted: string;
}

export interface TopAudiosResponse {
  items: TopAudio[];
  totalCount: number;
}

export interface TopRestaurant {
  restaurantId: string;
  restaurantName: string;
  playCount: number;
  averageDurationSeconds: number;
  averageDurationFormatted: string;
}

export interface TopRestaurantsResponse {
  items: TopRestaurant[];
  totalCount: number;
}

export interface MovementPoint {
  longitude: number;
  latitude: number;
  timestamp: string; // ISO 8601
}

export interface MovementPath {
  sessionId: string;
  points: MovementPoint[];
}

export interface MovementPathsResponse {
  sessions: MovementPath[];
  totalSessions: number;
}

export interface RecentActivity {
  audioId: number;
  restaurantId: string;
  restaurantName: string | null;
  duration: number;
  timestamp: string; // ISO 8601
}

export interface RecentActivityResponse {
  items: RecentActivity[];
  count: number;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ─── Dashboard Chart Types ───────────────────────────────────────────────────

export interface DailyListenData {
  date: string; // "dd/MM" e.g. "12/04"
  listens: number;
}

export interface TopRestaurantChartData {
  name: string;
  listens: number;
}
