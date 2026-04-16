# Flow Sequence App

Tài liệu mô tả flow thực tế của ứng dụng mobile trong thư mục FoodMarketNarrator.Maui (snapshot theo code hiện tại).

## Phạm vi và ghi chú

- App dùng .NET MAUI, điều hướng bằng AppShell với 6 tab: MainPage, MapPage, TourPage, FavoritePage, HistoryPage, SettingsPage.
- Khi app start sẽ chạy warm-up nền, khởi động tracking GPS và đồng bộ log location.
- Narration tự chạy theo geofence khi bật narration flow.
- Deep link hiện nhận scheme foodmarketnarrator://open, validate hợp lệ rồi apply qua QrAccessService.
- Dữ liệu có cơ chế offline cache cho POI, language, dishes, tours, images, audio.

## Tóm tắt chức năng theo sequence

1.Khởi Động App
Khởi tạo AppShell và giao diện tab, xử lý deep link đầu vào (nếu có), đồng thời kích hoạt các dịch vụ nền quan trọng gồm warm-up dữ liệu, khởi tạo audio, đồng bộ location log và bắt đầu tracking GPS.

2.Warm-up dữ liệu nền
Tải sẵn language, tour, POI và khởi chạy job làm ấm dữ liệu phụ (ảnh, món ăn) để giảm độ trễ khi người dùng mở các tab.

3.Bootstrap audio library khi startup
Chuẩn bị audio theo trạng thái online/offline, đồng bộ bản mới, đặt cờ sẵn sàng phát để narration không bị khựng.

4.Theo dõi vị trí và quyền truy cập
Xin quyền location/notification theo đúng Android version, bật foreground service và publish sự kiện vị trí theo ngưỡng di chuyển.

5.Đồng bộ session và location logs
Mở phiên theo dõi, gửi log vị trí định kỳ; nếu lỗi thì giữ batch để retry, tránh mất dữ liệu tracking.

6.Geofence enter/exit/switch
Từ luồng vị trí, xác định vào vùng, ra vùng, hoặc chuyển POI; có hysteresis để tránh rung biên.

7.Bật/tắt narration tự động
Quản lý vòng đời chế độ thuyết minh: start thì subscribe tracking và reset state, stop thì dọn queue và dừng audio.

8.Trigger phát audio theo POI
Áp dụng rule chống lặp (distance, cooldown, played list), chọn audio theo ngôn ngữ rồi phát và ghi nhận đã phát.

9.Playback nguồn audio và cache
Ưu tiên cache local, nếu thiếu mới tải mạng và lưu lại theo chính sách LRU để dùng lại offline.

10.Ghi audio logs khi phát
Ghi lại thời điểm bắt đầu/kết thúc playback, gửi backend phục vụ thống kê và phân tích hành vi nghe.

11.MainPage: danh sách POI và điều hướng chi tiết
Tải danh sách POI, filter theo ngữ cảnh và điều hướng vào trang chi tiết từng địa điểm.

12.MapPage: lọc/scope POI và tương tác bản đồ
Hiển thị marker, hỗ trợ tìm kiếm/lọc, giới hạn scope theo tour và điều hướng nhanh sang chi tiết.

13.POIDetail: phát thủ công, favorite, chỉ đường
Cung cấp thao tác trực tiếp trên một POI: nghe audio thủ công, thêm yêu thích, mở app bản đồ để dẫn đường.

14.Tour flow
Tải danh sách tour, cho phép bắt đầu tour để nhảy sang map với danh sách điểm dừng đúng thứ tự.

15.Favorites và History flow
Hiển thị danh sách yêu thích và lịch sử đã xem/nghe, kết hợp dữ liệu POI để render thông tin đầy đủ.

16.Settings flow
Đổi ngôn ngữ, xin quyền vị trí nền, xóa cache/dữ liệu cục bộ, dọn lịch sử/yêu thích.

17.Deep link QR flow
Nhận link từ Android intent, dispatch vào app, validate scheme/host rồi apply qua QrAccessService.

## 1. Khởi tạo ứng dụng

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant APP as App
    participant SHELL as AppShell
    participant WARM as Warmup Services
    participant LOC as Location Service
    participant LOG as Location Log Sync

    U->>APP: Mở ứng dụng
    APP->>SHELL: CreateWindow(AppShell)
    APP->>APP: Handle deep link command-line (nếu có)
    APP->>WARM: StartWarmupInBackground()
    APP->>WARM: AudioLibrary.InitializeOnStartupAsync()
    APP->>LOG: Start()
    APP->>LOC: StartTrackingAsync()
    SHELL-->>U: Hiển thị giao diện tab
```

## 2. Warm-up dữ liệu nền

```mermaid
sequenceDiagram
    autonumber
    participant APP as App
    participant LANG as Language Service
    participant TOUR as Tour Service
    participant POI as POI Service

    APP->>APP: Delay StartupWarmupDelayMs
    APP->>LANG: GetAllLanguagesAsync()
    APP->>TOUR: GetToursAsync()
    APP->>POI: GetAllPOIsAsync()
    POI->>POI: Start offline image/dishes warmup jobs
```

## 3. Bootstrap audio library khi startup

```mermaid
sequenceDiagram
    autonumber
    participant APP as App
    participant LIB as AudioLibraryService
    participant POI as POIService
    participant AUD as AudioService

    APP->>LIB: InitializeOnStartupAsync()
    alt audio_ready=false và có internet
        LIB->>POI: GetAllPOIsAsync()
        LIB->>AUD: PrefetchAudioAsync(audioId) cho audio active
        LIB->>LIB: Set Preferences audio_ready=true khi đủ dữ liệu
    else audio_ready=true và có internet
        LIB->>POI: GetAllPOIsAsync()
        LIB->>AUD: PrefetchAudioAsync(audioId) cho bản mới hơn
    else offline
        LIB->>LIB: Queue notice first-install offline (nếu cần)
    end
```

## 4. Theo dõi vị trí và quyền truy cập

```mermaid
sequenceDiagram
    autonumber
    participant FE as App
    participant LOC as Location Service
    participant OS as Android Permissions
    participant FG as TrackingForegroundService

    FE->>LOC: StartTrackingAsync()
    LOC->>OS: Check/Request LocationWhenInUse
    alt Được cấp quyền
        LOC->>OS: Request PostNotifications (Android 13+)
        LOC->>FG: Start foreground service (Android)
        LOC->>LOC: RunTrackingLoop 2s/lần
        LOC->>LOC: Publish LocationChanged khi di chuyển >= 6m
        LOC->>LOC: Publish LocationSampled cho mọi mẫu
    else Bị từ chối
        LOC-->>FE: Không tracking được
    end
```

## 5. Đồng bộ session và location logs

```mermaid
sequenceDiagram
    autonumber
    participant APP as App
    participant LSYNC as LocationLogSyncService
    participant API as Backend API

    APP->>LSYNC: Start()
    LSYNC->>API: POST /api/user-sessions/start
    loop Mỗi 10 giây
        LSYNC->>API: POST /api/location-logs/batch
        alt Gửi lỗi
            LSYNC->>LSYNC: Restore batch vào buffer để retry
        else Thành công
            LSYNC->>LSYNC: Xóa batch đã gửi
        end
    end
```

## 6. Geofence enter/exit/switch

```mermaid
sequenceDiagram
    autonumber
    participant LOC as LocationChanged Event
    participant NF as NarrationFlowService
    participant POI as POIService

    LOC->>NF: OnLocationChanged(location)
    NF->>NF: Debounce nếu di chuyển < 5m thì bỏ qua
    NF->>POI: UpdateNearestPOI(lat,lng)
    Note over POI: Enter radius=30m, Exit radius=40m
    alt Enter hoặc switch POI
        POI-->>NF: Trả về POI mới để trigger
    else Không đổi trạng thái geofence
        POI-->>NF: null
    end
```

## 7. Bật/tắt narration tự động

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant MAIN as MainPage
    participant NF as NarrationFlowService
    participant LOC as LocationService

    U->>MAIN: Bấm nút thuyết minh
    alt Chưa narrating
        MAIN->>NF: StartNarration()
        NF->>NF: Reset played POIs + cooldown map + geofence state
        NF->>LOC: Subscribe LocationChanged + StartTrackingAsync()
        NF->>NF: CheckAndNarrateAsync với vị trí cache/current
    else Đang narrating
        MAIN->>NF: StopNarration()
        NF->>NF: Unsubscribe location + clear queue + stop audio
    end
```

## 8. Trigger phát audio theo POI

```mermaid
sequenceDiagram
    autonumber
    participant NF as NarrationFlowService
    participant POI as POIService
    participant LANG as LanguageService
    participant AUD as AudioService

    NF->>POI: Lấy POI target từ geofence transition
    NF->>NF: Check distance <= TriggerDistanceMeters (30m)
    NF->>NF: Check cooldown 60s/POI
    NF->>NF: Check played POIs trong session
    NF->>LANG: Lấy CurrentLanguage
    NF->>NF: Resolve audio active theo language, fallback audio active mới nhất
    NF->>AUD: Enqueue và PlaySound(audioId)
    NF->>NF: Ghi _poiLastPlayedTime + _playedPOIs
```

## 9. Playback nguồn audio và cache

```mermaid
sequenceDiagram
    autonumber
    participant NF as NarrationFlowService
    participant AUD as AudioService
    participant CACHE as Local Cache
    participant NET as Network

    NF->>AUD: PlaySound(audioId)
    AUD->>CACHE: Tìm file trong audio_cache
    alt Cache hit
        CACHE-->>AUD: Stream local
    else Cache miss
        AUD->>NET: Tải audio từ endpoint candidates
        NET-->>AUD: Audio bytes
        AUD->>CACHE: Save cache (kèm LRU cleanup khi cần)
    end
    AUD-->>NF: Playback started/ended
```

## 10. Ghi audio logs khi phát

```mermaid
sequenceDiagram
    autonumber
    participant NF as NarrationFlowService
    participant ASYNC as AudioLogSyncService
    participant API as Backend API

    NF->>NF: Khi audio bắt đầu phát: lưu thời điểm start
    NF->>NF: Khi audio kết thúc: tính duration
    NF->>ASYNC: LogPlaybackAsync(sessionId, restaurantId, audioId, start, end)
    ASYNC->>API: POST /api/audio-logs
    alt Session chưa tồn tại backend
        ASYNC->>API: POST /api/user-sessions/start
        ASYNC->>API: Retry POST /api/audio-logs
    end
```

## 11. MainPage: danh sách POI và điều hướng chi tiết

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant MAIN as MainPage
    participant POI as POIService
    participant MAP as MapHelper

    U->>MAIN: Mở tab Trang chủ
    MAIN->>MAP: LoadMapAsync (1 lần)
    MAIN->>POI: GetAllPOIsAsync
    MAIN->>MAIN: Bind danh sách POI + filter (All/Nearby/Favorite/Open)
    U->>MAIN: Chọn 1 POI
    MAIN-->>U: Navigate POIDetailPage?restaurantId=...
```

## 12. MapPage: lọc/scope POI và map interaction

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant MAP as MapPage
    participant POI as POIService
    participant NF as NarrationFlowService

    U->>MAP: Mở tab Bản đồ
    MAP->>POI: GetAllPOIsAsync (nếu chưa có)
    MAP->>NF: SetAutoNarrationPoiScope theo tourPoiIds (nếu có)
    U->>MAP: Search/filter hoặc tap marker
    MAP->>MAP: Highlight POI + hiển thị card chi tiết
    U->>MAP: Bấm chi tiết
    MAP-->>U: Navigate POIDetailPage
```

## 13. POIDetail: phát audio thủ công, favorite, direction

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant DETAIL as POIDetailPage
    participant POI as POIService
    participant AUD as AudioService
    participant FAV as FavoriteService

    U->>DETAIL: Mở chi tiết POI
    DETAIL->>POI: GetPOIByIdAsync
    DETAIL->>POI: GetDishesByRestaurantIdAsync
    U->>DETAIL: Bấm Play/Pause audio
    DETAIL->>AUD: PlaySound(audioId) hoặc Pause/Resume
    U->>DETAIL: Bấm yêu thích
    DETAIL->>FAV: AddFavorite/RemoveFavorite
    U->>DETAIL: Bấm Đường đi
    DETAIL-->>U: Open map launcher với destination lat,lng
```

## 14. Tour flow

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant TOURP as TourPage
    participant TS as TourService
    participant MAP as MapPage

    U->>TOURP: Mở tab Hành trình
    TOURP->>TS: GetToursAsync
    TS->>TS: Dùng memory/disk cache; refresh network nếu cần
    TOURP-->>U: Hiển thị tour active
    U->>TOURP: Bấm Bắt đầu hoặc Xem chi tiết
    alt Bắt đầu
        TOURP-->>MAP: Navigate //MapPage?tourPoiIds&tourName&tourStopOrders
    else Xem chi tiết
        TOURP-->>U: Navigate TourDetailPage?tourId
    end
```

## 15. Favorites và History flow

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant FAVP as FavoritePage
    participant HISTP as HistoryPage
    participant FAV as FavoriteService
    participant HIST as HistoryService
    participant POI as POIService

    U->>FAVP: Mở tab Yêu thích
    FAVP->>FAV: GetFavorites() từ Preferences
    FAVP->>POI: GetAllPOIsAsync
    FAVP-->>U: Render danh sách favorite

    U->>HISTP: Mở tab Lịch sử
    HISTP->>HIST: GetHistory() (in-memory)
    HISTP->>POI: GetAllPOIsAsync
    HISTP-->>U: Render danh sách đã nghe/xem gần đây
```

## 16. Settings flow

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant SET as SettingsPage
    participant LANG as LanguageService
    participant LOC as LocationService
    participant AUD as AudioService
    participant HIST as HistoryService
    participant FAV as FavoriteService

    U->>SET: Mở tab Cài đặt
    SET->>LANG: GetAllLanguagesAsync
    SET->>SET: Tính dung lượng cache map/image/audio/offline

    U->>SET: Chọn ngôn ngữ
    SET->>LANG: ChangeLanguage(cultureCode)

    U->>SET: Bật quyền vị trí nền
    SET->>LOC: RequestBackgroundLocationPermissionAsync

    U->>SET: Xóa cache audio/toàn bộ dữ liệu
    SET->>AUD: ClearAudioCacheAsync
    SET->>SET: Delete offline_cache/image_cache/map_cache

    U->>SET: Xóa lịch sử/yêu thích
    SET->>HIST: ClearHistory()
    SET->>FAV: RemoveFavorite(*)
```

## 17. Deep link QR flow

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant AND as Android Intent
    participant MA as MainActivity
    participant DISP as AppLinkDispatcher
    participant APP as App
    participant QR as QrAccessService

    U->>AND: Mở link foodmarketnarrator://open
    AND->>MA: OnCreate/OnNewIntent
    MA->>DISP: Dispatch(url)
    DISP->>APP: DeepLinkReceived event
    APP->>QR: ApplyDeepLink(url)
    QR->>QR: Validate scheme=foodmarketnarrator, host=open
    QR-->>APP: Accept/ignore deep link
```

---

## Endpoint chính app đang gọi

- GET /restaurant
- GET /language
- GET /tour
- GET /tour/{id}
- GET /Restaurant/{restaurantId}/dishes
- POST /api/user-sessions/start
- POST /api/location-logs/batch
- POST /api/audio-logs

---

## Các flow đã bỏ hoặc đã đổi so với bản cũ

- Deep link hiện mới dừng ở bước validate URL hợp lệ, chưa có nhánh điều hướng nghiệp vụ chi tiết theo tham số.
- History đang lưu in-memory (reset khi app đóng), còn favorite lưu bằng Preferences (persist qua phiên).
- Audio playback ưu tiên local cache trước, không phụ thuộc mạng nếu file đã được prefetch.