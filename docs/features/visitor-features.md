# Yêu cầu tính năng dành cho Khách tham quan

## 1. Theo dõi vị trí

Ứng dụng phải theo dõi vị trí của khách tham quan theo thời gian thực.

### Yêu cầu

- Theo dõi vị trí GPS của người dùng liên tục.
- Hỗ trợ cả hai chế độ:
  - Theo dõi vị trí ở chế độ nền trước (foreground)
  - Theo dõi vị trí ở chế độ nền (background)

- Tối ưu hóa mức tiêu thụ pin.
- Đảm bảo độ chính xác vị trí chấp nhận được.

### Tiêu chí chấp nhận

- Ứng dụng có thể phát hiện chuyển động của khách tham quan.
- Cập nhật vị trí diễn ra định kỳ mà không gây hao pin quá mức.

---

# 2. Hiển thị bản đồ

Khách tham quan có thể xem vị trí của mình và các Điểm quan tâm (POI) lân cận.

### Tính năng

- Hiển thị vị trí người dùng trên bản đồ.
- Hiển thị tất cả POI có sẵn.
- Đánh dấu POI gần nhất.
- Cho phép khách tham quan nhấn vào POI để xem chi tiết.

### Chi tiết POI bao gồm

- Mô tả văn bản
- Hình ảnh
- Vị trí trên bản đồ
- Audio thuyết minh hoặc kịch bản TTS

---

# 3. Thuyết minh tự động (Kích hoạt Geofence)

Ứng dụng tự động phát thuyết minh khi khách tham quan đến gần một POI.

### Hành vi

- Phát hiện khi khách tham quan đi vào vùng geofence.
- Kích hoạt thuyết minh tự động.

### Cấu hình Geofence

Mỗi POI chứa:

- Vĩ độ / Kinh độ
- Bán kính kích hoạt
- Mức ưu tiên

### Quy tắc bổ sung

- Ngăn chặn thuyết minh lặp lại bằng cách sử dụng cooldown.
- Sử dụng logic debounce để tránh kích hoạt quá thường xuyên.

---

# 4. Thuyết minh audio

Thuyết minh được phát khi thăm các POI.

### Phương pháp được hỗ trợ

#### Chuyển văn bản thành giọng nói (TTS)

- Hỗ trợ đa ngôn ngữ
- Dung lượng lưu trữ nhẹ

#### Audio đã ghi sẵn

- Giọng nói tự nhiên hơn
- Chất lượng thuyết minh cao hơn

### Quản lý audio

- Quản lý hàng đợi phát audio.
- Tránh chồng lấn phát audio.
- Tạm dừng thuyết minh khi có gián đoạn hệ thống.

---

# 5. Kích hoạt nội dung qua mã QR

Khách tham quan có thể truy cập thuyết minh qua mã QR.

### Trường hợp sử dụng

- Trạm xe buýt
- Điểm du lịch
- Bảng thông tin

### Hành vi

- Quét mã QR để mở nội dung POI.
- Audio phát ngay lập tức.
- GPS không bắt buộc.

---

# 6. Quyền riêng tư của người dùng

Dữ liệu di chuyển của khách tham quan phải được ẩn danh.

### Yêu cầu

- Không lưu trữ thông tin người dùng có thể nhận dạng.
- Lưu trữ phân tích vị trí một cách ẩn danh.
