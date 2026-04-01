# Luồng Narration Trigger và Geofence (trạng thái hiện tại)

Tài liệu này mô tả đúng theo code hiện tại.

## 1) Tổng quan nhanh

- NarrationFlowService sử dụng POIService.UpdateNearestPOI(...) để phát hiện geofence transition.
- Có hai cơ chế chống lặp: \_playedPOIs (theo phiên) và cooldown 60 giây (theo thời gian).
- Enter radius: 30m, Exit radius: 40m (hysteresis chống rung biên).

## 2) Luồng thực thi hiện tại

### 2.1 Bật narration

Khi gọi StartNarration():

1. Đánh dấu isNarrationEnabled = true.
2. Clear các state: \_playedPOIs, \_poiLastPlayedTime, \_lastProcessedLocation.
3. Subscribe LocationChanged từ LocationService.
4. Bắt đầu tracking.
5. Lấy vị trí hiện tại và gọi CheckAndNarrateAsync(...) ngay một lần.

### 2.2 Auto trigger khi vị trí thay đổi

Mỗi LocationChanged sẽ gọi CheckAndNarrateAsync(location, force: false):

1. Bỏ qua nếu đang phát audio (trừ force).
2. Lấy danh sách POI.
3. Gọi UpdateNearestPOI(lat, lng) để kiểm tra geofence transition.
4. Nếu có POI mới (enter hoặc switch POI) HOẶC force = true:
   - Nếu force: dùng GetNearestPOI.
   - Nếu auto: dùng POI trả về từ UpdateNearestPOI.
5. Kiểm tra khoảng cách <= TriggerDistanceMeters (30m).
6. Kiểm tra cooldown 60 giây.
7. Kiểm tra đã phát trong phiên chưa (\_playedPOIs).
8. Nếu hợp lệ: enqueue và phát audio.

### 2.3 Manual trigger

Khi force = true:

- Bỏ qua kiểm tra IsPlaying.
- Bỏ qua kiểm tra khoảng cách 30m.
- Bỏ qua kiểm tra cooldown.
- Cho phép phát lại POI đã phát trong phiên.

## 3) Cơ chế chống lặp hiện tại

- **\_playedPOIs**: HashSet lưu POI đã phát trong phiên narration. Reset khi StopNarration().
- **\_poiLastPlayedTime**: Dictionary lưu thời gian phát gần nhất mỗi POI (cooldown 60 giây).
- **Queue**: \_playQueue để xử lý tuần tự nhiều POI chờ phát.

## 4) Geofence trong POIService

POIService.UpdateNearestPOI(lat, lng) có state machine:

- Enter radius: 30m (PoiEnterRadiusMeters).
- Exit radius: 40m (PoiExitRadiusMeters).
- Trả về POI mới khi:
  - Chưa ở trong POI nào và vào vùng 30m.
  - Đang trong POI và chuyển sang POI khác trong vùng 30m.
- Reset trạng thái khi ra khỏi vùng exit (40m).

## 5) Audio queue và xử lý

NarrationFlowService dùng queue để xử lý tuần tự:

1. Khi trigger hợp lệ, POI được enqueue.
2. ProcessQueueAsync xử lý từng POI:
   - Gọi AudioService.PlaySound.
   - Lưu vào History khi bắt đầu phát thành công.
   - Chờ audio phát xong mới xử lý tiếp.

## 6) Checklist test nhanh

1. Bật narration tự động, đi vào vùng <= 30m của POI mới: audio phát.
2. Đứng yên trong vùng đó: không phát lặp lại (cooldown 60 giây).
3. Đi ra ngoài 40m rồi vào lại: có thể phát lại.
4. Tắt rồi bật narration lại ở cùng vị trí: có thể phát lại do \_playedPOIs đã reset.
5. Trigger manual với force=true: phát ngay nearest POI dù đang xa hơn ngưỡng 30m.
6. Test chuyển giữa 2 POI gần nhau trong vùng 30m: phát lần lượt từng quán.
