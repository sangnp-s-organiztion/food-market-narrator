import { useCallback, useEffect, useRef, useState } from "react";
import { Download, QrCode as QrCodeIcon, Upload } from "lucide-react";
import { toast } from "sonner";
import AdminLayout from "@/components/AdminLayout";
import { Button } from "@/components/ui/button";

const API_BASE =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  "http://localhost:5044";
const QR_OUTPUT_FILE = "qr_open_app.png";
const STORED_QR_PATH = `/uploads/qr/${QR_OUTPUT_FILE}`;

const QrCodePage = () => {
  const [qrImageUrl, setQrImageUrl] = useState<string>("");
  const [isUploading, setIsUploading] = useState(false);
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const buildStoredQrUrl = (path = STORED_QR_PATH) =>
    `${API_BASE}${path}?v=${Date.now()}`;

  const loadStoredQr = useCallback(async () => {
    const storedUrl = buildStoredQrUrl();

    try {
      const response = await fetch(storedUrl, {
        method: "HEAD",
        credentials: "include",
      });

      if (response.ok) {
        setQrImageUrl(storedUrl);
        const lastModified = response.headers.get("last-modified");
        if (lastModified) {
          const parsed = new Date(lastModified);
          if (!Number.isNaN(parsed.getTime())) {
            setLastUpdatedAt(parsed);
          }
        }
      } else {
        setQrImageUrl("");
        setLastUpdatedAt(null);
      }
    } catch {
      setQrImageUrl("");
      setLastUpdatedAt(null);
    }
  }, []);

  useEffect(() => {
    void loadStoredQr();
  }, [loadStoredQr]);

  const handleClickUpload = () => {
    fileInputRef.current?.click();
  };

  const handleUploadQr: React.ChangeEventHandler<HTMLInputElement> = async (
    event,
  ) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const fileNameLower = file.name.toLowerCase();
    if (!fileNameLower.endsWith(".png")) {
      toast.error("Chỉ hỗ trợ file PNG");
      event.target.value = "";
      return;
    }

    const formData = new FormData();
    formData.append("file", file);

    setIsUploading(true);
    try {
      const response = await fetch(`${API_BASE}/Auth/admin/qr-code`, {
        method: "POST",
        credentials: "include",
        body: formData,
      });

      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          throw new Error("Phiên đăng nhập hết hạn hoặc không đủ quyền.");
        }

        const rawText = await response.text().catch(() => "");
        let message = "Cập nhật QR thất bại";

        if (rawText) {
          try {
            const parsed = JSON.parse(rawText) as { message?: string };
            if (parsed?.message) {
              message = parsed.message;
            }
          } catch {
            message = rawText;
          }
        }

        throw new Error(message);
      }

      const data = (await response.json()) as {
        url?: string;
        updatedAt?: string;
      };
      const nextUrl = buildStoredQrUrl(data.url || STORED_QR_PATH);
      setQrImageUrl(nextUrl);

      if (data.updatedAt) {
        const parsed = new Date(data.updatedAt);
        setLastUpdatedAt(Number.isNaN(parsed.getTime()) ? new Date() : parsed);
      } else {
        setLastUpdatedAt(new Date());
      }

      toast.success("Đã cập nhật ảnh QR");
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Không thể cập nhật ảnh QR";
      toast.error(message);
    } finally {
      setIsUploading(false);
      event.target.value = "";
    }
  };

  const handleDownload = async () => {
    if (!qrImageUrl) {
      toast.error("Chưa có mã QR để tải");
      return;
    }

    try {
      const response = await fetch(qrImageUrl, {
        credentials: "include",
      });

      if (!response.ok) {
        throw new Error("Không thể tải ảnh QR từ server");
      }

      const blob = await response.blob();
      const blobUrl = URL.createObjectURL(blob);

      const anchor = document.createElement("a");
      anchor.href = blobUrl;
      anchor.download = QR_OUTPUT_FILE;
      document.body.appendChild(anchor);
      anchor.click();
      document.body.removeChild(anchor);
      URL.revokeObjectURL(blobUrl);

      toast.success("Đã tải file PNG");
    } catch {
      toast.error("Tải PNG thất bại");
    }
  };

  return (
    <AdminLayout>
      <div className="page-header">
        <div>
          <h1 className="page-title">Mã QR</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Thêm và tải mã QR mở ứng dụng Food Market Narrator
          </p>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-8 py-6">
        <div className="stat-card p-6">
          <div className="flex items-center justify-end gap-4 flex-wrap">
            <input
              ref={fileInputRef}
              type="file"
              accept="image/png"
              className="hidden"
              onChange={handleUploadQr}
            />
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                onClick={handleClickUpload}
                disabled={isUploading}
                className="gap-2"
              >
                <Upload className="h-4 w-4" />
                Cập nhật QR
              </Button>
              <Button
                onClick={handleDownload}
                disabled={isUploading || !qrImageUrl}
                className="gap-2"
              >
                <Download className="h-4 w-4" />
                Tải PNG
              </Button>
            </div>
          </div>

          <div className="mt-6 rounded-xl border bg-white p-6 min-h-[460px] flex items-center justify-center">
            {qrImageUrl ? (
              <img
                src={qrImageUrl}
                alt="QR code mở Food Market Narrator"
                className="w-full max-w-[420px] h-auto"
              />
            ) : (
              <div className="flex flex-col items-center gap-3 text-muted-foreground">
                <QrCodeIcon className="h-8 w-8" />
                <p className="text-sm">Chưa có mã QR</p>
              </div>
            )}
          </div>

          <p className="text-xs text-muted-foreground mt-4">
            {lastUpdatedAt
              ? `Lần cập nhật gần nhất: ${lastUpdatedAt.toLocaleString("vi-VN")}`
              : "Chưa có ảnh QR trong hệ thống."}
          </p>
        </div>
      </div>
    </AdminLayout>
  );
};

export default QrCodePage;
