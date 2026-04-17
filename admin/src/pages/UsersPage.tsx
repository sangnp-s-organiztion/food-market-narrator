import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, Lock, Plus, Shield, Unlock } from "lucide-react";
import AdminLayout from "@/components/AdminLayout";
import ConfirmDialog from "@/components/ConfirmDialog";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  restaurantApi,
  userApi,
  type CreateUserRequest,
  type UserResponse,
  type VisitorSessionResponse,
} from "@/lib/adminApi";
import { toast } from "sonner";

const PHONE_REGEX = /^0\d{9,10}$/;
const EMAIL_REGEX = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

function toPageUser(r: UserResponse) {
  const normalizedRole = (r.role ?? "").toLowerCase();
  return {
    user_id: r.userId,
    username: r.username,
    fullName: r.fullName ?? "",
    phone: r.phone ?? "",
    email: r.email ?? "",
    role: normalizedRole === "admin" ? ("admin" as const) : ("saler" as const),
    is_active: r.isActive,
    created_at: r.createdAt ? r.createdAt.split("T")[0] : "",
  };
}

function toDisplayDateTime(value?: string | null): string {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return date.toLocaleString("vi-VN", {
    hour12: false,
  });
}

function toPageVisitor(visitor: VisitorSessionResponse) {
  return {
    sessionId: visitor.sessionId || "-",
    deviceId: visitor.deviceId || "-",
    deviceInfo: visitor.deviceInfo || "unknown",
    lastSeenAt: toDisplayDateTime(visitor.lastSeenAtUtc),
    createdAt: toDisplayDateTime(visitor.createdAtUtc),
  };
}

const emptyForm = {
  username: "",
  password: "",
  confirmPassword: "",
  phone: "",
  email: "",
  role: "saler" as const,
};

type PageUser = ReturnType<typeof toPageUser>;

const UsersPage = () => {
  const qc = useQueryClient();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState<{
    username: string;
    password: string;
    confirmPassword: string;
    phone: string;
    email: string;
    role: "admin" | "saler";
  }>(emptyForm);

  const [confirmUser, setConfirmUser] = useState<{
    id: number;
    name: string;
    lock: boolean;
  } | null>(null);
  const [detailUser, setDetailUser] = useState<PageUser | null>(null);

  const {
    data: apiUsers = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: userApi.getAll,
    staleTime: 60_000,
  });

  const {
    data: visitorSessions = [],
    isLoading: isVisitorsLoading,
    isError: isVisitorsError,
  } = useQuery({
    queryKey: ["admin", "users", "visitors"],
    queryFn: () => userApi.getVisitors(500),
    staleTime: 60_000,
  });

  const {
    data: restaurants = [],
    isLoading: isRestaurantsLoading,
    isError: isRestaurantsError,
  } = useQuery({
    queryKey: ["admin", "restaurants"],
    queryFn: restaurantApi.getAll,
    staleTime: 60_000,
  });

  const pageUsers = apiUsers.map(toPageUser);
  const pageVisitors = visitorSessions.map(toPageVisitor);
  const detailUserRestaurants = useMemo(() => {
    if (!detailUser) return [];

    return restaurants
      .filter((restaurant) => restaurant.userId === detailUser.user_id)
      .sort((a, b) => a.name.localeCompare(b.name));
  }, [restaurants, detailUser]);

  const createMutation = useMutation({
    mutationFn: (data: CreateUserRequest) => userApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Tạo người dùng thành công");
      setDialogOpen(false);
      setForm(emptyForm);
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Tạo người dùng thất bại");
    },
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      userApi.updateStatus(id, { isActive }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Cập nhật trạng thái thành công");
      setConfirmUser(null);
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Cập nhật thất bại");
    },
  });

  const handleCreate = () => {
    if (!form.username.trim()) {
      toast.error("Vui lòng nhập tên đăng nhập");
      return;
    }

    if (!form.password.trim()) {
      toast.error("Vui lòng nhập mật khẩu");
      return;
    }

    if (form.password !== form.confirmPassword) {
      toast.error("Mật khẩu nhập lại không khớp");
      return;
    }

    if (!PHONE_REGEX.test(form.phone.trim())) {
      toast.error("Số điện thoại không hợp lệ (bắt đầu bằng 0, gồm 10-11 số)");
      return;
    }

    if (!EMAIL_REGEX.test(form.email.trim())) {
      toast.error("Email không hợp lệ");
      return;
    }

    createMutation.mutate({
      username: form.username.trim(),
      password: form.password.trim(),
      phone: form.phone.trim(),
      email: form.email.trim(),
      role: form.role,
    });
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Quản lý người dùng</h1>
        <Button
          onClick={() => {
            setForm(emptyForm);
            setDialogOpen(true);
          }}
          size="sm"
        >
          <Plus className="mr-1.5 h-4 w-4" />
          Tạo người dùng
        </Button>
      </div>

      <div className="mx-auto max-w-7xl px-8 py-6">
        <div className="stat-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Tên đăng nhập</th>
                <th>Số điện thoại</th>
                <th>Email</th>
                <th>Vai trò</th>
                <th>Trạng thái</th>
                <th>Ngày tạo</th>
                <th className="w-40">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td
                    colSpan={7}
                    className="py-8 text-center text-muted-foreground"
                  >
                    Đang tải...
                  </td>
                </tr>
              )}

              {isError && (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-destructive">
                    Không thể tải danh sách người dùng. Vui lòng thử lại.
                  </td>
                </tr>
              )}

              {!isLoading && !isError && pageUsers.length === 0 && (
                <tr>
                  <td
                    colSpan={7}
                    className="py-8 text-center text-muted-foreground"
                  >
                    Chưa có người dùng nào.
                  </td>
                </tr>
              )}

              {!isLoading &&
                !isError &&
                pageUsers.map((u) => (
                  <tr key={u.user_id}>
                    <td className="font-medium">{u.username}</td>
                    <td className="mono text-xs text-muted-foreground">
                      {u.phone || "-"}
                    </td>
                    <td className="text-xs text-muted-foreground">
                      {u.email || "-"}
                    </td>
                    <td>
                      <span
                        className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${
                          u.role === "admin"
                            ? "bg-primary/10 text-primary"
                            : "bg-muted text-muted-foreground"
                        }`}
                      >
                        <Shield className="h-3 w-3" />
                        {u.role === "admin" ? "Quản trị viên" : "Người bán"}
                      </span>
                    </td>
                    <td>
                      <span
                        className={
                          u.is_active ? "status-active" : "status-inactive"
                        }
                      >
                        {u.is_active ? "Hoạt động" : "Ngừng hoạt động"}
                      </span>
                    </td>
                    <td className="mono text-xs text-muted-foreground">
                      {u.created_at}
                    </td>
                    <td className="flex items-center gap-1">
                      <button
                        onClick={() => setDetailUser(u)}
                        className="rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-muted"
                        title="Xem chi tiết người dùng"
                      >
                        <Eye className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() =>
                          setConfirmUser({
                            id: u.user_id,
                            name: u.username,
                            lock: u.is_active,
                          })
                        }
                        className={`rounded-md p-1.5 transition-colors hover:bg-muted ${
                          !u.is_active
                            ? "text-destructive"
                            : "text-muted-foreground"
                        }`}
                        title={
                          u.is_active ? "Khóa người dùng" : "Mở khóa người dùng"
                        }
                      >
                        {u.is_active ? (
                          <Unlock className="h-4 w-4" />
                        ) : (
                          <Lock className="h-4 w-4" />
                        )}
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>

        <div className="stat-card mt-6">
          <div className="mb-4 flex items-center justify-between">
            <h2 className="text-base font-semibold">Danh sách visitor</h2>
            <span className="text-xs text-muted-foreground">
              Tổng visitor: {pageVisitors.length.toLocaleString("vi-VN")}
            </span>
          </div>

          <table className="data-table">
            <thead>
              <tr>
                <th>Device ID</th>
                <th>Thiết bị</th>
                <th>Lần cuối sử dụng</th>
                <th>Lần đầu sử dụng</th>
              </tr>
            </thead>
            <tbody>
              {isVisitorsLoading && (
                <tr>
                  <td
                    colSpan={4}
                    className="py-8 text-center text-muted-foreground"
                  >
                    Đang tải visitor...
                  </td>
                </tr>
              )}

              {isVisitorsError && (
                <tr>
                  <td colSpan={4} className="py-8 text-center text-destructive">
                    Không thể tải danh sách visitor. Vui lòng thử lại.
                  </td>
                </tr>
              )}

              {!isVisitorsLoading &&
                !isVisitorsError &&
                pageVisitors.length === 0 && (
                  <tr>
                    <td
                      colSpan={4}
                      className="py-8 text-center text-muted-foreground"
                    >
                      Chưa có visitor nào.
                    </td>
                  </tr>
                )}

              {!isVisitorsLoading &&
                !isVisitorsError &&
                pageVisitors.map((visitor) => (
                  <tr key={`${visitor.deviceId}-${visitor.sessionId}`}>
                    <td className="mono text-xs text-muted-foreground">
                      {visitor.deviceId}
                    </td>
                    <td className="max-w-[320px] truncate text-xs text-muted-foreground">
                      {visitor.deviceInfo}
                    </td>
                    <td className="mono text-xs text-muted-foreground">
                      {visitor.lastSeenAt}
                    </td>
                    <td className="mono text-xs text-muted-foreground">
                      {visitor.createdAt}
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      </div>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Tạo người dùng mới</DialogTitle>
          </DialogHeader>
          <div className="grid gap-3 py-2">
            <div>
              <Label className="text-xs">Tên đăng nhập</Label>
              <Input
                value={form.username}
                onChange={(e) => setForm({ ...form, username: e.target.value })}
                className="mt-1"
                placeholder="username"
                autoComplete="username"
              />
            </div>
            <div>
              <Label className="text-xs">Số điện thoại</Label>
              <Input
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                className="mt-1"
                placeholder="0900000001"
                autoComplete="tel"
              />
            </div>
            <div>
              <Label className="text-xs">Email</Label>
              <Input
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                className="mt-1"
                placeholder="user@example.com"
                autoComplete="email"
              />
            </div>
            <div>
              <Label className="text-xs">Mật khẩu</Label>
              <Input
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                className="mt-1"
                placeholder="Nhập mật khẩu"
                autoComplete="new-password"
              />
            </div>
            <div>
              <Label className="text-xs">Nhập lại mật khẩu</Label>
              <Input
                type="password"
                value={form.confirmPassword}
                onChange={(e) =>
                  setForm({ ...form, confirmPassword: e.target.value })
                }
                className="mt-1"
                placeholder="Nhập lại mật khẩu"
                autoComplete="new-password"
              />
            </div>
            <div>
              <Label className="text-xs">Vai trò</Label>
              <Select
                value={form.role}
                onValueChange={(v) =>
                  setForm({ ...form, role: v as "admin" | "saler" })
                }
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="admin">Quản trị viên</SelectItem>
                  <SelectItem value="saler">Người bán</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button
              onClick={handleCreate}
              className="mt-2"
              disabled={createMutation.isPending}
            >
              {createMutation.isPending ? "Đang tạo..." : "Tạo mới"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog
        open={!!detailUser}
        onOpenChange={(open) => !open && setDetailUser(null)}
      >
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Chi tiết người dùng</DialogTitle>
          </DialogHeader>

          {detailUser && (
            <div className="space-y-4">
              <div className="grid gap-3 rounded-md border p-4 sm:grid-cols-2">
                <div>
                  <p className="text-xs text-muted-foreground">Họ và tên</p>
                  <p className="mt-1 font-medium">
                    {detailUser.fullName || "-"}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Tên đăng nhập</p>
                  <p className="mt-1 font-medium">{detailUser.username}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Vai trò</p>
                  <p className="mt-1">
                    {detailUser.role === "admin"
                      ? "Quản trị viên"
                      : "Người bán"}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Số điện thoại</p>
                  <p className="mt-1">{detailUser.phone || "-"}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Email</p>
                  <p className="mt-1">{detailUser.email || "-"}</p>
                </div>
              </div>

              <div className="rounded-md border p-4">
                <h3 className="text-sm font-semibold">Nhà hàng đang quản lý</h3>

                {isRestaurantsLoading && (
                  <p className="mt-3 text-sm text-muted-foreground">
                    Đang tải danh sách nhà hàng...
                  </p>
                )}

                {isRestaurantsError && (
                  <p className="mt-3 text-sm text-destructive">
                    Không thể tải danh sách nhà hàng của người dùng.
                  </p>
                )}

                {!isRestaurantsLoading &&
                  !isRestaurantsError &&
                  detailUserRestaurants.length === 0 && (
                    <p className="mt-3 text-sm text-muted-foreground">
                      Người dùng này chưa quản lý nhà hàng nào.
                    </p>
                  )}

                {!isRestaurantsLoading &&
                  !isRestaurantsError &&
                  detailUserRestaurants.length > 0 && (
                    <div className="mt-3 overflow-x-auto">
                      <table className="data-table min-w-[560px]">
                        <thead>
                          <tr>
                            <th className="w-48">Mã nhà hàng</th>
                            <th>Tên nhà hàng</th>
                            <th className="w-28">Trạng thái</th>
                          </tr>
                        </thead>
                        <tbody>
                          {detailUserRestaurants.map((restaurant) => (
                            <tr key={restaurant.restaurantId}>
                              <td className="mono text-xs">
                                {restaurant.restaurantId}
                              </td>
                              <td>{restaurant.name}</td>
                              <td>
                                <span
                                  className={
                                    restaurant.isActive
                                      ? "status-active"
                                      : "status-inactive"
                                  }
                                >
                                  {restaurant.isActive
                                    ? "Hoạt động"
                                    : "Ngừng hoạt động"}
                                </span>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!confirmUser}
        onOpenChange={(open) => !open && setConfirmUser(null)}
        title={confirmUser?.lock ? "Khóa người dùng" : "Mở khóa người dùng"}
        description={
          confirmUser?.lock
            ? "Người dùng sẽ không thể đăng nhập. Bạn có chắc không?"
            : "Người dùng sẽ có thể đăng nhập trở lại."
        }
        onConfirm={() => {
          if (!confirmUser) return;
          statusMutation.mutate({
            id: confirmUser.id,
            isActive: !confirmUser.lock,
          });
        }}
        variant={confirmUser?.lock ? "destructive" : "default"}
      />
    </AdminLayout>
  );
};

export default UsersPage;
