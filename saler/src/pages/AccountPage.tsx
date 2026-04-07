import { useMemo, useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { KeyRound, UserRound } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/contexts/AuthContext";
import { getMyAccountApi, updateMyPasswordApi } from "@/services/api";

function parseErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof Error)) return fallback;

  const raw = error.message?.trim();
  if (!raw) return fallback;

  try {
    const parsed = JSON.parse(raw) as { message?: string };
    if (parsed?.message) return parsed.message;
  } catch {
    // ignore JSON parse failure
  }

  return raw;
}

export default function AccountPage() {
  const { user } = useAuth();
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const userId = user?.user_id ?? 0;

  const {
    data: account,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["saler", "account", userId],
    queryFn: () => getMyAccountApi(userId),
    enabled: userId > 0,
    staleTime: 60_000,
  });

  const changePasswordMutation = useMutation({
    mutationFn: () => updateMyPasswordApi(userId, oldPassword.trim(), newPassword.trim()),
    onSuccess: () => {
      toast.success("Doi mat khau thanh cong");
      setOldPassword("");
      setNewPassword("");
      setConfirmPassword("");
    },
    onError: (error) => {
      toast.error(parseErrorMessage(error, "Khong the doi mat khau"));
    },
  });

  const roleLabel = useMemo(() => {
    const role = (account?.role ?? user?.role ?? "").toLowerCase();
    if (role === "admin") return "Quan tri vien";
    if (role === "saler") return "Nguoi ban";
    return role || "-";
  }, [account?.role, user?.role]);

  const handleChangePassword = () => {
    if (!oldPassword.trim()) {
      toast.error("Vui long nhap mat khau cu");
      return;
    }

    if (!newPassword.trim()) {
      toast.error("Vui long nhap mat khau moi");
      return;
    }

    if (newPassword.trim().length < 6) {
      toast.error("Mat khau moi phai tu 6 ky tu");
      return;
    }

    if (newPassword !== confirmPassword) {
      toast.error("Nhap lai mat khau moi khong khop");
      return;
    }

    if (oldPassword === newPassword) {
      toast.error("Mat khau moi khong duoc trung mat khau cu");
      return;
    }

    changePasswordMutation.mutate();
  };

  return (
    <div className="mx-auto max-w-3xl animate-fade-in space-y-6">
      <div className="page-header">
        <h1 className="page-title">Tai khoan</h1>
        <p className="page-description">Xem thong tin tai khoan va doi mat khau</p>
      </div>

      <div className="form-section space-y-4">
        <h3 className="flex items-center gap-2 font-medium text-foreground">
          <UserRound className="h-4 w-4 text-primary" /> Thong tin tai khoan
        </h3>

        {isLoading && <p className="text-sm text-muted-foreground">Dang tai thong tin...</p>}

        {isError && (
          <p className="text-sm text-destructive">Khong the tai thong tin tai khoan.</p>
        )}

        {!isLoading && !isError && (
          <div className="grid gap-4 text-sm md:grid-cols-2">
            <div>
              <p className="text-muted-foreground">Ten dang nhap</p>
              <p className="font-medium">{account?.username ?? user?.username ?? "-"}</p>
            </div>
            <div>
              <p className="text-muted-foreground">Vai tro</p>
              <p className="font-medium">{roleLabel}</p>
            </div>
            <div>
              <p className="text-muted-foreground">So dien thoai</p>
              <p className="font-medium">{account?.phone || "-"}</p>
            </div>
            <div>
              <p className="text-muted-foreground">Email</p>
              <p className="font-medium">{account?.email || "-"}</p>
            </div>
          </div>
        )}
      </div>

      <div className="form-section space-y-4">
        <h3 className="flex items-center gap-2 font-medium text-foreground">
          <KeyRound className="h-4 w-4 text-primary" /> Doi mat khau
        </h3>

        <div className="space-y-3">
          <div>
            <Label className="text-xs">Mat khau cu</Label>
            <Input
              type="password"
              className="mt-1"
              value={oldPassword}
              onChange={(e) => setOldPassword(e.target.value)}
              autoComplete="current-password"
            />
          </div>

          <div>
            <Label className="text-xs">Mat khau moi</Label>
            <Input
              type="password"
              className="mt-1"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              autoComplete="new-password"
            />
          </div>

          <div>
            <Label className="text-xs">Nhap lai mat khau moi</Label>
            <Input
              type="password"
              className="mt-1"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              autoComplete="new-password"
            />
          </div>

          <Button
            onClick={handleChangePassword}
            disabled={changePasswordMutation.isPending || userId <= 0}
          >
            {changePasswordMutation.isPending ? "Dang cap nhat..." : "Cap nhat mat khau"}
          </Button>
        </div>
      </div>
    </div>
  );
}
