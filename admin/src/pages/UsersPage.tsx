import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import {
  userApi,
  type UserResponse,
  type CreateUserRequest,
} from "@/lib/adminApi";
import { Plus, Lock, Unlock, Shield } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "sonner";
import ConfirmDialog from "@/components/ConfirmDialog";

// Map API response → page-local shape
function toPageUser(r: UserResponse) {
  const normalizedRole = (r.role ?? "").toLowerCase();
  return {
    user_id: r.userId,
    username: r.username,
    role: normalizedRole === "admin" ? ("admin" as const) : ("saler" as const),
    is_active: r.isActive,
    created_at: r.createdAt ? r.createdAt.split("T")[0] : "",
  };
}

const UsersPage = () => {
  const qc = useQueryClient();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [form, setForm] = useState<{
    username: string;
    password: string;
    confirmPassword: string;
    role: "admin" | "saler";
  }>({ username: "", password: "", confirmPassword: "", role: "saler" });
  const [confirmUser, setConfirmUser] = useState<{
    id: number;
    name: string;
    lock: boolean;
  } | null>(null);

  // ── Fetch ──────────────────────────────────────────────────────────────────
  const {
    data: apiUsers = [],
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["admin", "users"],
    queryFn: userApi.getAll,
    staleTime: 60_000,
  });

  const pageUsers = apiUsers.map(toPageUser);

  // ── Mutations ────────────────────────────────────────────────────────────────

  const createMutation = useMutation({
    mutationFn: (data: CreateUserRequest) => userApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Tạo người dùng thành công");
      setDialogOpen(false);
      setForm({ username: "", password: "", confirmPassword: "", role: "saler" });
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

    createMutation.mutate({
      username: form.username.trim(),
      password: form.password.trim(),
      role: form.role,
    });
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Quản lý người dùng</h1>
        <Button
          onClick={() => {
            setForm({ username: "", password: "", confirmPassword: "", role: "saler" });
            setDialogOpen(true);
          }}
          size="sm"
        >
          <Plus className="h-4 w-4 mr-1.5" />
          Tạo người dùng
        </Button>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Tên đăng nhập</th>
                <th>Vai trò</th>
                <th>Trạng thái</th>
                <th>Ngày tạo</th>
                <th className="w-32">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td
                    colSpan={5}
                    className="text-center py-8 text-muted-foreground"
                  >
                    Đang tải…
                  </td>
                </tr>
              )}
              {isError && (
                <tr>
                  <td colSpan={5} className="text-center py-8 text-destructive">
                    Không thể tải danh sách người dùng. Vui lòng thử lại.
                  </td>
                </tr>
              )}
              {!isLoading && !isError && pageUsers.length === 0 && (
                <tr>
                  <td
                    colSpan={5}
                    className="text-center py-8 text-muted-foreground"
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
                    <td>
                      <span
                        className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium ${
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
                      {/* Lock / unlock */}
                      <button
                        onClick={() =>
                          setConfirmUser({
                            id: u.user_id,
                            name: u.username,
                            lock: u.is_active,
                          })
                        }
                        className={`p-1.5 rounded-md hover:bg-muted transition-colors ${
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
      </div>

      {/* Create user dialog */}
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
              {createMutation.isPending ? "Đang tạo…" : "Tạo mới"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      {/* Confirm lock/unlock */}
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
