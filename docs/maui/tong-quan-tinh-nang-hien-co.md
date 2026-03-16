# Tổng quan tính năng hiện có - MAUI app

Tài liệu này tổng hợp trạng thái tính năng thực tế của dự án food-market-narrator-maui theo code hiện tại.

## 1) Màn hình và điều hướng

- AppShell đang khai báo 2 ShellContent chính:
  - MainPage (Trang chủ)
  - MapPage (Bản đồ)
- Đã đăng ký route POIDetailPage để mở chi tiết theo restaurantId.
- Bottom navigation custom hiển thị 5 mục (Trang chủ, Bản đồ, Yêu thích, Lịch sử, Cài đặt), nhưng:
  - Điều hướng thật hiện có: Trang chủ, Bản đồ.
  - Yêu thích/Lịch sử chưa có handler thực thi.
  - Cài đặt đang dùng action sheet để xóa audio cache.

## 2) Nguồn dữ liệu và POI

- POIService tải dữ liệu từ endpoint restaurant qua HttpClient.
- Có fallback base URL theo AppSettings.ApiFallbackBaseUrls.
- Có cache offline POI vào file cục bộ: offline_cache/pois.json.
- Cung cấp các hàm:
  - GetAllPOIsAsync
  - GetPOIByIdAsync
  - GetNearestPOI
  - UpdateNearestPOI (mô hình geofence 30m/40m)

## 3) Theo dõi vị trí

- LocationService dùng polling loop 2 giây.
- GeolocationRequest: Best, timeout 10 giây.
- Chỉ publish event khi dịch chuyển >= 6m.
- Trên Android:
  - Có luồng xin quyền theo tầng: WhenInUse -> Always (Android 10+) -> PostNotifications (Android 13+).
  - Có foreground service TrackingForegroundService để theo dõi nền.

## 4) Bản đồ (Mapsui + OSM)

- Dùng Mapsui và tile OpenStreetMap.
- Tile cache trong FileSystem.CacheDirectory/osm_tiles.
- MapHelper chịu trách nhiệm:
  - Load layer map + marker POI.
  - Highlight một hoặc nhiều POI.
  - Cập nhật marker vị trí người dùng.
  - Center map theo vị trí.
  - Force refresh dữ liệu/graphics để tránh trễ render.

## 5) MainPage

- Thành phần chính:
  - Bản đồ nhúng.
  - Danh sách POI (CollectionView) và điều hướng sang POIDetailPage.
  - Nút floating bật/tắt thuyết minh tự động.
  - Popup chọn ngôn ngữ.
- Khi mới vào app:
  - Nếu chưa chọn ngôn ngữ: tự mở popup chọn ngôn ngữ.
  - Nếu đã chọn: tự bật narration 1 lần cho mỗi phiên chạy app.
- Floating button chỉ hiện khi ở trong phạm vi TriggerDistanceMeters (30m) so với POI gần nhất.

## 6) MapPage

- Có nút Zoom In, Zoom Out, My Location.
- Có search theo từ khóa (debounce 220ms), gợi ý kết quả và highlight POI tìm được.
- Có popup card POI khi tap gần marker:
  - Tên, ảnh, địa chỉ.
  - Nút Xem chi tiết sang POIDetailPage.
- Ngưỡng tap marker động theo zoom:
  - Clamp(viewportResolution \* 28, 12m, 150m).

## 7) Ngôn ngữ

- LanguageService lấy danh sách ngôn ngữ từ API language.
- Mã ngôn ngữ đang dùng: vi-VN, en-US, zh-CN, ko-KR, ja-JP.
- Lựa chọn ngôn ngữ được lưu Preferences.
- Khi đổi ngôn ngữ, UI được cập nhật và luồng audio sử dụng audio tương ứng theo ngôn ngữ mới.

## 8) Narration flow

- NarrationFlowService quản lý bật/tắt narration tự động.
- Khi bật:
  - Subscribe LocationChanged.
  - Theo dõi vị trí và gọi CheckAndNarrateAsync.
- Cơ chế trigger hiện tại:
  - Lấy nearest bằng GetNearestPOI.
  - Nếu khoảng cách <= 30m thì enqueue phát audio.
  - \_playedPOIs chặn phát lặp trong cùng phiên.
  - force=true (manual) cho phép phát ngay và bỏ qua kiểm tra khoảng cách.

Lưu ý quan trọng:

- POIService đã có UpdateNearestPOI theo geofence enter/exit, nhưng NarrationFlowService hiện chưa dùng hàm này cho auto trigger.

## 9) Audio

- AudioService dùng Plugin.Maui.Audio.
- Hỗ trợ:
  - PlaySound
  - Pause, Resume, StopSound
  - Theo dõi IsPlaying, IsPaused, Duration, CurrentPosition
  - Event PlaybackEnded
- Cơ chế lấy audio:
  - Cache local -> Package -> Network.
- Có quản lý dung lượng cache (200MB), giữ trống tối thiểu (50MB), dọn LRU khi cần.

## 10) POIDetailPage

- Nhận restaurantId từ query route.
- Tải POI theo id và bind dữ liệu lên UI.
- Có module audio guide:
  - Play/Pause/Resume theo trạng thái track hiện tại.
  - Đồng bộ icon và progress bar.
  - Timer cập nhật tiến trình mỗi 200ms.
- Có nút back về MainPage.
- Hai nút Đường đi và Gọi điện ngay hiện là UI, chưa thấy handler code-behind.

## 11) Các phần đã có và chưa hoàn thiện

Đã có logic chạy thực tế:

- POI load từ API + cache offline.
- Theo dõi vị trí foreground/background (Android).
- Map OSM + marker + highlight + search trên MapPage.
- Auto narration theo vị trí và manual trigger.
- POI detail + audio player theo ngôn ngữ.

Chưa hoàn thiện hoặc mới ở mức khung:

- Tab Yêu thích/Lịch sử chưa có hành vi điều hướng.
- Filter chip danh mục trên MapPage chưa có logic lọc dữ liệu.
- Nút Favorite/Share trên POIDetailPage chưa nối logic.
- Nút Đường đi/Gọi điện trên POIDetailPage chưa xử lý sự kiện.

## 12) Cấu hình API hiện tại

- AppSettings dùng host động trên Android:
  - Emulator: 10.0.2.2
  - Thiết bị thật: LocalApiHost (hiện tại là 192.168.1.7)
- Port mặc định:
  - HTTP: 5044
  - HTTPS: 7041
