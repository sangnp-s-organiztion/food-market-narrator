# Luồng QR App Open (trạng thái hiện tại)

Tài liệu này mô tả flow QR mới (web landing + deep link + fallback tải APK).

## 1) Tổng quan

QR code hiện trỏ đến URL web:

- `/qr/open.html`

Luồng xử lý:

- Nếu đã cài app: trang web gọi deep link để mở app.
- Nếu chưa cài app: trang web tự động chuyển đến `/qr/download.html` để tải APK.

## 2) Deep link contract trong app

Deep link hợp lệ để mở MAUI app:

- Scheme: `foodmarketnarrator`
- Host: `open`
- Ví dụ: `foodmarketnarrator://open`

`QrAccessService` vẫn chỉ validate scheme/host.

- App không parse logic business từ query string.
- Query string (nếu có) không làm app crash.

## 3) Runtime flow

### 3.1 Trên web landing

1. User mở `/qr/open.html` sau khi scan QR.
2. Trang gọi `POST /api/user-sessions/start` (best effort) để ghi session vào `UserSessions`.
3. Trang thử mở app bằng `foodmarketnarrator://open`.
4. Nếu app không mở được trong khoảng timeout ngắn, redirect sang `/qr/download.html`.

### 3.2 Trên trang download

1. `/qr/download.html` tiếp tục gọi `POST /api/user-sessions/start`.
2. User bấm tải APK tại `/uploads/apk/food-market-narrator.apk`.

### 3.3 Trong MAUI app

App nhận deep link qua:

- Lúc khởi động app.
- Runtime thông qua `AppLinkDispatcher.DeepLinkReceived`.

Sau đó gọi `QrAccessService.ApplyDeepLink(deepLink)`.

## 4) API liên quan

- `POST /api/user-sessions/start` (public)
- `POST /api/location-logs/batch` (public)
- `POST /api/audio-logs` (public)

Ghi chú:

- MAUI app không còn gọi `GET /api/user-sessions/{sessionId}/qr-access` để chặn narration.

## 5) Checklist test nhanh

1. Quét QR trỏ `/qr/open.html` khi thiết bị đã cài app -> app mở.
2. Quét QR trên thiết bị chưa cài app -> chuyển tới `/qr/download.html`.
3. Bấm Download APK -> tải file `/uploads/apk/food-market-narrator.apk`.
4. Kiểm tra Mongo `UserSessions` có record `device_id` dạng `web-*` sau bước scan/tải.
