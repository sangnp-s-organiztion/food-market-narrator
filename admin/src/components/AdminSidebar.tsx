import { useLocation, useNavigate } from "react-router-dom";
import {
  LayoutDashboard,
  Store,
  Users,
  ScrollText,
  Route,
  AudioWaveform,
  LogOut,
} from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";
import { cn } from "@/lib/utils";

const navItems = [
  { path: "/", label: "Tổng quan", icon: LayoutDashboard },
  { path: "/trajectory", label: "Tuyến di chuyển", icon: Route },
  { path: "/restaurants", label: "Nhà hàng", icon: Store },
  { path: "/users", label: "Người dùng", icon: Users },
  { path: "/logs", label: "Nhật ký", icon: ScrollText },
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
      className="fixed left-0 top-0 bottom-0 w-60 flex flex-col z-40"
      style={{ background: "hsl(222, 47%, 6%)" }}
    >
      {/* Logo */}
      <div
        className="flex items-center gap-2.5 px-5 py-5 border-b"
        style={{ borderColor: "rgba(255,255,255,0.08)" }}
      >
        <AudioWaveform
          className="h-7 w-7"
          style={{ color: "hsl(221, 83%, 53%)" }}
        />
        <span
          className="text-base font-semibold tracking-tight truncate"
          style={{ color: "white" }}
          title="Food Market Narrator"
        >
          Food Market Narrator
        </span>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
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

      {/* User info + logout */}
      <div
        className="px-4 py-4 border-t"
        style={{ borderColor: "rgba(255,255,255,0.08)" }}
      >
        <div className="flex items-center gap-2.5">
          <div
            className="h-8 w-8 rounded-full flex items-center justify-center text-xs font-semibold shrink-0"
            style={{ background: "hsl(221, 83%, 53%)", color: "white" }}
          >
            {user?.username?.charAt(0).toUpperCase() ?? "A"}
          </div>
          <div className="flex-1 min-w-0">
            <p
              className="text-sm font-medium truncate"
              style={{ color: "white" }}
            >
              {user?.username ?? "Admin"}
            </p>
            <p
              className="text-xs truncate"
              style={{ color: "hsl(215, 20%, 65%)" }}
            >
              {roleLabel}
            </p>
          </div>
          <button
            onClick={handleLogout}
            className="p-1.5 rounded-md transition-colors hover:bg-white/10"
            title="Đăng xuất"
          >
            <LogOut
              className="h-4 w-4"
              style={{ color: "hsl(215, 20%, 65%)" }}
            />
          </button>
        </div>
      </div>
    </aside>
  );
};

export default AdminSidebar;
