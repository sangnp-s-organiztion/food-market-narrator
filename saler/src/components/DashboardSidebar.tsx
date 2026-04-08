import { NavLink } from "@/components/NavLink";
import { useAuth } from "@/contexts/AuthContext";
import { cn } from "@/lib/utils";
import { useEffect, useState } from "react";
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
  ChevronDown,
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
  { title: "Tài khoản", url: "/dashboard/account", icon: CircleUser },
];

const audioSubItems = [
  { title: "Mô tả âm thanh", url: "/dashboard/audio/description" },
  { title: "Thống kê", url: "/dashboard/audio/history" },
];

export function DashboardSidebar() {
  const { state } = useSidebar();
  const collapsed = state === "collapsed";
  const { user, logout } = useAuth();
  const location = useLocation();
  const isAudioSection = location.pathname.startsWith("/dashboard/audio");
  const [audioMenuOpen, setAudioMenuOpen] = useState(isAudioSection);

  useEffect(() => {
    if (isAudioSection) {
      setAudioMenuOpen(true);
    }
  }, [isAudioSection]);

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
              {navItems.slice(0, 3).map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild isActive={location.pathname === item.url}>
                    <NavLink
                      to={item.url}
                      end
                      className="hover:bg-sidebar-accent"
                      activeClassName="bg-sidebar-accent text-sidebar-foreground font-medium"
                    >
                      <item.icon className="mr-2 h-4 w-4" />
                      {!collapsed && <span>{item.title}</span>}
                    </NavLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}

              <SidebarMenuItem>
                {collapsed ? (
                  <SidebarMenuButton asChild isActive={isAudioSection}>
                    <NavLink
                      to="/dashboard/audio/description"
                      className="hover:bg-sidebar-accent"
                      activeClassName="bg-sidebar-accent text-sidebar-foreground font-medium"
                    >
                      <Volume2 className="mr-2 h-4 w-4" />
                    </NavLink>
                  </SidebarMenuButton>
                ) : (
                  <div className="w-full">
                    <button
                      type="button"
                      onClick={() => setAudioMenuOpen((prev) => !prev)}
                      className={cn(
                        "flex w-full items-center gap-2 rounded-md px-2 py-2 text-sm transition-colors hover:bg-sidebar-accent",
                        isAudioSection && "bg-sidebar-accent text-sidebar-foreground font-medium",
                      )}
                    >
                      <Volume2 className="h-4 w-4" />
                      <span className="flex-1 text-left">Âm thanh</span>
                      <ChevronDown
                        className={cn(
                          "h-4 w-4 transition-transform",
                          audioMenuOpen && "rotate-180",
                        )}
                      />
                    </button>

                    {audioMenuOpen && (
                      <div className="mt-1 space-y-1 pl-6">
                        {audioSubItems.map((sub) => {
                          const isActive = location.pathname === sub.url;
                          return (
                            <NavLink
                              key={sub.url}
                              to={sub.url}
                              className={cn(
                                "block rounded-md px-2 py-1.5 text-sm text-sidebar-muted transition-colors hover:bg-sidebar-accent hover:text-sidebar-foreground",
                                isActive && "bg-sidebar-accent text-sidebar-foreground font-medium",
                              )}
                            >
                              {sub.title}
                            </NavLink>
                          );
                        })}
                      </div>
                    )}
                  </div>
                )}
              </SidebarMenuItem>

              {navItems.slice(3).map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild isActive={location.pathname === item.url}>
                    <NavLink
                      to={item.url}
                      end
                      className="hover:bg-sidebar-accent"
                      activeClassName="bg-sidebar-accent text-sidebar-foreground font-medium"
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
