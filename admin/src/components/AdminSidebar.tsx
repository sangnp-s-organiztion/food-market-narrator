import { useLocation, useNavigate } from "react-router-dom";
import {
  AudioWaveform,
  CircleUser,
  LayoutDashboard,
  LogOut,
  Receipt,
  Route,
  ScrollText,
  Store,
  Users,
} from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { cn } from "@/lib/utils";

const navItems = [
  { path: "/", label: "Tổng quan", icon: LayoutDashboard },
  { path: "/tours", label: "Tour", icon: Route },
  { path: "/restaurants", label: "Nhà hàng", icon: Store },
  { path: "/users", label: "Người dùng", icon: Users },
  { path: "/logs", label: "Nhật ký", icon: ScrollText },
  { path: "/translation-billing", label: "Chi phí dịch token", icon: Receipt },
  { path: "/account", label: "Tài khoản", icon: CircleUser },
];

const AdminSidebar = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const roleLabel =
    user?.role?.toLowerCase() === "admin"
      ? "Quản trị viên"
      : user?.role?.toLowerCase() === "saler"
        ? "Người bán"
        : (user?.role ?? "");

  const handleLogout = async () => {
    await logout();
    navigate("/login");
  };

  return (
    <aside
      className="fixed bottom-0 left-0 top-0 z-40 flex w-60 flex-col"
      style={{ background: "hsl(222, 47%, 6%)" }}
    >
      <div
        className="flex items-center gap-2.5 border-b px-5 py-5"
        style={{ borderColor: "rgba(255,255,255,0.08)" }}
      >
        <AudioWaveform className="h-7 w-7" style={{ color: "hsl(221, 83%, 53%)" }} />
        <span
          className="truncate text-base font-semibold tracking-tight"
          style={{ color: "white" }}
          title="Food Market Narrator"
        >
          Food Market Narrator
        </span>
      </div>

      <nav className="flex-1 space-y-1 overflow-y-auto px-3 py-4">
        {navItems.map((item) => {
          const isActive =
            location.pathname === item.path ||
            (item.path !== "/" && location.pathname.startsWith(item.path));

          return (
            <button
              key={item.path}
              onClick={() => navigate(item.path)}
              className={cn("sidebar-item w-full", isActive ? "active" : "")}
            >
              <item.icon className="h-4 w-4 shrink-0" />
              {item.label}
            </button>
          );
        })}
      </nav>

      <div className="border-t px-4 py-4" style={{ borderColor: "rgba(255,255,255,0.08)" }}>
        <div className="flex items-center gap-2.5">
          <div
            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold"
            style={{ background: "hsl(221, 83%, 53%)", color: "white" }}
          >
            {user?.username?.charAt(0).toUpperCase() ?? "A"}
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium" style={{ color: "white" }}>
              {user?.username ?? "Admin"}
            </p>
            <p className="truncate text-xs" style={{ color: "hsl(215, 20%, 65%)" }}>
              {roleLabel}
            </p>
          </div>
          <button
            onClick={handleLogout}
            className="rounded-md p-1.5 transition-colors hover:bg-white/10"
            title="Đăng xuất"
          >
            <LogOut className="h-4 w-4" style={{ color: "hsl(215, 20%, 65%)" }} />
          </button>
        </div>
      </div>
    </aside>
  );
};

export default AdminSidebar;
