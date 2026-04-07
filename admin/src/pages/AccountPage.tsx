import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, X } from "lucide-react";
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
    phone: "",
    email: "",
  });
  const [passwordForm, setPasswordForm] = useState({
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

  const updateProfileMutation = useMutation({
    mutationFn: () =>
      userApi.updateMyProfile({
        username: profileForm.username.trim(),
        phone: profileForm.phone.trim(),
        email: profileForm.email.trim(),
      }),
    onSuccess: async () => {
      toast.success("Cap nhat thong tin tai khoan thanh cong");
      await refreshMe();
      qc.invalidateQueries({ queryKey: ["admin", "account", userId] });
      qc.invalidateQueries({ queryKey: ["admin", "users"] });
      setIsEditingProfile(false);
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Cap nhat thong tin that bai");
    },
  });

  const changePasswordMutation = useMutation({
    mutationFn: () =>
      userApi.updateMyPassword({
        oldPassword: passwordForm.oldPassword.trim(),
        newPassword: passwordForm.newPassword.trim(),
      }),
    onSuccess: () => {
      toast.success("Doi mat khau thanh cong");
      setPasswordForm({ oldPassword: "", newPassword: "", confirmNewPassword: "" });
    },
    onError: (err: Error) => {
      toast.error(err.message ?? "Doi mat khau that bai");
    },
  });

  const startEditProfile = () => {
    setProfileForm({
      username: data?.username ?? "",
      phone: data?.phone ?? "",
      email: data?.email ?? "",
    });
    setIsEditingProfile(true);
  };

  const handleSaveProfile = () => {
    if (!profileForm.username.trim()) {
      toast.error("Vui long nhap ten dang nhap");
      return;
    }

    if (!PHONE_REGEX.test(profileForm.phone.trim())) {
      toast.error("So dien thoai khong hop le (bat dau bang 0, gom 10-11 so)");
      return;
    }

    if (!EMAIL_REGEX.test(profileForm.email.trim())) {
      toast.error("Email khong hop le");
      return;
    }

    updateProfileMutation.mutate();
  };

  const handleChangePassword = () => {
    if (!passwordForm.oldPassword.trim()) {
      toast.error("Vui long nhap mat khau cu");
      return;
    }

    if (!passwordForm.newPassword.trim()) {
      toast.error("Vui long nhap mat khau moi");
      return;
    }

    if (passwordForm.newPassword.trim().length < 6) {
      toast.error("Mat khau moi phai co it nhat 6 ky tu");
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
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
          <div className="mb-4 flex items-start justify-between gap-3">
            <h2 className="text-base font-semibold">Thong tin tai khoan hien tai</h2>
            {!isEditingProfile ? (
              <Button size="sm" variant="outline" onClick={startEditProfile}>
                <Pencil className="mr-1.5 h-4 w-4" />
                Chinh sua
              </Button>
            ) : (
              <Button
                size="sm"
                variant="ghost"
                onClick={() => setIsEditingProfile(false)}
              >
                <X className="mr-1.5 h-4 w-4" />
                Huy
              </Button>
            )}
          </div>

          {isLoading && <p className="text-sm text-muted-foreground">Dang tai thong tin...</p>}
          {isError && (
            <p className="text-sm text-destructive">Khong the tai thong tin tai khoan.</p>
          )}

          {!isLoading && !isError && data && !isEditingProfile && (
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

          {!isLoading && !isError && data && isEditingProfile && (
            <div className="grid gap-3">
              <div>
                <Label className="text-xs">Ten dang nhap</Label>
                <Input
                  className="mt-1"
                  value={profileForm.username}
                  onChange={(e) => setProfileForm({ ...profileForm, username: e.target.value })}
                />
              </div>
              <div>
                <Label className="text-xs">So dien thoai</Label>
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
              <Button
                className="mt-2 w-fit"
                onClick={handleSaveProfile}
                disabled={updateProfileMutation.isPending}
              >
                {updateProfileMutation.isPending ? "Dang cap nhat..." : "Luu thong tin"}
              </Button>
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
                value={passwordForm.oldPassword}
                onChange={(e) => setPasswordForm({ ...passwordForm, oldPassword: e.target.value })}
                autoComplete="current-password"
              />
            </div>
            <div>
              <Label className="text-xs">Mat khau moi</Label>
              <Input
                type="password"
                className="mt-1"
                value={passwordForm.newPassword}
                onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                autoComplete="new-password"
              />
            </div>
            <div>
              <Label className="text-xs">Nhap lai mat khau moi</Label>
              <Input
                type="password"
                className="mt-1"
                value={passwordForm.confirmNewPassword}
                onChange={(e) =>
                  setPasswordForm({ ...passwordForm, confirmNewPassword: e.target.value })
                }
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
