# QR Code Content

## Overview

QR code nên dùng URL web trung gian thay vì deep link trực tiếp để đảm bảo:

- Có app -> mở app ngay.
- Chưa có app -> chuyển đến trang tải APK.
- Có thể ghi session vào Mongo `UserSessions` ngay từ lúc scan/download.

## QR Code Data

Nội dung nên encode trong QR:

```
https://food-market-narrator-api.onrender.com/qr/open.html
```

Ví dụ khi chạy nội bộ LAN (local only):

```
http://192.168.1.7:5044/qr/open.html
```

## Runtime Flow

1. User scan QR -> mở `/qr/open.html`.
2. Trang gọi `POST /api/user-sessions/start` để tạo/cập nhật visitor session.
3. Trang thử mở app qua `foodmarketnarrator://open`.
4. Nếu mở app thất bại sau timeout ngắn -> redirect sang `/qr/download.html`.
5. Trang download tiếp tục gọi `POST /api/user-sessions/start` trước khi tải APK.
6. User tải APK tại `/uploads/apk/food-market-narrator.apk`.

## APK Placement

Đặt file APK tại:

```
FoodMarketNarrator.Api/wwwroot/uploads/apk/food-market-narrator.apk
```

## Notes

- Endpoint session start đã là public nên dùng trực tiếp cho flow QR web.
- Dữ liệu web visitor dùng `deviceId` dạng `web-*` và `deviceInfo` lấy từ user-agent.
