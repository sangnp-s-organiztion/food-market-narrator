import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { KeyRound, Pencil, UserRound, X } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/contexts/AuthContext";
import { getMyAccountApi, updateMyPasswordApi, updateMyProfileApi } from "@/services/api";

const PHONE_REGEX = /^0\d{9,10}$/;
const EMAIL_REGEX = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

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
  const qc = useQueryClient();
  const { user, refreshMe } = useAuth();
  const [isEditingProfile, setIsEditingProfile] = useState(false);
  const [profileForm, setProfileForm] = useState({
    username: "",
    phone: "",
    email: "",
  });
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
      toast.success("Đổi mật khẩu thành công");
      setOldPassword("");
      setNewPassword("");
      setConfirmPassword("");
    },
    onError: (error) => {
      toast.error(parseErrorMessage(error, "Không thể đổi mật khẩu"));
    },
  });

  const updateProfileMutation = useMutation({
    mutationFn: () =>
      updateMyProfileApi({
        username: profileForm.username.trim(),
        phone: profileForm.phone.trim(),
        email: profileForm.email.trim(),
      }),
    onSuccess: async () => {
      toast.success("Cập nhật thông tin tài khoản thành công");
      await refreshMe();
      qc.invalidateQueries({ queryKey: ["saler", "account", userId] });
      setIsEditingProfile(false);
    },
    onError: (error) => {
      toast.error(parseErrorMessage(error, "Không thể cập nhật thông tin tài khoản"));
    },
  });

  const roleLabel = useMemo(() => {
    const role = (account?.role ?? user?.role ?? "").toLowerCase();
    if (role === "admin") return "Quản trị viên";
    if (role === "saler") return "Người bán";
    return role || "-";
  }, [account?.role, user?.role]);

  const handleChangePassword = () => {
    if (!oldPassword.trim()) {
      toast.error("Vui lòng nhập mật khẩu cũ");
      return;
    }

    if (!newPassword.trim()) {
      toast.error("Vui lòng nhập mật khẩu mới");
      return;
    }

    if (newPassword.trim().length < 6) {
      toast.error("Mật khẩu mới phải từ 6 ký tự");
      return;
    }

    if (newPassword !== confirmPassword) {
      toast.error("Nhập lại mật khẩu mới không khớp");
      return;
    }

    if (oldPassword === newPassword) {
      toast.error("Mật khẩu mới không được trùng mật khẩu cũ");
      return;
    }

    changePasswordMutation.mutate();
  };

  const startEditProfile = () => {
    setProfileForm({
      username: account?.username ?? user?.username ?? "",
      phone: account?.phone ?? "",
      email: account?.email ?? "",
    });
    setIsEditingProfile(true);
  };

  const handleSaveProfile = () => {
    if (!profileForm.username.trim()) {
      toast.error("Vui lòng nhập tên đăng nhập");
      return;
    }

    if (!PHONE_REGEX.test(profileForm.phone.trim())) {
      toast.error("Số điện thoại không hợp lệ (bắt đầu bằng 0, gồm 10-11 số)");
      return;
    }

    if (!EMAIL_REGEX.test(profileForm.email.trim())) {
      toast.error("Email không hợp lệ");
      return;
    }

    updateProfileMutation.mutate();
  };

  return (
    <div className="mx-auto max-w-3xl animate-fade-in space-y-6">
      <div className="page-header">
        <h1 className="page-title">Tài khoản</h1>
        <p className="page-description">Xem thông tin tài khoản và đổi mật khẩu</p>
      </div>

      <div className="form-section space-y-4">
        <div className="flex items-start justify-between gap-3">
          <h3 className="flex items-center gap-2 font-medium text-foreground">
            <UserRound className="h-4 w-4 text-primary" /> Thông tin tài khoản
          </h3>
          {!isEditingProfile ? (
            <Button size="sm" variant="outline" onClick={startEditProfile}>
              <Pencil className="mr-1.5 h-4 w-4" />
              Chỉnh sửa
            </Button>
          ) : (
            <Button size="sm" variant="ghost" onClick={() => setIsEditingProfile(false)}>
              <X className="mr-1.5 h-4 w-4" />
              Hủy
            </Button>
          )}
        </div>

        {isLoading && <p className="text-sm text-muted-foreground">Đang tải thông tin...</p>}

        {isError && (
          <p className="text-sm text-destructive">Không thể tải thông tin tài khoản.</p>
        )}

        {!isLoading && !isError && !isEditingProfile && (
          <div className="grid gap-4 text-sm md:grid-cols-2">
            <div>
              <p className="text-muted-foreground">Tên đăng nhập</p>
              <p className="font-medium">{account?.username ?? user?.username ?? "-"}</p>
            </div>
            <div>
              <p className="text-muted-foreground">Vai trò</p>
              <p className="font-medium">{roleLabel}</p>
            </div>
            <div>
              <p className="text-muted-foreground">Số điện thoại</p>
              <p className="font-medium">{account?.phone || "-"}</p>
            </div>
            <div>
              <p className="text-muted-foreground">Email</p>
              <p className="font-medium">{account?.email || "-"}</p>
            </div>
          </div>
        )}

        {!isLoading && !isError && isEditingProfile && (
          <div className="space-y-3">
            <div>
              <Label className="text-xs">Tên đăng nhập</Label>
              <Input
                className="mt-1"
                value={profileForm.username}
                onChange={(e) =>
                  setProfileForm({ ...profileForm, username: e.target.value })
                }
              />
            </div>
            <div>
              <Label className="text-xs">Số điện thoại</Label>
              <Input
                className="mt-1"
                value={profileForm.phone}
                onChange={(e) => setProfileForm({ ...profileForm, phone: e.target.value })}
              />
            </div>
            <div>
              <Label className="text-xs">Email</Label>
              <Input
                className="mt-1"
                value={profileForm.email}
                onChange={(e) => setProfileForm({ ...profileForm, email: e.target.value })}
              />
            </div>
            <Button onClick={handleSaveProfile} disabled={updateProfileMutation.isPending}>
              {updateProfileMutation.isPending ? "Đang cập nhật..." : "Lưu thông tin"}
            </Button>
          </div>
        )}
      </div>

      <div className="form-section space-y-4">
        <h3 className="flex items-center gap-2 font-medium text-foreground">
          <KeyRound className="h-4 w-4 text-primary" /> Đổi mật khẩu
        </h3>

        <div className="space-y-3">
          <div>
            <Label className="text-xs">Mật khẩu cũ</Label>
            <Input
              type="password"
              className="mt-1"
              value={oldPassword}
              onChange={(e) => setOldPassword(e.target.value)}
              autoComplete="current-password"
            />
          </div>

          <div>
            <Label className="text-xs">Mật khẩu mới</Label>
            <Input
              type="password"
              className="mt-1"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              autoComplete="new-password"
            />
          </div>

          <div>
            <Label className="text-xs">Nhập lại mật khẩu mới</Label>
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
            {changePasswordMutation.isPending ? "Đang cập nhật..." : "Cập nhật mật khẩu"}
          </Button>
        </div>
      </div>
    </div>
  );
}
