# MAUI Feature Inventory and Gap Analysis

## 1. Mục đích

Tài liệu này trả lời trực tiếp câu hỏi: phần MAUI đã full tính năng chưa.

Kết luận nhanh:

- Chưa full 100%.
- Core flow visitor đã chạy tốt: map, POI, auto narration, manual audio, cache/offline cơ bản, favorite/history, sync location/audio log.
- Vẫn còn một số hạng mục mới dừng ở UI hoặc chưa có backend wiring.

## 2. Feature Inventory (theo trạng thái)

### 2.1 Đã hoàn thiện (Implemented)

1. Startup warm-up dữ liệu POI + language.
2. Theo dõi vị trí định kỳ, publish location theo ngưỡng di chuyển.
3. Geofence enter/switch/exit cho POI.
4. Auto narration với anti-repeat theo session + cooldown theo thời gian.
5. Queue phát audio tuần tự.
6. Manual play/pause/resume audio trong POI detail.
7. Audio caching local với quota 200MB + LRU cleanup + ngưỡng free space tối thiểu 50MB.
8. Offline fallback cho POI/language (cache file) và audio (cache/package).
9. Đồng bộ location log theo batch 10 giây.
10. Đồng bộ audio playback log, có retry khi Session not found.
11. Favorite lưu persistent qua Preferences.
12. History lưu in-memory với giới hạn 50 mục.
13. Deep link scheme foodmarketnarrator://open.
14. Foreground service tracking nền trên Android.

### 2.2 Hoạt động một phần / UX placeholder (Partial)

1. MainPage Search bar và category chips hiện là UI tĩnh, chưa có logic lọc danh sách POI tại MainPage.
2. Settings có công tắc Thông báo và Chế độ tối nhưng chưa có luồng lưu/áp dụng setting thực tế.
3. Settings có cụm Trung tâm trợ giúp, Chính sách bảo mật, Điều khoản sử dụng nhưng chưa có handler mở trang tương ứng.
4. POIDetail có icon share ở UI nhưng chưa có event handler thực hiện chia sẻ.
5. POIDetail hiển thị rating/review dạng placeholder (chưa nối dữ liệu thực).

### 2.3 Chưa có trong code hiện tại (Not implemented)

1. Cơ chế TTL time-based cho POI/language/audio manifest (ví dụ 24h/7 ngày).
2. Persist queue cho telemetry/audio log khi app bị kill đột ngột.
3. Đồng bộ favorite/history lên server theo tài khoản.
4. Login flow thực sự cho phần profile trong Settings (nút đăng nhập hiện là UI).

## 3. Chi tiết các gap cần làm để "full"

### Gap A: MainPage search/filter chưa functional

- Hiện trạng:
  - UI đã có Entry + category chips.
  - Chưa bind vào logic lọc POI list như MapPage.
- Đề xuất:
  1. Tái sử dụng normalize + scoring từ MapPage.
  2. Lọc CollectionView nguồn dữ liệu trên MainPage.
  3. Đồng bộ highlight marker theo kết quả lọc.

### Gap B: Settings toggles chưa có state persistence

- Hiện trạng:
  - Thông báo và Chế độ tối chỉ là Switch UI.
- Đề xuất:
  1. Thêm Preferences keys cho từng toggle.
  2. Load lại giá trị trong OnAppearing.
  3. Áp dụng AppTheme nếu bật chế độ tối.

### Gap C: Support links chưa wired

- Hiện trạng:
  - Item UI có sẵn nhưng không có TapGesture/Command.
- Đề xuất:
  1. Thêm handler mở URL bằng Launcher.OpenAsync.
  2. Fallback alert nếu mở link thất bại.

### Gap D: Share action trong POIDetail chưa wired

- Hiện trạng:
  - Nút share hiện chỉ hiển thị icon.
- Đề xuất:
  1. Dùng Share.Default.RequestAsync.
  2. Nội dung share gồm tên quán + địa chỉ + link bản đồ.

### Gap E: Telemetry durability khi crash/process kill

- Hiện trạng:
  - Buffer location logs ở RAM.
  - Audio logs gửi trực tiếp không có local queue bền vững.
- Đề xuất:
  1. Thêm local persistent queue (SQLite/file queue).
  2. Replay queue khi app resume online.

## 4. Định nghĩa "full" thực tế cho MAUI

Để xem là full production-ready cho phía mobile visitor, tối thiểu cần đạt:

1. Tất cả control có trên UI đều có hành vi rõ ràng (không placeholder trống).
2. Trạng thái cài đặt quan trọng được lưu persistent.
3. Telemetry quan trọng không mất dữ liệu khi app kill đột ngột.
4. Error state hiển thị thân thiện cho user ở các luồng network chính.
5. Tài liệu API contract và cache được đồng bộ với code mỗi lần đổi logic.

## 5. Backlog ưu tiên (đề xuất)

### Priority 1

1. Functionalize MainPage search/filter.
2. Wire share action ở POIDetail.
3. Wire support/policy/terms links ở Settings.
4. Persist Notification/Dark mode settings.

### Priority 2

1. Bổ sung local persistent queue cho location/audio logs.
2. Thêm TTL policy cho cache POI/language (ví dụ soft TTL 24h).

### Priority 3

1. Đồng bộ favorite/history theo user account khi có auth flow.
2. Bổ sung rating/review real data khi backend sẵn sàng.

## 6. Trạng thái hiện tại (summary)

- Core business flow narration: Đạt.
- Cache/offline nền tảng: Đạt mức usable.
- Quản lý setting và tiện ích phụ trợ: Chưa full.
- Production-hardening telemetry: Chưa full.

=> MAUI hiện tại mạnh ở luồng thuyết minh tự động và trải nghiệm khám phá quán, nhưng chưa full toàn bộ tính năng UI đã hiển thị.
