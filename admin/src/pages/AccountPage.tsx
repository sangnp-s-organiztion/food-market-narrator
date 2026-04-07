import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import AdminLayout from "@/components/AdminLayout";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/contexts/AuthContext";
import { userApi } from "@/lib/adminApi";
import { toast } from "sonner";

const AccountPage = () => {
  const { user } = useAuth();
  const [form, setForm] = useState({
    oldPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });

  const userId = user?.userId ?? 0;

  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin", "account", userId],
    queryFn: () => userApi.getById(userId),
    enabled: userId > 0,
    staleTime: 60_000,
  });

  const changePasswordMutation = useMutation({
    mutationFn: () =>
      userApi.updateMyPassword({
        oldPassword: form.oldPassword.trim(),
        newPassword: form.newPassword.trim(),
      }),
    onSuccess: () => {
      toast.success("Doi mat khau thanh cong");
      setForm({ oldPassword: "", newPassword: "", confirmNewPassword: "" });
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Doi mat khau that bai");
    },
  });

  const handleChangePassword = () => {
    if (!form.oldPassword.trim()) {
      toast.error("Vui long nhap mat khau cu");
      return;
    }

    if (!form.newPassword.trim()) {
      toast.error("Vui long nhap mat khau moi");
      return;
    }

    if (form.newPassword.trim().length < 6) {
      toast.error("Mat khau moi phai co it nhat 6 ky tu");
      return;
    }

    if (form.newPassword !== form.confirmNewPassword) {
      toast.error("Nhap lai mat khau moi khong khop");
      return;
    }

    changePasswordMutation.mutate();
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Tai khoan</h1>
      </div>

      <div className="mx-auto grid max-w-4xl gap-6 px-8 py-6">
        <section className="stat-card p-6">
          <h2 className="mb-4 text-base font-semibold">Thong tin tai khoan hien tai</h2>

          {isLoading && <p className="text-sm text-muted-foreground">Dang tai thong tin...</p>}
          {isError && (
            <p className="text-sm text-destructive">Khong the tai thong tin tai khoan.</p>
          )}

          {!isLoading && !isError && data && (
            <div className="grid gap-3 text-sm sm:grid-cols-2">
              <div>
                <p className="text-muted-foreground">Ten dang nhap</p>
                <p className="font-medium">{data.username}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Vai tro</p>
                <p className="font-medium">{data.role}</p>
              </div>
              <div>
                <p className="text-muted-foreground">So dien thoai</p>
                <p className="font-medium">{data.phone || "-"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Email</p>
                <p className="font-medium">{data.email || "-"}</p>
              </div>
            </div>
          )}
        </section>

        <section className="stat-card p-6">
          <h2 className="mb-4 text-base font-semibold">Doi mat khau</h2>
          <div className="grid gap-3">
            <div>
              <Label className="text-xs">Mat khau cu</Label>
              <Input
                type="password"
                className="mt-1"
                value={form.oldPassword}
                onChange={(e) => setForm({ ...form, oldPassword: e.target.value })}
                autoComplete="current-password"
              />
            </div>
            <div>
              <Label className="text-xs">Mat khau moi</Label>
              <Input
                type="password"
                className="mt-1"
                value={form.newPassword}
                onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
                autoComplete="new-password"
              />
            </div>
            <div>
              <Label className="text-xs">Nhap lai mat khau moi</Label>
              <Input
                type="password"
                className="mt-1"
                value={form.confirmNewPassword}
                onChange={(e) => setForm({ ...form, confirmNewPassword: e.target.value })}
                autoComplete="new-password"
              />
            </div>
            <Button
              className="mt-2 w-fit"
              onClick={handleChangePassword}
              disabled={changePasswordMutation.isPending || userId <= 0}
            >
              {changePasswordMutation.isPending ? "Dang cap nhat..." : "Cap nhat mat khau"}
            </Button>
          </div>
        </section>
      </div>
    </AdminLayout>
  );
};

export default AccountPage;
