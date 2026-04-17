import { useMemo, useState, type ComponentType } from "react";
import { useQuery } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import { Store, Users } from "lucide-react";
import {
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
import { adminStatsApi, restaurantApi } from "@/lib/adminApi";

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

type EntityStat = {
  label: string;
  value: number;
  icon: ComponentType<{ className?: string }>;
};

// ─── Dashboard Page ───────────────────────────────────────────────────────────

const Dashboard = () => {
  const [heatmapHours, setHeatmapHours] = useState<1 | 6 | 24 | "all">("all");

  const { data: restaurantCount } = useQuery({
    queryKey: ["admin-stats", "restaurants", "count"],
    queryFn: () => adminStatsApi.getRestaurantCount(),
    staleTime: 60_000,
  });

  const { data: userCount } = useQuery({
    queryKey: ["admin-stats", "users", "count"],
    queryFn: () => adminStatsApi.getUserCount(),
    staleTime: 60_000,
  });

  const entityStats: EntityStat[] = [
    {
      label: "Tổng nhà hàng",
      value: restaurantCount?.count ?? 0,
      icon: Store,
    },
    {
      label: "Tổng khách tham quan",
      value: userCount?.visitors ?? 0,
      icon: Users,
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

  const { data: allRestaurantsData } = useQuery({
    queryKey: ["admin", "restaurants", "all-for-heatmap"],
    queryFn: () => restaurantApi.getAll(),
    staleTime: 120_000,
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

  const heatmapPoiList = useMemo(() => {
    const playCountByRestaurant = new Map(
      (topRestaurantsData?.items ?? []).map((r) => [
        r.restaurantId,
        r.playCount,
      ]),
    );

    return (allRestaurantsData ?? [])
      .filter((r) => r.isActive)
      .map((r) => ({
        restaurantId: r.restaurantId,
        restaurantName: r.name,
        latitude: r.latitude,
        longitude: r.longitude,
        playCount: playCountByRestaurant.get(r.restaurantId) ?? 0,
      }));
  }, [allRestaurantsData, topRestaurantsData]);

  const avgTime = kpis?.averageListeningTimeSeconds ?? 0;
  const formattedAvgTime = avgTime > 0 ? formatMinutesSeconds(avgTime) : "0.0";

  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Tổng quan hệ thống</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Hệ thống quản lý âm thanh thuyết minh &amp; nhà hàng
          </p>
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
              </div>
            </div>
          ))}
          <div className="stat-card">
            <span className="stat-label">Tổng lượt nghe</span>
            <div className="mt-2 flex items-baseline gap-1">
              <span className="stat-value mono">
                {kpis?.totalPoiPlays != null
                  ? kpis.totalPoiPlays.toLocaleString("vi-VN")
                  : "—"}
              </span>
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">
              Thời gian trung bình nghe 1 nhà hàng
            </span>
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

        {/* ── Charts row ────────────────────────────────────────────────────── */}
        <div className="grid grid-cols-1 gap-4">
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

        {/* ── Maps ───────────────────────────────────────────────────────────── */}
        <HeatmapSection
          points={heatmapData?.points}
          poiList={heatmapPoiList}
          lookbackHours={heatmapHours}
          onLookbackHoursChange={setHeatmapHours}
        />
      </div>
    </AdminLayout>
  );
};

export default Dashboard;
