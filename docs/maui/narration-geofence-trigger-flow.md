# Luồng Narration Trigger và Geofence (trạng thái hiện tại)

Tài liệu này mô tả đúng theo code hiện tại, đồng thời chỉ rõ phần nào đã triển khai và phần nào mới ở mức hỗ trợ nền tảng.

## 1) Tổng quan nhanh

- NarrationFlowService hiện trigger tự động chủ yếu theo khoảng cách (distance <= TriggerDistanceMeters).
- POIService đã có hàm UpdateNearestPOI(...) theo mô hình enter/exit geofence (30m/40m), nhưng chưa được NarrationFlowService dùng cho auto trigger.
- Chống phát lặp hiện dùng HashSet \_playedPOIs theo phiên narration, không phải cooldown theo thời gian.

## 2) Luồng thực thi hiện tại

### 2.1 Bật narration

Khi gọi StartNarration():

1. Đánh dấu isNarrationEnabled = true.
2. Subscribe LocationChanged từ LocationService.
3. Bắt đầu tracking.
4. Lấy vị trí hiện tại và gọi CheckAndNarrateAsync(...) ngay một lần.

### 2.2 Auto trigger khi vị trí thay đổi

Mỗi LocationChanged sẽ gọi CheckAndNarrateAsync(location, force: false):

1. Lấy danh sách POI.
2. Tìm nearest bằng GetNearestPOI(...).
3. Tính khoảng cách đến nearest.
4. Nếu khoảng cách <= TriggerDistanceMeters (30m) thì xét phát audio.
5. Nếu POI chưa có trong \_playedPOIs thì enqueue và phát.

### 2.3 Manual trigger

Khi force = true:

- Bỏ qua check khoảng cách 30m.
- Vẫn lấy nearest và cho phép phát kể cả POI đã từng phát trong phiên.

## 3) Cơ chế chống lặp hiện tại

- \_playedPOIs lưu danh sách POI đã phát trong phiên narration.
- Khi StopNarration(), \_playedPOIs được clear.
- Không có timer cooldown theo phút/giây trong NarrationFlowService hiện tại.

## 4) Geofence hiện có trong POIService

POIService.UpdateNearestPOI(lat, lng) đã có state machine:

- Enter radius: 30m.
- Exit radius: 40m (hysteresis để chống rung biên).
- Có thể trả POI mới khi chuyển vùng hợp lệ.

Tuy nhiên, ở thời điểm hiện tại NarrationFlowService chưa gọi hàm này trong luồng auto trigger.

## 5) Hệ quả vận hành

- Dễ hiểu và chạy ổn định trong đa số trường hợp.
- Ở khu POI rất dày, auto trigger vẫn phụ thuộc nearest theo từng tick vị trí thay vì geofence transition chính thức.
- Cơ chế “chỉ phát một lần mỗi phiên” giúp giảm lặp âm thanh, nhưng chưa phải cooldown thời gian.

## 6) Đề xuất nâng cấp tiếp theo

1. Chuyển auto mode sang dùng UpdateNearestPOI(...) để trigger theo enter/switch geofence.
2. Bổ sung cooldown theo thời gian (ví dụ 60-120 giây/POI) nếu cần replay có kiểm soát.
3. Giữ force/manual trigger phát ngay để không ảnh hưởng thao tác chủ động của người dùng.

## 7) Checklist test nhanh

1. Bật narration tự động, đi vào vùng <= 30m của POI mới: audio phát.
2. Đứng yên trong vùng đó: không phát lặp lại trong cùng phiên.
3. Tắt rồi bật narration lại ở cùng vị trí: có thể phát lại do \_playedPOIs đã reset.
4. Trigger manual với force=true: phát ngay nearest POI dù đang xa hơn ngưỡng 30m.
