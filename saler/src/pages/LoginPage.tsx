import { useAuth } from "@/contexts/AuthContext";
import { useEffect, useState } from "react";
import {
  forgotPasswordResetApi,
  forgotPasswordSendOtpApi,
  forgotPasswordVerifyOtpApi,
} from "@/services/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import { UtensilsCrossed, Eye, EyeOff } from "lucide-react";

function parseErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof Error)) return fallback;

  const raw = error.message?.trim();
  if (!raw) return fallback;

  try {
    const parsed = JSON.parse(raw) as { message?: string };
    return parsed.message?.trim() || fallback;
  } catch {
    return raw;
  }
}

function formatCountdown(totalSeconds: number): string {
  const safeSeconds = Math.max(0, totalSeconds);
  const minutes = Math.floor(safeSeconds / 60)
    .toString()
    .padStart(2, "0");
  const seconds = (safeSeconds % 60).toString().padStart(2, "0");
  return `${minutes}:${seconds}`;
}

export default function LoginPage() {
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const [forgotOpen, setForgotOpen] = useState(false);
  const [forgotUsername, setForgotUsername] = useState("");
  const [forgotEmail, setForgotEmail] = useState("");
  const [forgotOtp, setForgotOtp] = useState("");
  const [forgotNewPassword, setForgotNewPassword] = useState("");
  const [forgotConfirmPassword, setForgotConfirmPassword] = useState("");
  const [forgotError, setForgotError] = useState("");
  const [forgotSuccess, setForgotSuccess] = useState("");
  const [sendingOtp, setSendingOtp] = useState(false);
  const [verifyingOtp, setVerifyingOtp] = useState(false);
  const [resettingPassword, setResettingPassword] = useState(false);
  const [otpSent, setOtpSent] = useState(false);
  const [otpVerified, setOtpVerified] = useState(false);
  const [remainingSeconds, setRemainingSeconds] = useState(0);

  const otpExpired = otpSent && remainingSeconds <= 0;

  useEffect(() => {
    if (!forgotOpen || !otpSent || remainingSeconds <= 0) {
      return;
    }

    const timer = window.setInterval(() => {
      setRemainingSeconds((previous) => {
        if (previous <= 1) {
          window.clearInterval(timer);
          return 0;
        }

        return previous - 1;
      });
    }, 1000);

    return () => window.clearInterval(timer);
  }, [forgotOpen, otpSent, remainingSeconds]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    const success = await login(username, password);
    if (!success) setError("Thông tin đăng nhập không hợp lệ. Vui lòng thử lại.");
    setLoading(false);
  };

  const resetForgotDialog = () => {
    setForgotUsername("");
    setForgotEmail("");
    setForgotOtp("");
    setForgotNewPassword("");
    setForgotConfirmPassword("");
    setForgotError("");
    setForgotSuccess("");
    setSendingOtp(false);
    setVerifyingOtp(false);
    setResettingPassword(false);
    setOtpSent(false);
    setOtpVerified(false);
    setRemainingSeconds(0);
  };

  const handleForgotDialogChange = (open: boolean) => {
    setForgotOpen(open);
    if (!open) {
      resetForgotDialog();
    }
  };

  const handleSendOtp = async () => {
    setForgotError("");
    setForgotSuccess("");

    const normalizedUsername = forgotUsername.trim();
    const normalizedEmail = forgotEmail.trim();

    if (!normalizedUsername || !normalizedEmail) {
      setForgotError("Vui lòng nhập username và gmail.");
      return;
    }

    setSendingOtp(true);
    try {
      const response = await forgotPasswordSendOtpApi(normalizedUsername, normalizedEmail);
      setOtpSent(true);
      setOtpVerified(false);
      setForgotOtp("");
      setForgotNewPassword("");
      setForgotConfirmPassword("");
      setRemainingSeconds(Math.max(0, response.expiresInSeconds || 120));
      setForgotSuccess(response.message || "Đã gửi OTP qua gmail.");
      setForgotError("");
    } catch (err) {
      setForgotError(parseErrorMessage(err, "Không thể gửi OTP."));
    } finally {
      setSendingOtp(false);
    }
  };

  const handleVerifyOtp = async () => {
    setForgotError("");
    setForgotSuccess("");

    const normalizedUsername = forgotUsername.trim();
    const normalizedEmail = forgotEmail.trim();
    const normalizedOtp = forgotOtp.trim();

    if (!otpSent) {
      setForgotError("Vui lòng gửi OTP trước.");
      return;
    }

    if (otpExpired) {
      setForgotError("Hết hạn OTP vui lòng gửi lại.");
      return;
    }

    if (!normalizedUsername || !normalizedEmail || !normalizedOtp) {
      setForgotError("Vui lòng nhập đầy đủ username, gmail và OTP.");
      return;
    }

    setVerifyingOtp(true);
    try {
      await forgotPasswordVerifyOtpApi(normalizedUsername, normalizedEmail, normalizedOtp);
      setOtpVerified(true);
      setForgotSuccess("OTP hợp lệ. Vui lòng nhập mật khẩu mới.");
    } catch (err) {
      setForgotError(parseErrorMessage(err, "Không thể xác minh OTP."));
    } finally {
      setVerifyingOtp(false);
    }
  };

  const handleResetPassword = async () => {
    setForgotError("");
    setForgotSuccess("");

    const normalizedUsername = forgotUsername.trim();
    const normalizedEmail = forgotEmail.trim();
    const normalizedOtp = forgotOtp.trim();

    if (!otpVerified) {
      setForgotError("Vui lòng nhập OTP và xác minh trước.");
      return;
    }

    if (otpExpired) {
      setForgotError("Hết hạn OTP vui lòng gửi lại.");
      return;
    }

    if (!normalizedUsername || !normalizedEmail || !normalizedOtp || !forgotNewPassword || !forgotConfirmPassword) {
      setForgotError("Vui lòng nhập đầy đủ thông tin.");
      return;
    }

    if (forgotNewPassword.length < 6) {
      setForgotError("Mật khẩu mới phải có ít nhất 6 ký tự.");
      return;
    }

    if (forgotNewPassword !== forgotConfirmPassword) {
      setForgotError("Mật khẩu xác nhận không khớp.");
      return;
    }

    setResettingPassword(true);
    try {
      await forgotPasswordResetApi(
        normalizedUsername,
        normalizedEmail,
        normalizedOtp,
        forgotNewPassword,
      );
      handleForgotDialogChange(false);
      toast.success("Đặt lại mật khẩu thành công.");
    } catch (err) {
      setForgotError(parseErrorMessage(err, "Không thể đặt lại mật khẩu."));
    } finally {
      setResettingPassword(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-4">
      <div className="w-full max-w-md animate-fade-in">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-primary/10 mb-4">
            <UtensilsCrossed className="w-8 h-8 text-primary" />
          </div>
          <h1 className="text-3xl font-display font-semibold text-foreground">Chào mừng trở lại</h1>
          <p className="text-muted-foreground mt-2">Đăng nhập để quản lý nhà hàng của bạn</p>
        </div>

        <form onSubmit={handleSubmit} className="form-section space-y-5">
          <div className="space-y-2">
            <Label htmlFor="username">Tên đăng nhập</Label>
            <Input
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Nhập tên đăng nhập"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Mật khẩu</Label>
            <div className="relative">
              <Input
                id="password"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Nhập mật khẩu"
                required
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
              >
                {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
            </div>
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <Button type="submit" className="w-full" disabled={loading}>
            {loading ? "Đang đăng nhập..." : "Đăng nhập"}
          </Button>

          <div className="flex justify-end">
            <Button
              type="button"
              variant="link"
              className="h-auto px-0 text-sm"
              onClick={() => setForgotOpen(true)}
            >
              Quên mật khẩu?
            </Button>
          </div>

        </form>
      </div>

      <Dialog open={forgotOpen} onOpenChange={handleForgotDialogChange}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Quên mật khẩu</DialogTitle>
            <DialogDescription>
              Nhập username và gmail để nhận mã OTP đặt lại mật khẩu.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-2">
            <div className="space-y-2">
              <Label htmlFor="forgot-username">Username</Label>
              <Input
                id="forgot-username"
                value={forgotUsername}
                onChange={(e) => setForgotUsername(e.target.value)}
                placeholder="Nhập username"
                disabled={resettingPassword || verifyingOtp || otpVerified}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="forgot-email">Gmail</Label>
              <Input
                id="forgot-email"
                value={forgotEmail}
                onChange={(e) => setForgotEmail(e.target.value)}
                placeholder="Nhập gmail"
                disabled={resettingPassword || verifyingOtp || otpVerified}
              />
            </div>

            <div className="flex items-center gap-3">
              <Button
                type="button"
                variant="secondary"
                onClick={handleSendOtp}
                disabled={sendingOtp || resettingPassword || verifyingOtp}
              >
                {sendingOtp ? "Đang gửi OTP..." : otpSent ? "Gửi lại OTP" : "Gửi OTP"}
              </Button>

              {otpSent && !otpExpired && (
                <p className="text-xs text-muted-foreground">
                  Mã OTP còn hiệu lực: {formatCountdown(remainingSeconds)}
                </p>
              )}

              {otpExpired && (
                <p className="text-xs text-destructive">Hết hạn OTP vui lòng gửi lại.</p>
              )}
            </div>

            {otpSent && !otpVerified && (
              <>
                <div className="space-y-2">
                  <Label htmlFor="forgot-otp">Mã OTP</Label>
                  <Input
                    id="forgot-otp"
                    value={forgotOtp}
                    onChange={(e) => setForgotOtp(e.target.value)}
                    placeholder="Nhập mã OTP"
                    disabled={resettingPassword || verifyingOtp}
                  />
                </div>

                <div className="flex justify-start">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleVerifyOtp}
                    disabled={verifyingOtp || resettingPassword}
                  >
                    {verifyingOtp ? "Đang xác minh OTP..." : "Nhập OTP"}
                  </Button>
                </div>
              </>
            )}

            {otpVerified && (
              <>
                <div className="rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
                  OTP đã xác minh thành công. Vui lòng nhập mật khẩu mới.
                </div>

                <div className="space-y-2">
                  <Label htmlFor="forgot-new-password">Mật khẩu mới</Label>
                  <Input
                    id="forgot-new-password"
                    type="password"
                    value={forgotNewPassword}
                    onChange={(e) => setForgotNewPassword(e.target.value)}
                    placeholder="Nhập mật khẩu mới"
                    disabled={resettingPassword}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="forgot-confirm-password">Xác nhận mật khẩu mới</Label>
                  <Input
                    id="forgot-confirm-password"
                    type="password"
                    value={forgotConfirmPassword}
                    onChange={(e) => setForgotConfirmPassword(e.target.value)}
                    placeholder="Nhập lại mật khẩu mới"
                    disabled={resettingPassword}
                  />
                </div>
              </>
            )}

            {forgotError && <p className="text-sm text-destructive">{forgotError}</p>}
            {forgotSuccess && <p className="text-sm text-emerald-600">{forgotSuccess}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleForgotDialogChange(false)}>
              Đóng
            </Button>
            <Button
              type="button"
              onClick={handleResetPassword}
              disabled={!otpVerified || resettingPassword}
            >
              {resettingPassword ? "Đang đặt lại..." : "Đặt lại mật khẩu"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
