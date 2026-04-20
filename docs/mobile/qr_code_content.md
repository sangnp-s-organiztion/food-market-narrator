# QR Code Content

## Overview

QR code nên trỏ tới một URL web trung gian để xử lý được cả 2 tình huống:

- Thiết bị đã cài app -> tự mở app.
- Thiết bị chưa cài app -> chuyển sang trang tải APK.

## QR Code Data

Nội dung khuyến nghị để mã hóa trong QR code:

```
https://food-market-narrator-api.onrender.com/qr/open.html
```

Ví dụ trong mạng LAN (khi chỉ chạy local):

```
http://192.168.1.7:5044/qr/open.html
```

Nếu đã có domain public thì dùng HTTPS domain tương ứng (như Render URL bên trên).

## How It Works

1. Người dùng quét QR và mở URL `/qr/open.html`.
2. Trang này gọi `POST /api/user-sessions/start` (public) để ghi nhận session vào Mongo collection `UserSessions`.
3. Trang thử mở app bằng deep link `foodmarketnarrator://open`.
4. Nếu app đã cài -> app được mở.
5. Nếu app chưa cài -> tự động chuyển sang `/qr/download.html`.
6. Trang download tiếp tục gọi `POST /api/user-sessions/start` (best effort) trước khi tải APK.
7. Người dùng tải và cài file APK từ `/uploads/apk/food-market-narrator.apk`.

## APK File Requirement

Backend cần có file APK tại:

```
FoodMarketNarrator.Api/wwwroot/uploads/apk/food-market-narrator.apk
```

URL tải tương ứng:

```
/uploads/apk/food-market-narrator.apk
```

## Notes

- Flow mới không phụ thuộc việc QR scanner có hỗ trợ custom scheme trực tiếp hay không.
- Session tạo từ trang web dùng `deviceId` dạng `web-*` để theo dõi anonymous visitor.
