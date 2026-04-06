# MAUI Architecture Overview

## 1. Mục tiêu tài liệu

Tài liệu này mô tả kiến trúc kỹ thuật hiện tại của ứng dụng mobile trong thư mục FoodMarketNarrator.Maui, tập trung vào:

- Kiến trúc khởi tạo và dependency injection.
- Vai trò của từng service lõi.
- Cấu trúc navigation và lifecycle.
- Quy ước dữ liệu và các điểm mở rộng.

Phạm vi phản ánh đúng code hiện tại tại thời điểm cập nhật tài liệu.

## 2. Cấu trúc thành phần

Ứng dụng MAUI chia thành các nhóm chính:

- Views: MainPage, MapPage, POIDetailPage, FavoritePage, HistoryPage, SettingsPage.
- Services: POI, Location, NarrationFlow, Audio, AudioLibrary, Language, Favorite, History, LocationLogSync, AudioLogSync.
- Models: POI, AudioModel, DishModel, LanguageModel, RestaurantImageModel và các payload sync.
- Helpers: MapHelper.
- Settings: AppSettings.
- Platform Android: MainActivity, TrackingForegroundService.

## 3. Dependency Injection và vòng đời service

Đăng ký trong MauiProgram sử dụng singleton cho hầu hết service dữ liệu/tracking:

- Singleton:
  - IPOIService -> POIService
  - IAudioService -> AudioService
  - IAudioLibraryService -> AudioLibraryService
  - ILanguageService -> LanguageService
  - NarrationFlowService
  - IFavoriteService -> FavoriteService
  - IHistoryService -> HistoryService
  - ILocationService -> LocationService
  - ILocationLogSyncService -> LocationLogSyncService
  - IAudioLogSyncService -> AudioLogSyncService
- Transient page:
  - MainPage, MapPage, POIDetailPage, FavoritePage, HistoryPage, SettingsPage

Ý nghĩa kiến trúc:

- State runtime được giữ xuyên suốt phiên app nhờ singleton.
- Cache in-memory của POI/Language tồn tại cho đến khi process app kết thúc.
- NarrationFlowService giữ trạng thái anti-repeat và queue ở mức toàn app.

## 4. HttpClient và cấu hình mạng

App dùng một HttpClient singleton, BaseAddress lấy từ AppSettings.ApiBaseUrl.

- Android emulator: host 10.0.2.2.
- Android device thật: host LocalApiHost (hiện tại 192.168.1.8).
- Fallback URL: cả HTTP và HTTPS theo host active.
- HttpClientHandler đang bật DangerousAcceptAnyServerCertificateValidator.

Nhận xét:

- Ứng dụng ưu tiên chạy được trong môi trường nội bộ/dev.
- Cần kiểm soát cấu hình chứng chỉ khi phát hành production.

## 5. App lifecycle cấp cao

Trong App.OnStart:

- Warm-up nền:
  - Tải POI list.
  - Tải danh sách ngôn ngữ.
- Khởi tạo audio library sync nền.
- Bắt đầu LocationLogSyncService.
- Bắt đầu location tracking.

Trong App.OnSleep:

- Flush location log ngay 1 lần qua LocationLogSyncService.FlushNowAsync.

## 6. Navigation và route

Shell route chính:

- MainPage
- MapPage
- FavoritePage
- HistoryPage
- SettingsPage

Route đăng ký thêm (detail navigation):

- POIDetailPage

Deep link Android:

- Scheme: foodmarketnarrator://open
- MainActivity xử lý intent filter và giữ LaunchMode SingleTop.

## 7. Trách nhiệm từng service

POIService:

- Tải danh sách quán từ API.
- Fallback sang offline cache file pois.json.
- Cung cấp thuật toán nearest POI và geofence transition enter/switch/exit.
- Cung cấp dish theo restaurantId.

LocationService:

- Quản lý quyền vị trí foreground/background.
- Poll GPS theo chu kỳ (2 giây).
- Publish LocationChanged khi dịch chuyển đủ ngưỡng.
- Publish LocationSampled cho telemetry ngay cả khi không có location.

NarrationFlowService:

- Điều phối auto narration theo geofence.
- Chống phát lặp theo session (\_playedPOIs).
- Cooldown theo thời gian cho POI (\_poiLastPlayedTime).
- Queue phát tuần tự và ghi lịch sử/audio log.

AudioService:

- Phát audio theo language+file hoặc audioId.
- Quản lý cache audio local (quota + LRU).
- Prefetch audio nền.
- Pause/Resume/Stop và trạng thái track hiện tại.
- Trên Android: quản lý audio focus interruption.

AudioLibraryService:

- Sync thư viện audio lúc startup.
- Theo dõi version audio qua manifest local.
- Xử lý luồng first install khi offline.

LanguageService:

- Lấy danh sách ngôn ngữ từ API hoặc cache offline.
- Đổi culture hiện tại và giữ nguyên navigation stack (không reset AppShell).

FavoriteService:

- Lưu danh sách favorite vào Preferences (persistent).

HistoryService:

- Lưu lịch sử trong memory (không persistent), giới hạn 50 mục.

LocationLogSyncService:

- Buffer location sample và flush batch định kỳ 10 giây.
- Khởi tạo user session trên backend.
- Persist buffer location log xuống file local để không mất dữ liệu khi app bị kill lúc offline.

AudioLogSyncService:

- Gửi audio playback log.
- Retry khi backend báo Session not found.

## 8. Các cấu hình hành vi quan trọng

Trong AppSettings:

- TriggerDistanceMeters: 30m.
- PoiEnterRadiusMeters: 30m.
- PoiExitRadiusMeters: 40m.
- MapHighlightDistanceMeters: 20m.

Trong LocationService:

- PollInterval: 2 giây.
- MinPublishDistanceMeters: 6m.
- Geolocation request timeout: 10 giây.

Trong AudioService:

- MaxAudioCacheBytes: 200MB.
- MinDeviceFreeSpaceBytes: 50MB.
- MinValidAudioBytes: 256 bytes.

## 9. Android-specific behavior

Manifest quyền chính:

- ACCESS_FINE_LOCATION
- ACCESS_COARSE_LOCATION
- ACCESS_BACKGROUND_LOCATION
- FOREGROUND_SERVICE
- FOREGROUND_SERVICE_LOCATION
- POST_NOTIFICATIONS
- INTERNET, ACCESS_NETWORK_STATE

Foreground service:

- TrackingForegroundService chạy notification ongoing khi tracking nền.
- Có action STOP để dừng foreground service.

## 10. Điểm cần lưu ý khi mở rộng

- Không đổi lifetime singleton của NarrationFlowService/POIService nếu chưa đánh giá tác động anti-repeat và cache.
- Nếu thêm chính sách TTL cho cache file, cần đồng bộ với logic fallback offline hiện tại.
- Nếu đổi endpoint backend, cần cập nhật AppSettings và tài liệu docs/architecture/maui-api-and-sync-contracts.md.
- SettingsPage hiện dùng một số API DisplayAlert/FadeTo đã obsolete theo warning build; chưa ảnh hưởng chức năng nhưng nên có kế hoạch nâng cấp dần.
