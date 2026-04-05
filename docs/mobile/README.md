# MAUI Docs

Tài liệu tổng quan cho FoodMarketNarrator.Maui.

## Mục tiêu app

Ứng dụng mobile visitor với tính năng:

- Theo dõi vị trí GPS
- Geofence theo POI
- Tự động phát audio thuyết minh
- Hỗ trợ đa ngôn ngữ
- Cache offline cơ bản cho POI và audio

## Stack

- .NET MAUI (net10.0-android)
- Mapsui
- Plugin.Maui.Audio

## Luồng narration

- Poll vị trí theo chu kỳ
- Xác định POI gần nhất
- Trigger enter/switch theo geofence
- Chọn audio theo ngôn ngữ hiện tại
- Chống lặp bằng session state và cooldown

Ghi chú hành vi hiện tại:

- Khi đang phát audio POI A mà chuyển sang POI B, audio POI A sẽ phát thêm khoảng 3 giây, sau đó bị ngắt để chuyển sang audio POI B.
- Trạng thái mở/đóng ưu tiên tính theo `OpenTime/CloseTime` từ API; nếu không có thì mới fallback parse `OpeningHours`.
- Nếu thiếu cả dữ liệu giờ mở/đóng, UI hiển thị `Đang cập nhật` thay vì dùng giờ mặc định cố định.

## Cấu hình API

Cấu hình host và endpoint trong:

- FoodMarketNarrator.Maui/Settings/AppSettings.cs

Lưu ý thiết bị thật:

- Điện thoại và máy chạy API phải cùng mạng
- API local thông thường: <http://localhost:5044>

## Chạy local

```bash
cd FoodMarketNarrator.Maui
dotnet restore
dotnet build
dotnet run -f net10.0-android
```

## Chạy test

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj
```

## Tài liệu liên quan

- overview-current-features.md
- narration-geofence-trigger-flow.md
- qr-access-session-flow.md
- audio-cache-storage.md
- ../testing/unit/maui-unit-test-cases.md
