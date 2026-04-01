import AdminLayout from "@/components/AdminLayout";
import { Store, Headphones, Users, UtensilsCrossed, TrendingUp, ArrowUp } from "lucide-react";
import { restaurants, audios, users, dishes, topRestaurants } from "@/lib/mockData";
import { XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, BarChart, Bar } from "recharts";
import HeatmapSection from "@/components/HeatmapSection";
import UserRouteSection from "@/components/UserRouteSection";

const stats = [
  { label: "Tổng nhà hàng", value: restaurants.length, delta: "+2", deltaType: "positive" as const, icon: Store },
  { label: "Tổng âm thanh", value: audios.length, delta: "+3", deltaType: "positive" as const, icon: Headphones },
  { label: "Người dùng", value: users.length, delta: "0", deltaType: "neutral" as const, icon: Users },
  { label: "Tổng món ăn", value: dishes.length, delta: "+5", deltaType: "positive" as const, icon: UtensilsCrossed },
];

const Dashboard = () => {
  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Tổng quan hệ thống</h1>
          <p className="text-sm text-muted-foreground mt-0.5">Hệ thống quản lý âm thanh thuyết minh & nhà hàng</p>
        </div>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6 space-y-6">
        {/* Stats Grid */}
        <div className="grid grid-cols-4 gap-4">
          {stats.map((stat) => (
            <div key={stat.label} className="stat-card">
              <div className="flex items-center justify-between mb-3">
                <span className="stat-label">{stat.label}</span>
                <stat.icon className="h-4 w-4 text-muted-foreground" />
              </div>
              <div className="flex items-end gap-2">
                <span className="stat-value">{stat.value.toLocaleString()}</span>
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
        <div className="grid grid-cols-1 gap-4">
          <div className="stat-card">
            <h3 className="text-sm font-semibold text-foreground mb-4">Nhà hàng được nghe nhiều nhất</h3>
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={topRestaurants} layout="vertical">
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
              <span className="stat-value mono">15,254</span>
              <span className="stat-delta-positive flex items-center gap-0.5">
                <TrendingUp className="h-3 w-3" />+12.5%
              </span>
            </div>
          </div>
          <div className="stat-card">
            <span className="stat-label">Thời gian trung bình nghe 1 POI</span>
            <div className="mt-2 flex items-baseline gap-1">
              <span className="stat-value mono">3:45</span>
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
