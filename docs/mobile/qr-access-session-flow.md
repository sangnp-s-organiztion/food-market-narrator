# Luồng QR App Open (trạng thái hiện tại)

Tài liệu này mô tả flow thực tế theo code hiện tại của MAUI app.

## 1) Tổng quan

QR deep link hiện chỉ dùng để mở ứng dụng.

- Không còn chế độ giới hạn thời gian từ QR.
- Không còn chặn narration theo trạng thái hết hạn QR.

Deep link hợp lệ:

- Scheme: foodmarketnarrator
- Host: open
- Ví dụ khuyến nghị: foodmarketnarrator://open

## 2) Tham số deep link

QrAccessService hiện chỉ kiểm tra định dạng deep link hợp lệ theo scheme/host.

- App không còn parse các tham số thời gian.
- Nếu deep link có query string, app vẫn mở bình thường miễn là đúng scheme/host.

## 3) Runtime flow trong app

### 3.1 Nhận deep link

App nhận deep link qua 2 đường:

- Lúc khởi động app (HandleAppStart)
- Trong runtime qua AppLinkDispatcher.DeepLinkReceived

Sau đó app gọi:

1. QrAccessService.ApplyDeepLink(deepLink)

### 3.2 Hành vi narration

- Nút thuyết minh và phát audio không còn bị disable bởi trạng thái QR.
- App không hiển thị cảnh báo "QR hết hạn".

## 4) API liên quan

- MAUI app không còn gọi endpoint kiểm tra QR access theo session.
- Endpoint session start và các endpoint log vẫn hoạt động bình thường theo flow tracking/audio.

## 5) Checklist test nhanh

1. Quét deep link foodmarketnarrator://open, app mở thành công.
2. Bật thuyết minh tự động, narration hoạt động bình thường.
3. Vào trang chi tiết POI, phát audio bình thường.
4. Đóng app và quét lại QR, app vẫn mở và hoạt động như trên.
