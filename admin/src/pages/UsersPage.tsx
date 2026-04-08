import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Lock, Plus, Shield, Unlock } from "lucide-react";
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
import { userApi, type CreateUserRequest, type UserResponse } from "@/lib/adminApi";
import { toast } from "sonner";

const PHONE_REGEX = /^0\d{9,10}$/;
const EMAIL_REGEX = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

function toPageUser(r: UserResponse) {
  const normalizedRole = (r.role ?? "").toLowerCase();
  return {
    user_id: r.userId,
    username: r.username,
    phone: r.phone ?? "",
    email: r.email ?? "",
    role: normalizedRole === "admin" ? ("admin" as const) : ("saler" as const),
    is_active: r.isActive,
    created_at: r.createdAt ? r.createdAt.split("T")[0] : "",
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

  const createMutation = useMutation({
    mutationFn: (data: CreateUserRequest) => userApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Tao nguoi dung thanh cong");
      setDialogOpen(false);
      setForm(emptyForm);
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Tao nguoi dung that bai");
    },
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: number; isActive: boolean }) =>
      userApi.updateStatus(id, { isActive }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      toast.success("Cap nhat trang thai thanh cong");
      setConfirmUser(null);
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Cap nhat that bai");
    },
  });

  const handleCreate = () => {
    if (!form.username.trim()) {
      toast.error("Vui long nhap ten dang nhap");
      return;
    }

    if (!form.password.trim()) {
      toast.error("Vui long nhap mat khau");
      return;
    }

    if (form.password !== form.confirmPassword) {
      toast.error("Mat khau nhap lai khong khop");
      return;
    }

    if (!PHONE_REGEX.test(form.phone.trim())) {
      toast.error("So dien thoai khong hop le (bat dau bang 0, gom 10-11 so)");
      return;
    }

    if (!EMAIL_REGEX.test(form.email.trim())) {
      toast.error("Email khong hop le");
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
        <h1 className="page-title">Quan ly nguoi dung</h1>
        <Button
          onClick={() => {
            setForm(emptyForm);
            setDialogOpen(true);
          }}
          size="sm"
        >
          <Plus className="mr-1.5 h-4 w-4" />
          Tao nguoi dung
        </Button>
      </div>

      <div className="mx-auto max-w-7xl px-8 py-6">
        <div className="stat-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Ten dang nhap</th>
                <th>So dien thoai</th>
                <th>Email</th>
                <th>Vai tro</th>
                <th>Trang thai</th>
                <th>Ngay tao</th>
                <th className="w-32">Hanh dong</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-muted-foreground">
                    Dang tai...
                  </td>
                </tr>
              )}

              {isError && (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-destructive">
                    Khong the tai danh sach nguoi dung. Vui long thu lai.
                  </td>
                </tr>
              )}

              {!isLoading && !isError && pageUsers.length === 0 && (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-muted-foreground">
                    Chua co nguoi dung nao.
                  </td>
                </tr>
              )}

              {!isLoading &&
                !isError &&
                pageUsers.map((u) => (
                  <tr key={u.user_id}>
                    <td className="font-medium">{u.username}</td>
                    <td className="mono text-xs text-muted-foreground">{u.phone || "-"}</td>
                    <td className="text-xs text-muted-foreground">{u.email || "-"}</td>
                    <td>
                      <span
                        className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${
                          u.role === "admin"
                            ? "bg-primary/10 text-primary"
                            : "bg-muted text-muted-foreground"
                        }`}
                      >
                        <Shield className="h-3 w-3" />
                        {u.role === "admin" ? "Quan tri vien" : "Nguoi ban"}
                      </span>
                    </td>
                    <td>
                      <span className={u.is_active ? "status-active" : "status-inactive"}>
                        {u.is_active ? "Hoat dong" : "Ngung hoat dong"}
                      </span>
                    </td>
                    <td className="mono text-xs text-muted-foreground">{u.created_at}</td>
                    <td className="flex items-center gap-1">
                      <button
                        onClick={() =>
                          setConfirmUser({
                            id: u.user_id,
                            name: u.username,
                            lock: u.is_active,
                          })
                        }
                        className={`rounded-md p-1.5 transition-colors hover:bg-muted ${
                          !u.is_active ? "text-destructive" : "text-muted-foreground"
                        }`}
                        title={u.is_active ? "Khoa nguoi dung" : "Mo khoa nguoi dung"}
                      >
                        {u.is_active ? <Unlock className="h-4 w-4" /> : <Lock className="h-4 w-4" />}
                      </button>
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
            <DialogTitle>Tao nguoi dung moi</DialogTitle>
          </DialogHeader>
          <div className="grid gap-3 py-2">
            <div>
              <Label className="text-xs">Ten dang nhap</Label>
              <Input
                value={form.username}
                onChange={(e) => setForm({ ...form, username: e.target.value })}
                className="mt-1"
                placeholder="username"
                autoComplete="username"
              />
            </div>
            <div>
              <Label className="text-xs">So dien thoai</Label>
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
              <Label className="text-xs">Mat khau</Label>
              <Input
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                className="mt-1"
                placeholder="Nhap mat khau"
                autoComplete="new-password"
              />
            </div>
            <div>
              <Label className="text-xs">Nhap lai mat khau</Label>
              <Input
                type="password"
                value={form.confirmPassword}
                onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
                className="mt-1"
                placeholder="Nhap lai mat khau"
                autoComplete="new-password"
              />
            </div>
            <div>
              <Label className="text-xs">Vai tro</Label>
              <Select
                value={form.role}
                onValueChange={(v) => setForm({ ...form, role: v as "admin" | "saler" })}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="admin">Quan tri vien</SelectItem>
                  <SelectItem value="saler">Nguoi ban</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button onClick={handleCreate} className="mt-2" disabled={createMutation.isPending}>
              {createMutation.isPending ? "Dang tao..." : "Tao moi"}
            </Button>
          </div>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!confirmUser}
        onOpenChange={(open) => !open && setConfirmUser(null)}
        title={confirmUser?.lock ? "Khoa nguoi dung" : "Mo khoa nguoi dung"}
        description={
          confirmUser?.lock
            ? "Nguoi dung se khong the dang nhap. Ban co chac khong?"
            : "Nguoi dung se co the dang nhap tro lai."
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
