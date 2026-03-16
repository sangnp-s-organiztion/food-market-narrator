# Visitor Feature - Food Market Narrator

## 1. Mục tiêu

Tài liệu này mô tả bộ tính năng dành cho người dùng khách tham quan (visitor) trong ứng dụng mobile. Mục tiêu là giúp visitor khám phá khu ẩm thực nhanh, tìm đúng điểm quan tâm và nghe thuyết minh tự động theo ngôn ngữ phù hợp.

## 2. Đối tượng sử dụng

- Khách lần đầu đến khu ẩm thực.
- Khách du lịch không rành địa điểm.
- Người dùng muốn trải nghiệm "đi tới đâu nghe thuyết minh tới đó".

## 3. Phạm vi tính năng

### 3.1 Bản đồ và POI

- Hiển thị danh sách POI trên bản đồ.
- Hiển thị vị trí hiện tại của người dùng.
- Cho phép chọn POI để xem chi tiết.
- Làm nổi bật POI gần nhất theo khoảng cách.

### 3.2 Chi tiết POI

- Hiển thị thông tin cơ bản: tên, mô tả, địa chỉ.
- Hiển thị hình ảnh theo thứ tự ưu tiên.
- Hiển thị danh sách món ăn liên quan.
- Hiển thị trạng thái audio theo ngôn ngữ đang chọn.

### 3.3 Thuyết minh tự động

- Theo dõi vị trí theo chu kỳ khi người dùng bật narration.
- Tự động xác định POI gần nhất.
- Nếu vào bán kính kích hoạt, tự phát audio phù hợp ngôn ngữ.
- Tránh phát lặp lại POI đã nghe trong cùng một phiên.
- Cho phép phát thủ công lại audio POI khi người dùng yêu cầu.

### 3.4 Chọn ngôn ngữ

- Người dùng chọn ngôn ngữ thuyết minh.
- Hệ thống ưu tiên audio theo ngôn ngữ đã chọn.
- Nếu thiếu audio ngôn ngữ mong muốn, hiển thị thông báo rõ ràng.

### 3.5 Offline cơ bản

- Cache danh sách POI để dùng lại khi mất mạng.
- Cache audio đã phát để giảm độ trễ và tiết kiệm dữ liệu.
- Tự fallback qua nguồn dữ liệu cache khi API không khả dụng.

## 4. Luồng trải nghiệm chính

1. Người dùng mở app, cấp quyền vị trí.
2. App tải POI và hiển thị trên bản đồ.
3. Người dùng bật narration tự động.
4. Khi di chuyển gần POI, app tự phát audio.
5. Người dùng có thể mở chi tiết POI để xem ảnh, món ăn và phát lại audio.

## 5. Luật nghiệp vụ

- POI được coi là đủ gần để trigger khi khoảng cách <= trigger distance cấu hình.
- Một POI chỉ auto-play một lần trong cùng phiên narration.
- Khi người dùng tắt narration:
  - Dừng theo dõi vị trí cho luồng narration.
  - Dừng audio đang phát.
  - Xóa hàng đợi audio chờ phát.
- Manual trigger được phép phát lại dù POI đã auto-play trước đó.

## 6. Yêu cầu API cho visitor

### 6.1 Dữ liệu công khai

- GET /Restaurant
- GET /Restaurant/{id}
- GET /Language
- GET /Language/{languageCode}
- GET /public/Restaurant/{restaurantId}/images
- GET /public/Restaurant/{restaurantId}/dishes
- GET /public/Restaurant/{restaurantId}/audios

### 6.2 Media

- Ảnh: tải qua đường dẫn /maui-images/... hoặc URL ảnh trả về từ API.
- Audio: tải qua đường dẫn /maui-audios/... hoặc /uploads/audios/...

## 7. Kiến trúc hệ thống (Visitor scope)

### 7.1 Architecture

```text
+----------------------------------------------------------------------------------+
|                               FRONTEND (Mobile MAUI)                            |
|  Map UI | POI Detail UI | Narration Controls | Language Selector                |
|  Services: POIService | LocationService | NarrationFlowService | AudioService    |
|  Local: offline_cache (POI) + audio_cache                                        |
+----------------------------------------------------------------------------------+
                                      |
                                      | HTTPS REST (JSON)
                                      v
+----------------------------------------------------------------------------------+
|                           BACKEND (ASP.NET Core API)                             |
|  Controllers -> Services -> Repositories -> EF Core                              |
|  Public endpoints:                                                               |
|   - GET /Restaurant                                                              |
|   - GET /Restaurant/{id}                                                         |
|   - GET /Language                                                                |
|   - GET /Language/{languageCode}                                                 |
|   - GET /public/Restaurant/{restaurantId}/images                                 |
|   - GET /public/Restaurant/{restaurantId}/dishes                                 |
|   - GET /public/Restaurant/{restaurantId}/audios                                 |
+----------------------------------------------------------------------------------+
                    |                                           |
                    | SQL                                       | Static file serving
                    v                                           v
         +----------------------------+            +--------------------------------+
         | SQL Server                 |            | Media Storage                  |
         | Restaurant, Dish, Audio,   |            | /maui-images, /maui-audios,   |
         | Language, User, Image      |            | /uploads/audios               |
         +----------------------------+            +--------------------------------+
```

### 7.2 How Data Works

```text
App Open
  -> POIService gọi GET /Restaurant
  -> API trả danh sách POI
  -> UI render marker map
  -> ghi cache offline_cache/pois.json

Tap POI
  -> gọi song song:
     GET /public/Restaurant/{restaurantId}/images
     GET /public/Restaurant/{restaurantId}/dishes
     GET /public/Restaurant/{restaurantId}/audios
  -> UI hiển thị detail (ảnh, menu, audio)

Start Narration
  -> LocationService phát sự kiện vị trí
  -> NarrationFlowService tính POI gần nhất + khoảng cách
  -> nếu khoảng cách <= trigger_distance:
       chọn audio theo language hiện tại
       AudioService phát từ audio_cache nếu có
       nếu chưa có cache -> tải từ API -> lưu cache -> phát

Network Error
  -> fallback đọc POI/audio từ cache
  -> UI vẫn hoạt động ở chế độ offline cơ bản
  -> khi mạng ổn định: đồng bộ lại dữ liệu mới
```

### 7.3 Luồng dữ liệu thời gian thực (narration)

```text
GPS Update
  -> LocationService
  -> NarrationFlowService
  -> GetNearestPOI()
  -> Check trigger radius
  -> Resolve audio by language
  -> PlaySound()
  -> UI update: playing/paused/ended
```

## 8. Yêu cầu phi chức năng

- Thời gian hiển thị POI ban đầu: nên < 3 giây trong điều kiện mạng bình thường.
- Audio bắt đầu phát: nên < 2 giây nếu đã cache, < 5 giây nếu tải từ mạng.
- App không crash khi:
  - Mất mạng đột ngột.
  - Không có quyền vị trí.
  - Dữ liệu audio bị thiếu hoặc URL lỗi.
- Tiêu thụ pin ở mức hợp lý khi bật theo dõi vị trí liên tục.

## 9. Edge cases

- Không lấy được GPS: hiển thị hướng dẫn bật định vị, cho phép thử lại.
- Người dùng đứng giữa nhiều POI gần nhau: ưu tiên POI gần nhất.
- Audio đang phát gặp cuộc gọi đến: tạm dừng/phục hồi theo hành vi nền tảng.
- API timeout: fallback cache và thông báo nhẹ, không chặn toàn bộ UI.

## 10. Acceptance criteria

- Người dùng xem được POI trên bản đồ sau khi mở app.
- Người dùng bật narration và nghe được ít nhất một audio khi vào vùng gần POI.
- Người dùng đổi ngôn ngữ và audio lần phát tiếp theo tuân theo ngôn ngữ mới.
- Khi offline, app vẫn hiển thị được POI đã cache trước đó.
- Không phát lặp tự động cùng một POI trong cùng phiên narration.

## 11. Gợi ý theo dõi chất lượng

- Tỷ lệ tải POI thành công.
- Tỷ lệ trigger narration thành công.
- Tỷ lệ phát audio lỗi.
- Thời gian trung bình từ lúc vào vùng trigger đến lúc audio bắt đầu phát.
- Tỷ lệ phiên sử dụng có fallback cache.
