## Mobile App Rules

Platform: Android (.NET MAUI)

### Responsibilities

Mobile app chịu trách nhiệm xử lý logic phía client để mang lại trải nghiệm thuyết minh tự động cho visitor.

Các trách nhiệm chính:

- Lấy vị trí GPS của người dùng theo chu kỳ.
- Tính khoảng cách từ vị trí người dùng tới các restaurant (POI).
- Phát hiện khi người dùng đi vào vùng geofence của restaurant.
- Tự động phát audio narration tương ứng với ngôn ngữ đang chọn.
- Gọi backend API để lấy dữ liệu restaurant, images, dishes và audio.
- Cache dữ liệu để hỗ trợ trải nghiệm offline cơ bản.

---

### Location Tracking

Ứng dụng theo dõi vị trí người dùng khi chế độ narration được bật.

Cấu hình hiện tại:

- PollInterval: 2 giây
- MinPublishDistanceMeters: 6 mét
- GeolocationRequest: Best accuracy, timeout 10 giây
- Chỉ publish event khi di chuyển >= 6m

Trên Android:
- Xin quyền theo tầng: WhenInUse -> Always (Android 10+) -> PostNotifications (Android 13+)
- Có foreground service TrackingForegroundService để theo dõi nền

---

### Distance Calculation

Mobile app tính khoảng cách giữa vị trí hiện tại và các restaurant dựa trên tọa độ:

- latitude
- longitude

Restaurant gần nhất sẽ được xác định để kiểm tra điều kiện trigger narration.

---

### Geofence Trigger

Mỗi restaurant được coi là một **Point of Interest (POI)** với bán kính enter/exit.

Cấu hình hiện tại (trong AppSettings):

- PoiEnterRadiusMeters: 30m (kích hoạt khi vào vùng)
- PoiExitRadiusMeters: 40m (thoát vùng - hysteresis chống rung biên)
- TriggerDistanceMeters: 30m (ngưỡng phát audio)

State machine trong POIService.UpdateNearestPOI():
- Enter: chưa trong POI nào -> vào vùng 30m
- Switch: đang trong POI này -> chuyển sang POI khác trong vùng 30m
- Exit: ra khỏi vùng 40m của POI hiện tại

---

### Narration Trigger Rules

Có hai cơ chế chống lặp:

1. **Theo phiên narration**: HashSet \_playedPOIs lưu các restaurant đã phát trong phiên. Reset khi StopNarration().

2. **Theo thời gian**: Cooldown 60 giây giữa các lần phát cho cùng một POI.

Session narration được định nghĩa là khoảng thời gian từ khi người dùng bật narration cho đến khi tắt narration.

Luật hoạt động:

- Khi user **enter geofence** (vào vùng 30m) → narration có thể được phát.
- Nếu restaurant đã được phát trong phiên → không auto-play lại (trừ khi hết cooldown).
- Nếu chưa hết 60 giây kể từ lần phát cuối → không phát.
- **Force/manual trigger** cho phép phát ngay và bỏ qua mọi kiểm tra khoảng cách/cooldown.
- Người dùng vẫn có thể **phát lại audio thủ công** từ màn hình chi tiết POI.

---

### Audio Selection

Khi narration được kích hoạt:

1. Lấy ngôn ngữ hiện tại của user.
2. Tìm audio tương ứng với restaurant và language.
3. Nếu audio tồn tại → phát audio.
4. Nếu không có audio → hiển thị thông báo nhẹ và bỏ qua narration.

---

### API Interaction

Các API được đánh dấu (public) là endpoint không yêu cầu đăng nhập, phù hợp cho mobile app visitor.

Mobile app hiện nên ưu tiên gọi:

- GET /Restaurant (public)
- GET /Restaurant/{id} (public)
- GET /Language (public)
- GET /Language/{languageCode} (public)
- GET /public/Restaurant/{restaurantId}/images (public)
- GET /public/Restaurant/{restaurantId}/dishes (public)
- GET /public/Restaurant/{restaurantId}/audios (public)

Lưu ý:

- Không dùng nhầm các endpoint /Restaurant/{restaurantId}/images|dishes|audios vì các endpoint này yêu cầu đăng nhập.
- Mobile app không chứa business logic quản trị dữ liệu; backend chịu trách nhiệm validate và điều phối dữ liệu.

---

### Offline Support

Mobile app cache các dữ liệu sau:

- **POI**: Lưu vào file offline_cache/pois.json trong FileSystem.AppDataDirectory.
- **Audio**: Lưu vào thư mục audio_cache với tên file là hash SHA256 (language|path).

Chính sách cache audio:
- Giới hạn tổng: 200MB
- Dung lượng trống tối thiểu: 50MB
- Cơ chế dọn LRU khi gần đầy
- Cache ưu tiên: local -> package -> network

Khi mất kết nối mạng:
- App đọc POI từ cache offline
- Narration vẫn hoạt động nếu audio đã được cache trước đó
- Nếu chưa cache và không có mạng -> không phát audio

---

### Performance Constraints

Để đảm bảo hiệu năng và tiết kiệm pin:

- không cập nhật GPS quá thường xuyên
- áp dụng debounce cho location updates
- chỉ tính khoảng cách khi có location update hợp lệ
- chỉ trigger narration khi điều kiện geofence được thỏa mãn
