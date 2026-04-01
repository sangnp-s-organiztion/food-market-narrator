import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import {
  Store,
  Headphones,
  Users,
  UtensilsCrossed,
  TrendingUp,
  ArrowUp,
} from "lucide-react";
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
} from "recharts";
import { HeatmapSection } from "@/components/HeatmapSection";
import { analyticsApi } from "@/lib/analyticsApi";
import { adminStatsApi } from "@/lib/adminApi";

// ─── Derived chart helpers ────────────────────────────────────────────────────

function formatMinutesSeconds(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = Math.round(seconds % 60);
  return m > 0
    ? `${m}:${String(s).padStart(2, "0")}`
    : `0:${String(s).padStart(2, "0")}`;
}

// Map TopRestaurants → recharts bar-chart shape
function toBarChartData(
  items: { restaurantName: string; playCount: number }[],
) {
  return items.map((r) => ({ name: r.restaurantName, listens: r.playCount }));
}

// ─── Dashboard Page ───────────────────────────────────────────────────────────

const Dashboard = () => {
  const [heatmapHours, setHeatmapHours] = useState<1 | 6 | 24 | 168>(24);

  const { data: restaurantCount } = useQuery({
    queryKey: ["admin-stats", "restaurants", "count"],
    queryFn: () => adminStatsApi.getRestaurantCount(),
    staleTime: 60_000,
  });

  const { data: audioCount } = useQuery({
    queryKey: ["admin-stats", "audios", "count"],
    queryFn: () => adminStatsApi.getAudioCount(),
    staleTime: 60_000,
  });

  const { data: userCount } = useQuery({
    queryKey: ["admin-stats", "users", "count"],
    queryFn: () => adminStatsApi.getUserCount(),
    staleTime: 60_000,
  });

  const { data: dishCount } = useQuery({
    queryKey: ["admin-stats", "dishes", "count"],
    queryFn: () => adminStatsApi.getDishCount(),
    staleTime: 60_000,
  });

  const entityStats = [
    {
      label: "Tổng nhà hàng",
      value: restaurantCount?.count ?? 0,
      delta: "",
      deltaType: "neutral" as const,
      icon: Store,
    },
    {
      label: "Tổng âm thanh",
      value: audioCount?.count ?? 0,
      delta: "",
      deltaType: "neutral" as const,
      icon: Headphones,
    },
    {
      label: "Người dùng",
      value: userCount?.count ?? 0,
      delta: "",
      deltaType: "neutral" as const,
      icon: Users,
    },
    {
      label: "Tổng món ăn",
      value: dishCount?.count ?? 0,
      delta: "",
      deltaType: "neutral" as const,
      icon: UtensilsCrossed,
    },
  ];

  // ── Analytics queries ──────────────────────────────────────────────────────

  const { data: kpis } = useQuery({
    queryKey: ["analytics", "kpis"],
    queryFn: () => analyticsApi.getKpis(),
    staleTime: 30_000, // 30s — KPIs don't change every second
  });

  const { data: topRestaurantsData } = useQuery({
    queryKey: ["analytics", "top-restaurants"],
    queryFn: () => analyticsApi.getTopRestaurants(5),
    staleTime: 60_000,
  });

  const barChartData = useMemo(
    () => toBarChartData(topRestaurantsData?.items ?? []),
    [topRestaurantsData],
  );

  const { data: heatmapData } = useQuery({
    queryKey: ["analytics", "heatmap", heatmapHours],
    queryFn: () => analyticsApi.getHeatmap(heatmapHours),
    staleTime: 60_000,
  });

  const { data: movementPaths } = useQuery({
    queryKey: ["analytics", "movement-paths"],
    queryFn: () => analyticsApi.getMovementPaths(50),
    staleTime: 60_000,
  });

  const avgTime = kpis?.averageListeningTimeSeconds ?? 0;
  const formattedAvgTime = avgTime > 0 ? formatMinutesSeconds(avgTime) : "—";

  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Tổng quan hệ thống</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Hệ thống quản lý âm thanh thuyết minh &amp; nhà hàng
          </p>
        </div>
        <div className="text-xs text-muted-foreground mono">
          Cập nhật lần cuối: {new Date().toLocaleString("vi-VN")}
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6 space-y-6">
        {/* ── Entity Stats ─────────────────────────────────────────────────── */}
        <div className="grid grid-cols-4 gap-4">
          {entityStats.map((stat) => (
            <div key={stat.label} className="stat-card">
              <div className="flex items-center justify-between mb-3">
                <span className="stat-label">{stat.label}</span>
                <stat.icon className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="flex items-end gap-2">
                <span className="stat-value">
                  {stat.value.toLocaleString()}
                </span>
                {stat.deltaType === "positive" && stat.delta && (
                  <span className="stat-delta-positive flex items-center gap-0.5 mb-0.5">
                    <ArrowUp className="h-3 w-3" />
                    {stat.delta}
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* ── Charts row ────────────────────────────────────────────────────── */}
        <div className="grid grid-cols-2 gap-4">
          {/* Daily listens — placeholder area chart driven by static mock data */}
          <div className="stat-card">
            <h3 className="text-sm font-semibold text-foreground mb-4">
              Lượt nghe theo ngày
            </h3>
            <ResponsiveContainer width="100%" height={240}>
              <AreaChart
                data={[
                  { date: "12/03", listens: 245 },
                  { date: "13/03", listens: 312 },
                  { date: "14/03", listens: 289 },
                  { date: "15/03", listens: 456 },
                  { date: "16/03", listens: 398 },
                  { date: "17/03", listens: 521 },
                  { date: "18/03", listens: 478 },
                  { date: "19/03", listens: 612 },
                  { date: "20/03", listens: 534 },
                  { date: "21/03", listens: 489 },
                  { date: "22/03", listens: 567 },
                  { date: "23/03", listens: 623 },
                  { date: "24/03", listens: 701 },
                  { date: "25/03", listens: 658 },
                ]}
              >
                <defs>
                  <linearGradient id="colorListens" x1="0" y1="0" x2="0" y2="1">
                    <stop
                      offset="5%"
                      stopColor="hsl(221, 83%, 53%)"
                      stopOpacity={0.15}
                    />
                    <stop
                      offset="95%"
                      stopColor="hsl(221, 83%, 53%)"
                      stopOpacity={0}
                    />
                  </linearGradient>
                </defs>
                <CartesianGrid
                  strokeDasharray="3 3"
                  stroke="hsl(214, 32%, 91%)"
                />
                <XAxis
                  dataKey="date"
                  tick={{ fontSize: 12 }}
                  stroke="hsl(215, 16%, 47%)"
                />
                <YAxis tick={{ fontSize: 12 }} stroke="hsl(215, 16%, 47%)" />
                <Tooltip
                  contentStyle={{
                    borderRadius: 8,
                    fontSize: 13,
                    border: "1px solid hsl(214, 32%, 91%)",
                  }}
                />
                <Area
                  type="monotone"
                  dataKey="listens"
                  stroke="hsl(221, 83%, 53%)"
                  strokeWidth={2}
                  fill="url(#colorListens)"
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          {/* Top restaurants bar chart — driven by real API data */}
          <div className="stat-card">
            <h3 className="text-sm font-semibold text-foreground mb-4">
              Nhà hàng được nghe nhiều nhất
            </h3>
            {barChartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={barChartData} layout="vertical">
                  <CartesianGrid
                    strokeDasharray="3 3"
                    stroke="hsl(214, 32%, 91%)"
                  />
                  <XAxis
                    type="number"
                    tick={{ fontSize: 12 }}
                    stroke="hsl(215, 16%, 47%)"
                  />
                  <YAxis
                    type="category"
                    dataKey="name"
                    tick={{ fontSize: 11 }}
                    width={130}
                    stroke="hsl(215, 16%, 47%)"
                  />
                  <Tooltip
                    contentStyle={{
                      borderRadius: 8,
                      fontSize: 13,
                      border: "1px solid hsl(214, 32%, 91%)",
                    }}
                  />
                  <Bar
                    dataKey="listens"
                    fill="hsl(199, 89%, 48%)"
                    radius={[0, 4, 4, 0]}
                  />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div className="h-[240px] flex items-center justify-center text-muted-foreground text-sm">
                Chưa có dữ liệu lượt nghe
              </div>
            )}
          </div>
        </div>

        {/* ── Analytics KPIs ────────────────────────────────────────────────── */}
        <div className="grid grid-cols-2 max-w-2xl gap-4">
          <div className="stat-card">
            <span className="stat-label">Tổng lượt nghe</span>
            <div className="mt-2 flex items-baseline gap-1">
              <span className="stat-value mono">
                {kpis?.totalPoiPlays != null
                  ? kpis.totalPoiPlays.toLocaleString("vi-VN")
                  : "—"}
              </span>
              {kpis && (
                <span className="stat-delta-positive flex items-center gap-0.5">
                  <TrendingUp className="h-3 w-3" />
                  {kpis.totalUsers.toLocaleString()} phiên
                </span>
              )}
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Thời gian trung bình nghe 1 POI</span>
            <div className="mt-2 flex items-baseline gap-1">
              <span className="stat-value mono">{formattedAvgTime}</span>
              {avgTime > 0 && (
                <span className="text-xs text-muted-foreground mb-0.5">
                  phút
                </span>
              )}
            </div>
          </div>
        </div>

        {/* ── Maps ───────────────────────────────────────────────────────────── */}
        <HeatmapSection
          points={heatmapData?.points}
          movementPaths={movementPaths?.sessions}
          restaurantPois={topRestaurantsData?.items}
          lookbackHours={heatmapHours}
          onLookbackHoursChange={setHeatmapHours}
        />
      </div>
    </AdminLayout>
  );
};

export default Dashboard;
