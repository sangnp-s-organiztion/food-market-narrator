# MAUI Runtime Flows

## 1. Startup Flow

### 1.1 App startup

Khi app khởi động:

1. App.CreateWindow trả về AppShell.
2. App.OnStart chạy các tác vụ nền:
   - StartWarmupInBackground tải POI + Language.
   - AudioLibraryService.InitializeOnStartupAsync.
   - LocationLogSyncService.Start.
   - LocationService.StartTrackingAsync.

Đặc tính:

- Các tác vụ startup được chạy không chặn UI thread.
- Nếu startup không có internet, app vẫn giữ khả năng đọc dữ liệu đã cache.

### 1.2 MainPage first appear

MainPage.OnAppearing:

1. Subscribe LocationChanged để cập nhật map và UI near-POI.
2. StartTrackingAsync không await.
3. Khởi chạy InitializeMainPageAsync để:
   - Load map.
   - Bind danh sách POI.
   - Lấy location lần đầu.
4. Auto-start narration 1 lần mỗi phiên app bằng cờ static \_hasAutoStartedNarrationThisSession.
5. Nếu có cờ startup offline notice thì hiển thị alert.

## 2. Auto Narration Flow

### 2.1 Bật narration

MainPage.OnNarratorTapped:

- Bắt đầu/dừng narration ngay.
- Animation nút chạy song song, không chặn hành động.

NarrationFlowService.StartNarration:

1. Bật cờ \_isNarrationEnabled.
2. Reset trạng thái session:
   - \_playedPOIs.Clear().
   - \_poiLastPlayedTime.Clear().
   - \_lastProcessedLocation = null.
   - \_poiService.ResetGeofenceState().
3. Subscribe LocationChanged.
4. StartTrackingAsync chạy nền.
5. Nếu có LastKnownLocation thì CheckAndNarrateAsync ngay.
6. Nếu chưa có location cache thì fallback gọi GetCurrentLocationAsync ở background.

### 2.2 Trigger narration theo geofence

Khi có location mới:

1. Debounce theo khoảng cách: bỏ qua nếu dịch chuyển < 5m từ lần đã xử lý trước.
2. Gọi CheckAndNarrateAsync(location).
3. CheckAndNarrateAsync:
   - Nếu đang phát audio và không force thì bỏ qua.
   - Lấy POI list.
   - Gọi POIService.UpdateNearestPOI.
4. UpdateNearestPOI trả POI khi:
   - Enter: từ ngoài vào trong bán kính 30m.
   - Switch: chuyển sang POI khác trong vùng enter.
   - Exit: đi ra khỏi POI hiện tại > 40m (không trả POI, chỉ reset state).
5. TryPlayAudioAsync kiểm tra:
   - Khoảng cách <= TriggerDistanceMeters (30m) nếu không force.
   - Cooldown 60 giây cho cùng POI nếu không force.
   - POI chưa phát trong session nếu không force.
6. Nếu qua điều kiện thì enqueue audio và xử lý queue tuần tự.

### 2.3 Queue phát và logging

ProcessQueueAsync:

1. Đảm bảo chỉ một queue worker chạy bằng \_isProcessingQueue.
2. Dequeue từng item và gọi AudioService.PlaySound(audioId).
3. Chờ playback bắt đầu (tối đa 2 giây).
4. Nếu bắt đầu thành công:
   - AddToHistory(restaurantId).
   - Chờ đến khi playback kết thúc.
   - Gửi AudioLogSyncService.LogPlaybackAsync.

### 2.4 Dừng narration

NarrationFlowService.StopNarration:

1. Unsubscribe LocationChanged.
2. StopSound.
3. Clear queue và reset flag queue processing.
4. Reset anti-repeat/cooldown/debounce/geofence state.

Tác dụng:

- Bật lại narration tại cùng vị trí vẫn có thể trigger lại theo enter logic đã reset.

## 3. Manual Audio Flow ở POIDetailPage

Khi user bấm play trong trang chi tiết:

1. Resolve audio theo ngôn ngữ hiện tại.
2. Nếu đang phát đúng track:
   - Bấm lần nữa -> Pause.
3. Nếu đúng track nhưng paused:
   - Bấm -> Resume.
4. Nếu track khác/chưa phát:
   - PlaySound(audioId).
   - Chờ xác nhận started.
   - Bắt đầu progress timer 200ms.
5. Khi playback kết thúc hoặc page rời đi:
   - LogPlaybackIfPossible gửi audio log.

Ghi chú:

- Lịch sử được thêm khi audio thực sự ở trạng thái playing.

## 4. Map Interaction Flow

MapPage.OnAppearing:

1. Subscribe LocationChanged.
2. StartTrackingAsync.
3. Load map (nếu chưa load instance này).
4. Nạp POI list vào memory page.

Các flow chính:

- Tap marker:
  - Tính ngưỡng chạm theo độ zoom và resolution.
  - Nếu trúng marker thì hiện card POI.
- Search:
  - Debounce 220ms.
  - Normalize text bỏ dấu.
  - Highlight tập kết quả và focus quán đầu tiên.
- My location:
  - Lấy vị trí hiện tại và center map.

## 5. Settings Flow

SettingsPage hỗ trợ:

- Đổi ngôn ngữ narration/UI culture.
- Xóa cache audio.
- Xóa lịch sử.
- Xóa danh sách yêu thích.
- Yêu cầu quyền location background trên Android.

Flow đổi ngôn ngữ:

1. Load danh sách language từ API/cache.
2. Chọn language mới.
3. LanguageService.ChangeLanguage đổi culture thread và reload AppShell.
4. Nếu đang narration, code hiện tại gọi StartNarration lại, nhưng StartNarration có guard IsNarrating nên không tái khởi động khi đang bật.

## 6. Logging and Telemetry Flow

### 6.1 Session and location logs

LocationLogSyncService:

- Mỗi lần LocationSampled tạo item gồm sessionId, timestamp, geojson point/null.
- Buffer tối đa 2000 item (quá thì drop item cũ nhất).
- Flush định kỳ mỗi 10 giây.
- Khi flush fail thì chèn lại batch vào đầu buffer để retry.

### 6.2 Audio logs

AudioLogSyncService:

- Gửi log gồm sessionId, restaurantId, audioId, start/end/duration.
- Trước khi gửi audio log luôn gọi ensure session start endpoint.
- Nếu backend trả 404 + Session not found:
  - Ensure session start lại.
  - Flush location logs ngay.
  - Retry gửi audio log 1 lần.

## 7. Background Tracking Flow Android

LocationService.StartTrackingAsync:

- Ensure quyền foreground location.
- Nếu granted thì gọi StartForegroundTrackingServiceIfNeeded.
- Tạo tracking loop poll location mỗi 2 giây.

TrackingForegroundService:

- Chạy foreground notification low priority.
- Có nút Dung để stop service.

Khi StopTracking:

- Cancel tracking loop.
- Dừng foreground service.

## 8. Error Handling Pattern

Pattern chính trong app:

- Best effort + fallback local cache.
- Nhiều khối try/catch swallow exception để giữ UX không crash.
- Với sync log: thất bại không làm vỡ luồng chính, retry ở tick sau.
