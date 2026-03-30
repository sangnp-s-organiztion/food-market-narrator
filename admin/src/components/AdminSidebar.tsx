import { useLocation, useNavigate } from "react-router-dom";
import {
  LayoutDashboard,
  Store,
  Users,
  ScrollText,
  
  AudioWaveform,
  LogOut,
} from "lucide-react";
import { useAuth } from "@/contexts/AuthContext";

const navItems = [
  { path: "/", label: "Tổng quan", icon: LayoutDashboard },
  { path: "/restaurants", label: "Nhà hàng", icon: Store },
  { path: "/users", label: "Người dùng", icon: Users },
  { path: "/logs", label: "Nhật ký", icon: ScrollText },
];

const AdminSidebar = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { logout } = useAuth();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <aside className="fixed left-0 top-0 bottom-0 w-60 flex flex-col z-40" style={{ background: 'hsl(222, 47%, 6%)' }}>
      <div className="flex items-center gap-2.5 px-5 py-5 border-b" style={{ borderColor: 'rgba(255,255,255,0.08)' }}>
        <AudioWaveform className="h-7 w-7" style={{ color: 'hsl(221, 83%, 53%)' }} />
        <span className="text-base font-semibold tracking-tight" style={{ color: 'white' }}>SonicMap</span>
        <span className="text-xs font-medium px-1.5 py-0.5 rounded" style={{ background: 'hsl(221, 83%, 53%)', color: 'white' }}>Admin</span>
      </div>
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        {navItems.map((item) => {
          const isActive = location.pathname === item.path || 
            (item.path !== "/" && location.pathname.startsWith(item.path));
          return (
            <button
              key={item.path}
              onClick={() => navigate(item.path)}
              className={`sidebar-item w-full ${isActive ? "active" : ""}`}
            >
              <item.icon className="h-4 w-4 shrink-0" />
              {item.label}
            </button>
          );
        })}
      </nav>
      <div className="px-4 py-4 border-t" style={{ borderColor: 'rgba(255,255,255,0.08)' }}>
        <div className="flex items-center gap-2.5">
          <div className="h-8 w-8 rounded-full flex items-center justify-center text-xs font-semibold" style={{ background: 'hsl(221, 83%, 53%)', color: 'white' }}>
            A
          </div>
          <div className="flex-1">
            <p className="text-sm font-medium" style={{ color: 'white' }}>Admin</p>
            <p className="text-xs" style={{ color: 'hsl(215, 20%, 65%)' }}>admin@sonicmap.vn</p>
          </div>
          <button
            onClick={handleLogout}
            className="p-1.5 rounded-md transition-colors hover:bg-white/10"
            title="Đăng xuất"
          >
            <LogOut className="h-4 w-4" style={{ color: 'hsl(215, 20%, 65%)' }} />
          </button>
        </div>
      </div>
    </aside>
  );
};

export default AdminSidebar;
