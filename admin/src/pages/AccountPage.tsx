import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, EyeOff, Pencil, X } from "lucide-react";
import AdminLayout from "@/components/AdminLayout";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/contexts/AuthContext";
import { userApi } from "@/lib/adminApi";
import { toast } from "sonner";

const PHONE_REGEX = /^0\d{9,10}$/;
const EMAIL_REGEX = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;

const AccountPage = () => {
  const qc = useQueryClient();
  const { user, refreshMe } = useAuth();

  const [isEditingProfile, setIsEditingProfile] = useState(false);
  const [profileForm, setProfileForm] = useState({
    username: "",
    fullName: "",
    phone: "",
    email: "",
  });
  const [passwordForm, setPasswordForm] = useState({
    oldPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [showOldPassword, setShowOldPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmNewPassword, setShowConfirmNewPassword] = useState(false);

  const userId = user?.userId ?? 0;

  const { data, isLoading, isError } = useQuery({
    queryKey: ["admin", "account", userId],
    queryFn: () => userApi.getById(userId),
    enabled: userId > 0,
    staleTime: 60_000,
  });

  const updateProfileMutation = useMutation({
    mutationFn: () =>
      userApi.updateMyProfile({
        username: profileForm.username.trim(),
        fullName: profileForm.fullName.trim() || null,
        phone: profileForm.phone.trim(),
        email: profileForm.email.trim(),
      }),
    onSuccess: async () => {
      toast.success("Cập nhật thông tin tài khoản thành công");
      await refreshMe();
      qc.invalidateQueries({ queryKey: ["admin", "account", userId] });
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      setIsEditingProfile(false);
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Cập nhật thông tin thất bại");
    },
  });

  const changePasswordMutation = useMutation({
    mutationFn: () =>
      userApi.updateMyPassword({
        oldPassword: passwordForm.oldPassword.trim(),
        newPassword: passwordForm.newPassword.trim(),
      }),
    onSuccess: () => {
      toast.success("Đổi mật khẩu thành công");
      setPasswordForm({
        oldPassword: "",
        newPassword: "",
        confirmNewPassword: "",
      });
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Đổi mật khẩu thất bại");
    },
  });

  const startEditProfile = () => {
    setProfileForm({
      username: data?.username ?? "",
      fullName: data?.fullName ?? "",
      phone: data?.phone ?? "",
      email: data?.email ?? "",
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

  const handleChangePassword = () => {
    if (!passwordForm.oldPassword.trim()) {
      toast.error("Vui lòng nhập mật khẩu cũ");
      return;
    }

    if (!passwordForm.newPassword.trim()) {
      toast.error("Vui lòng nhập mật khẩu mới");
      return;
    }

    if (passwordForm.newPassword.trim().length < 6) {
      toast.error("Mật khẩu mới phải có ít nhất 6 ký tự");
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
      toast.error("Nhập lại mật khẩu mới không khớp");
      return;
    }

    changePasswordMutation.mutate();
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Tài khoản</h1>
      </div>

      <div className="mx-auto grid max-w-4xl gap-6 px-8 py-6">
        <section className="stat-card p-6">
          <div className="mb-4 flex items-start justify-between gap-3">
            <h2 className="text-base font-semibold">
              Thông tin tài khoản hiện tại
            </h2>
            {!isEditingProfile ? (
              <Button size="sm" variant="outline" onClick={startEditProfile}>
                <Pencil className="mr-1.5 h-4 w-4" />
                Chỉnh sửa
              </Button>
            ) : (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setIsEditingProfile(false)}
              >
                <X className="mr-1.5 h-4 w-4" />
                Hủy
              </Button>
            )}
          </div>

          {isLoading && (
            <p className="text-sm text-muted-foreground">
              Đang tải thông tin...
            </p>
          )}
          {isError && (
            <p className="text-sm text-destructive">
              Không thể tải thông tin tài khoản.
            </p>
          )}

          {!isLoading && !isError && data && !isEditingProfile && (
            <div className="grid gap-3 text-sm sm:grid-cols-2">
              <div>
                <p className="text-muted-foreground">Họ và tên</p>
                <p className="font-medium">{data.fullName || "-"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Tên đăng nhập</p>
                <p className="font-medium">{data.username}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Vai trò</p>
                <p className="font-medium">{data.role}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Số điện thoại</p>
                <p className="font-medium">{data.phone || "-"}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Email</p>
                <p className="font-medium">{data.email || "-"}</p>
              </div>
            </div>
          )}

          {!isLoading && !isError && data && isEditingProfile && (
            <div className="grid gap-3">
              <div>
                <Label className="text-xs">Họ và tên</Label>
                <Input
                  className="mt-1"
                  value={profileForm.fullName}
                  onChange={(e) =>
                    setProfileForm({ ...profileForm, fullName: e.target.value })
                  }
                />
              </div>
              <div>
                <Label className="text-xs">Tên đăng nhập</Label>
                <Input
                  className="mt-1"
                  value={profileForm.username}
                  disabled
                  readOnly
                />
              </div>
              <div>
                <Label className="text-xs">Số điện thoại</Label>
                <Input
                  className="mt-1"
                  value={profileForm.phone}
                  onChange={(e) =>
                    setProfileForm({ ...profileForm, phone: e.target.value })
                  }
                />
              </div>
              <div>
                <Label className="text-xs">Email</Label>
                <Input
                  className="mt-1"
                  value={profileForm.email}
                  onChange={(e) =>
                    setProfileForm({ ...profileForm, email: e.target.value })
                  }
                />
              </div>
              <Button
                className="mt-2 w-fit"
                onClick={handleSaveProfile}
                disabled={updateProfileMutation.isPending}
              >
                {updateProfileMutation.isPending
                  ? "Đang cập nhật..."
                  : "Lưu thông tin"}
              </Button>
            </div>
          )}
        </section>

        <section className="stat-card p-6">
          <h2 className="mb-4 text-base font-semibold">Đổi mật khẩu</h2>
          <div className="grid gap-3">
            <div>
              <Label className="text-xs">Mật khẩu cũ</Label>
              <div className="relative mt-1">
                <Input
                  type={showOldPassword ? "text" : "password"}
                  className="pr-11"
                  value={passwordForm.oldPassword}
                  onChange={(e) =>
                    setPasswordForm({
                      ...passwordForm,
                      oldPassword: e.target.value,
                    })
                  }
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  onClick={() => setShowOldPassword((prev) => !prev)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  aria-label={
                    showOldPassword ? "Ẩn mật khẩu cũ" : "Hiện mật khẩu cũ"
                  }
                >
                  {showOldPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              </div>
            </div>
            <div>
              <Label className="text-xs">Mật khẩu mới</Label>
              <div className="relative mt-1">
                <Input
                  type={showNewPassword ? "text" : "password"}
                  className="pr-11"
                  value={passwordForm.newPassword}
                  onChange={(e) =>
                    setPasswordForm({
                      ...passwordForm,
                      newPassword: e.target.value,
                    })
                  }
                  autoComplete="new-password"
                />
                <button
                  type="button"
                  onClick={() => setShowNewPassword((prev) => !prev)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  aria-label={
                    showNewPassword ? "Ẩn mật khẩu mới" : "Hiện mật khẩu mới"
                  }
                >
                  {showNewPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              </div>
            </div>
            <div>
              <Label className="text-xs">Nhập lại mật khẩu mới</Label>
              <div className="relative mt-1">
                <Input
                  type={showConfirmNewPassword ? "text" : "password"}
                  className="pr-11"
                  value={passwordForm.confirmNewPassword}
                  onChange={(e) =>
                    setPasswordForm({
                      ...passwordForm,
                      confirmNewPassword: e.target.value,
                    })
                  }
                  autoComplete="new-password"
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmNewPassword((prev) => !prev)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  aria-label={
                    showConfirmNewPassword
                      ? "Ẩn xác nhận mật khẩu mới"
                      : "Hiện xác nhận mật khẩu mới"
                  }
                >
                  {showConfirmNewPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              </div>
            </div>
            <Button
              className="mt-2 w-fit"
              onClick={handleChangePassword}
              disabled={changePasswordMutation.isPending || userId <= 0}
            >
              {changePasswordMutation.isPending
                ? "Đang cập nhật..."
                : "Cập nhật mật khẩu"}
            </Button>
          </div>
        </section>
      </div>
    </AdminLayout>
  );
};

export default AccountPage;
