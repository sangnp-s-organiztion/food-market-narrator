import { NavLink } from "@/components/NavLink";
import { useAuth } from "@/contexts/AuthContext";
import { useLocation } from "react-router-dom";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  useSidebar,
} from "@/components/ui/sidebar";
import {
  CircleUser,
  ImageIcon,
  LogOut,
  Store,
  UtensilsCrossed,
  Volume2,
} from "lucide-react";
import { Button } from "@/components/ui/button";

const navItems = [
  { title: "Nhà hàng", url: "/dashboard/restaurant", icon: Store },
  { title: "Thực đơn", url: "/dashboard/dishes", icon: UtensilsCrossed },
  { title: "Hình ảnh", url: "/dashboard/images", icon: ImageIcon },
  { title: "Âm thanh", url: "/dashboard/audio", icon: Volume2 },
  { title: "Tài khoản", url: "/dashboard/account", icon: CircleUser },
];

export function DashboardSidebar() {
  const { state } = useSidebar();
  const collapsed = state === "collapsed";
  const { user, logout } = useAuth();
  const location = useLocation();

  return (
    <Sidebar collapsible="icon">
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel className="text-sidebar-muted">
            {!collapsed && (
              <span className="flex items-center gap-2">
                <UtensilsCrossed className="h-4 w-4 text-sidebar-primary" />
                Bảng điều khiển
              </span>
            )}
          </SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {navItems.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild isActive={location.pathname === item.url}>
                    <NavLink
                      to={item.url}
                      end
                      className="hover:bg-sidebar-accent"
                      activeClassName="bg-sidebar-accent text-sidebar-primary font-medium"
                    >
                      <item.icon className="mr-2 h-4 w-4" />
                      {!collapsed && <span>{item.title}</span>}
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        {!collapsed && user && (
          <div className="px-2 pb-2">
            <p className="mb-2 truncate text-xs text-sidebar-muted">
              Đăng nhập với <span className="font-medium text-sidebar-foreground">{user.username}</span>
            </p>
            <Button
              variant="ghost"
              size="sm"
              onClick={logout}
              className="w-full justify-start text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-foreground"
            >
              <LogOut className="mr-2 h-4 w-4" />
              Đăng xuất
            </Button>
          </div>
        )}
      </SidebarFooter>
    </Sidebar>
  );
}
