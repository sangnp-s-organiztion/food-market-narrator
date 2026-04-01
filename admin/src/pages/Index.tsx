import { useMemo } from "react";
import AdminLayout from "@/components/AdminLayout";
import { Store, Headphones, Users, UtensilsCrossed, TrendingUp, ArrowUp } from "lucide-react";
import { analyticsApi } from "@/lib/analyticsApi";
import { useQuery } from "@tanstack/react-query";
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, BarChart, Bar } from "recharts";
import HeatmapSection from "@/components/HeatmapSection";
import UserRouteSection from "@/components/UserRouteSection";

const Dashboard = () => {
  const { data: entityCounts } = useQuery({
    queryKey: ["analytics", "entity-counts"],
    queryFn: () => analyticsApi.getEntityCounts(),
    staleTime: 60_000,
  });

  const entityStats = [
    { label: "Tổng nhà hàng", value: entityCounts?.totalRestaurants ?? "—", delta: null, deltaType: "neutral" as const, icon: Store },
    { label: "Tổng âm thanh",  value: entityCounts?.totalAudios ?? "—",      delta: null, deltaType: "neutral" as const, icon: Headphones },
    { label: "Người dùng",    value: entityCounts?.totalUsers ?? "—",        delta: null, deltaType: "neutral" as const, icon: Users },
    { label: "Tổng món ăn",   value: entityCounts?.totalDishes ?? "—",       delta: null, deltaType: "neutral" as const, icon: UtensilsCrossed },
  ];

  const { data: timeseriesData } = useQuery({
    queryKey: ["analytics", "listens-timeseries", 14],
    queryFn: () => analyticsApi.getListensTimeseries(14),
    staleTime: 60_000,
  });

  const dailyListensData = useMemo(() => {
    return (timeseriesData?.items ?? []).map((item) => ({
      date: item.date.slice(5), // "03-01" from "2026-03-01"
      listens: item.listens,
    }));
  }, [timeseriesData]);

  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Tổng quan hệ thống</h1>
          <p className="text-sm text-muted-foreground mt-0.5">Hệ thống quản lý âm thanh thuyết minh & nhà hàng</p>
        </div>
        <div className="text-xs text-muted-foreground mono">
          Cập nhật lần cuối: {new Date().toLocaleString("vi-VN")}
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6 space-y-6">
        {/* Stats Grid */}
        <div className="grid grid-cols-4 gap-4">
          {entityStats.map((stat) => (
            <div key={stat.label} className="stat-card">
              <div className="flex items-center justify-between mb-3">
                <span className="stat-label">{stat.label}</span>
                <stat.icon className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="flex items-end gap-2">
                <span className="stat-value">{String(stat.value).toLocaleString()}</span>
                {stat.deltaType === "positive" && (
                  <span className="stat-delta-positive flex items-center gap-0.5 mb-0.5">
                    <ArrowUp className="h-3 w-3" />{stat.delta}
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>

        {/* Charts */}
        <div className="grid grid-cols-2 gap-4">
          <div className="stat-card">
            <h3 className="text-sm font-semibold text-foreground mb-4">Lượt nghe theo ngày</h3>
            <ResponsiveContainer width="100%" height={240}>
              <AreaChart data={dailyListensData}>
                <defs>
                  <linearGradient id="colorListens" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="hsl(221, 83%, 53%)" stopOpacity={0.15} />
                    <stop offset="95%" stopColor="hsl(221, 83%, 53%)" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(214, 32%, 91%)" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} stroke="hsl(215, 16%, 47%)" />
                <YAxis tick={{ fontSize: 12 }} stroke="hsl(215, 16%, 47%)" />
                <Tooltip contentStyle={{ borderRadius: 8, fontSize: 13, border: '1px solid hsl(214, 32%, 91%)' }} />
                <Area type="monotone" dataKey="listens" stroke="hsl(221, 83%, 53%)" strokeWidth={2} fill="url(#colorListens)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
          <div className="stat-card">
            <h3 className="text-sm font-semibold text-foreground mb-4">Nhà hàng được nghe nhiều nhất</h3>
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={[]} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" stroke="hsl(214, 32%, 91%)" />
                <XAxis type="number" tick={{ fontSize: 12 }} stroke="hsl(215, 16%, 47%)" />
                <YAxis type="category" dataKey="name" tick={{ fontSize: 11 }} width={130} stroke="hsl(215, 16%, 47%)" />
                <Tooltip contentStyle={{ borderRadius: 8, fontSize: 13, border: '1px solid hsl(214, 32%, 91%)' }} />
                <Bar dataKey="listens" fill="hsl(199, 89%, 48%)" radius={[0, 4, 4, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Metrics */}
        <div className="grid grid-cols-2 max-w-2xl gap-4">
          <div className="stat-card">
            <span className="stat-label">Tổng lượt nghe</span>
            <div className="mt-2 flex items-baseline gap-1">
              <span className="stat-value mono">—</span>
              <span className="stat-delta-positive flex items-center gap-0.5">
                <TrendingUp className="h-3 w-3" />
              </span>
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Thời gian trung bình nghe 1 POI</span>
            <div className="mt-2 flex items-baseline gap-1">
              <span className="stat-value mono">—</span>
              <span className="text-xs text-muted-foreground mb-0.5">phút</span>
            </div>
          </div>
        </div>

        {/* Heatmap */}
        <HeatmapSection />

        {/* User Routes */}
        <UserRouteSection />
      </div>
    </AdminLayout>
  );
};

export default Dashboard;
