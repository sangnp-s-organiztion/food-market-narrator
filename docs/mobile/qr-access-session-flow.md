# Luồng QR Access Session (trạng thái hiện tại)

Tài liệu này mô tả flow thực tế theo code hiện tại của MAUI app và API.

## 1) Tổng quan

QR deep link có thể bật chế độ giới hạn thời gian truy cập narration.

- Nếu deep link không có tham số thời gian: app chạy như bình thường (không giới hạn QR).
- Nếu deep link có tham số thời gian: app bật chế độ QR time-restricted và theo dõi hạn truy cập liên tục.

Deep link hợp lệ:

- Scheme: foodmarketnarrator
- Host: open
- Ví dụ: foodmarketnarrator://open?durationMinutes=30

## 2) Các tham số deep link được hỗ trợ

QrAccessService đang parse các tham số sau theo thứ tự ưu tiên:

1. expiresAtUtc | expiresAt | until
2. durationMinutes | durationMins | ttlMinutes
3. durationSeconds | ttlSeconds

Nếu parse được expiry:

- IsQrTimeRestricted = true
- QrAccessExpiresAtUtc được set theo UTC

Nếu không parse được expiry:

- IsQrTimeRestricted = false
- QrAccessExpiresAtUtc = null

## 3) Runtime flow trong app

### 3.1 Nhận deep link

App nhận deep link qua 2 đường:

- Lúc khởi động app (HandleAppStart)
- Trong runtime qua AppLinkDispatcher.DeepLinkReceived

Sau đó app gọi:

1. QrAccessService.ApplyDeepLink(deepLink)
2. EnsureQrAccessGuardLoopState()

### 3.2 Vòng guard kiểm tra quyền narration

Khi IsQrTimeRestricted = true, app bật vòng loop check mỗi 1 giây:

1. Lấy CurrentSessionId từ LocationLogSyncService.
2. Gọi CanContinueNarrationAsync(sessionId).
3. Nếu allowed = false: dừng narration và thông báo QR hết hạn.

### 3.3 Logic CanContinueNarrationAsync

1. Nếu không bị giới hạn QR -> true.
2. Nếu local expiry đã qua -> false (reason = expired).
3. Nếu sessionId rỗng -> true (không check server).
4. Nếu vừa check cùng session trong < 10 giây -> dùng cache kết quả lần trước.
5. Nếu cần check server:
   - Gọi GET /api/user-sessions/{sessionId}/qr-access
   - Nhận allowed, expiresAtUtc, reason
   - Cập nhật cache và đồng bộ expiry local theo min(local, server)
6. Nếu lỗi mạng khi gọi server -> fallback true (không cắt narration ngay).

## 4) API liên quan

- POST /api/user-sessions/start
- GET /api/user-sessions/{sessionId}/qr-access

Qr-access response:

- allowed: bool
- expiresAtUtc: datetime?
- reason: string?

## 5) Xử lý hết hạn QR

Khi guard nhận allowed = false:

1. Flush log đang chờ gửi.
2. Dừng NarrationFlowService.StopNarration().
3. Hiển thị alert cho user: "QR hết hạn".
4. Yêu cầu user quét lại QR để tiếp tục.

## 6) Checklist test nhanh

1. Quét deep link có durationMinutes=1, narration chạy được trong khoảng 1 phút.
2. Hết thời gian, app dừng narration và hiển thị thông báo hết hạn.
3. Quét deep link không có tham số thời gian, narration không bị giới hạn QR.
4. Mất mạng tạm thời, narration không bị cắt ngay (nếu local expiry chưa qua).
5. Server trả allowed=false sớm hơn local expiry, app vẫn cắt narration theo kết quả server.
