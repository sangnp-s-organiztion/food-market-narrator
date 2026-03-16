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

Yêu cầu:

- Location update phải được **debounced** để tránh xử lý quá nhiều lần.
- Chỉ xử lý khi:
  - người dùng di chuyển vượt quá khoảng cách tối thiểu, hoặc
  - sau một khoảng thời gian nhất định.

Ví dụ cấu hình:

- location_update_interval: 2–5 seconds
- location_min_distance: 5–10 meters

---

### Distance Calculation

Mobile app tính khoảng cách giữa vị trí hiện tại và các restaurant dựa trên tọa độ:

- latitude
- longitude

Restaurant gần nhất sẽ được xác định để kiểm tra điều kiện trigger narration.

---

### Geofence Trigger

Mỗi restaurant được coi là một **Point of Interest (POI)** với một bán kính kích hoạt.

Narration được kích hoạt khi:

distance(user_location, restaurant_location) <= trigger_radius

Ví dụ:

- trigger_radius = 25 meters

Khi người dùng đi vào vùng này, hệ thống sẽ kiểm tra điều kiện phát narration.

---

### Narration Trigger Rules

Narration chỉ được **auto-play một lần cho mỗi restaurant trong cùng một session**.

Session narration được định nghĩa là khoảng thời gian từ khi người dùng bật narration cho đến khi tắt narration.

Luật hoạt động:

- Khi user **enter geofence** → narration có thể được phát.
- Nếu restaurant đã được phát trước đó trong session → không auto-play lại.
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

Mobile app nên cache các dữ liệu sau:

- danh sách POI
- hình ảnh restaurant
- audio narration đã phát

Khi mất kết nối mạng:

- app đọc dữ liệu từ cache
- narration vẫn hoạt động nếu audio đã được cache trước đó.

---

### Performance Constraints

Để đảm bảo hiệu năng và tiết kiệm pin:

- không cập nhật GPS quá thường xuyên
- áp dụng debounce cho location updates
- chỉ tính khoảng cách khi có location update hợp lệ
- chỉ trigger narration khi điều kiện geofence được thỏa mãn
