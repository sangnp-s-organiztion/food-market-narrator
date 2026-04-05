# MAUI Caching and Offline Behavior

## 1. Mục tiêu

Tài liệu này mô tả chi tiết toàn bộ cache trong MAUI app:

- Cache nằm ở đâu.
- Cache theo key nào.
- Cache tồn tại bao lâu.
- Cơ chế làm mới/xóa.
- Hành vi khi offline.

## 2. Cache Matrix

| Loại dữ liệu                 | Tầng lưu           | Đường dẫn/Key                                                                  | TTL thời gian                            | Chính sách xóa/làm mới                                                                                      |
| ---------------------------- | ------------------ | ------------------------------------------------------------------------------ | ---------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| POI list                     | In-memory + file   | \_pois + AppData/offline_cache/pois.json + \_lastFetchUtc                      | TTL 3 phút (chỉ cho in-memory)           | Hết TTL sẽ thử refresh; chỉ cập nhật \_lastFetchUtc khi fetch API thành công; giữ cache cũ khi refresh fail |
| POI images                   | File cache         | AppData/image_cache/{SHA256(imageUrl)}.ext                                     | Không có TTL theo thời gian              | Prefetch nền sau khi có POI; nếu cache tồn tại thì ưu tiên path local                                       |
| Dishes theo nhà hàng         | File JSON          | AppData/offline_cache/dishes/{restaurantId}.json                               | Không có TTL theo thời gian              | Ghi đè khi gọi API thành công; fallback file cache khi offline/API fail                                     |
| Languages                    | In-memory + file   | \_cachedLanguages + AppData/offline_cache/languages.json                       | Không có TTL theo thời gian              | Ghi đè khi fetch API thành công; giữ cache cũ khi API fail                                                  |
| Audio file                   | File cache         | AppData/audio_cache/{SHA256}.ext                                               | Không có TTL theo thời gian              | LRU eviction theo quota/space, hoặc xóa thủ công từ Settings                                                |
| Audio manifest version       | File JSON          | AppData/audio_manifest.json                                                    | Không có TTL theo thời gian              | Cập nhật khi prefetch/sync audio thành công                                                                 |
| Map tile OSM                 | File cache library | CacheDirectory/osm_tiles                                                       | Không cấu hình TTL tường minh trong code | Do BruTile FileCache quản lý; app không có lệnh clear riêng                                                 |
| Favorites                    | Preferences        | favorite_restaurants                                                           | Không TTL, persistent                    | Chỉ xóa khi user remove/clear/app data clear                                                                |
| Language preference          | Preferences        | AppLanguage                                                                    | Không TTL, persistent                    | Ghi đè khi user đổi ngôn ngữ                                                                                |
| Audio-ready state            | Preferences        | audio_ready                                                                    | Không TTL, persistent                    | Set true khi full sync thành công                                                                           |
| Startup offline notice flags | Preferences        | audio_first_install_offline_notice_shown, audio_startup_offline_notice_pending | Không TTL                                | Theo flow first install audio                                                                               |
| Tracking device id           | Preferences        | tracking_device_id                                                             | Không TTL, persistent                    | Tạo một lần, dùng lại lâu dài                                                                               |
| History list                 | In-memory          | \_history (max 50)                                                             | Theo vòng đời process app                | Mất khi app process kết thúc hoặc clear bằng UI                                                             |
| Narration anti-repeat state  | In-memory          | \_playedPOIs, \_poiLastPlayedTime                                              | Theo phiên narration                     | Reset khi Start/Stop narration                                                                              |

## 3. POI cache chi tiết

POIService.GetPOIsAsync:

1. Nếu \_pois đang có dữ liệu thì trả ngay in-memory.
2. Nếu chưa có, đọc file offline_cache/pois.json.
3. Thử gọi API qua danh sách base URL candidates.
4. Nếu API thành công:
   - Set \_pois.
   - Save cache file (atomic temp file -> replace).
5. Nếu API fail toàn bộ:
   - Dùng cachedPois nếu có.

POIService.GetAllPOIsAsync:

1. Nếu \_pois còn hạn TTL 3 phút theo \_lastFetchUtc -> trả ngay.
2. Nếu hết TTL hoặc chưa có dữ liệu -> vào lock và thử refresh.
3. Khi refresh:

- Tạm bypass in-memory để ép thử nguồn mới.
- Nếu nhận dữ liệu và nguồn là network success -> cập nhật \_lastFetchUtc.
- Nếu nhận dữ liệu từ offline/in-memory fallback -> không cập nhật \_lastFetchUtc.
- Nếu refresh rỗng nhưng có previous data -> khôi phục previous data để tránh rỗng UI.

TTL:

- Có TTL 3 phút cho in-memory POI list.
- Timestamp \_lastFetchUtc chỉ được set khi fetch API thành công.
- Offline cache file pois.json không có TTL cứng; được dùng làm fallback khi không lấy được network.

Offline asset warm-up sau khi load POI:

- Chạy nền để prefetch ảnh POI vào AppData/image_cache.
- Nếu ảnh đã có cache local thì map trực tiếp sang path local để dùng offline.
- Đồng thời prefetch danh sách dishes theo từng restaurant và lưu vào offline_cache/dishes/{restaurantId}.json.
- Warm-up chia 2 lớp:
  - Phase A (ưu tiên cao): top N POI, ảnh primary + dishes.
  - Phase B (ưu tiên thường): toàn bộ ảnh còn lại + dishes còn lại (delay sau first render).

Thread safety / race-condition guard:

- Dùng PriorityQueue cho warm-up jobs (high -> normal).
- Dedupe job key bằng ConcurrentDictionary (\_queuedOrRunningWarmupKeys) để không enqueue trùng.
- Giới hạn đồng thời:
  - Image warm-up: semaphore giới hạn song song.
  - Dishes warm-up: semaphore riêng.
- Dedupe network call đang chạy:
  - \_imageDownloadsInFlight cho ảnh.
  - \_dishRequestsInFlight cho dishes.
- Khóa ghi file theo path bằng \_fileWriteLocks để tránh 2 luồng ghi cùng file.
- Ghi file kiểu atomic temp -> replace để tránh file hỏng nửa chừng.

## 4. Language cache chi tiết

LanguageService.GetAllLanguagesAsync chạy tương tự POI:

- Ưu tiên \_cachedLanguages.
- Fallback file offline_cache/languages.json.
- Fetch API và ghi đè cache khi thành công.

TTL:

- Không có expiration theo giờ/ngày.

## 5. Image cache chi tiết (POI)

POIService warm-up xử lý ảnh theo flow:

1. Duyệt danh sách images trong từng POI.
2. Nếu image path local đã tồn tại -> dùng ngay.
3. Nếu chưa có, tạo key cache bằng SHA256(imageUrl) + extension.
4. Nếu imageUrl là remote/relative hợp lệ:

- Build URL candidates từ BaseAddress + ApiFallbackBaseUrls.
- Tải file về image_cache.
- Nếu hợp lệ thì đổi ImageUrl sang path local để render offline.
- Nếu nhiều luồng cùng yêu cầu 1 ảnh:
  - Dedupe theo normalized image key, chỉ còn 1 download thật sự.
  - Các luồng còn lại await cùng task in-flight.
- Khi ghi cache file ảnh:
  - Khóa file-path bằng semaphore theo path.
  - Ghi temp file rồi replace.

TTL:

- Không có TTL theo thời gian.
- File tồn tại đến khi app data bị xóa hoặc bị ghi đè khi tải lại cùng key.

## 6. Dishes cache chi tiết

POIService.GetDishesByRestaurantIdAsync:

1. Đọc cache file dishes theo restaurantId trước.
2. Thử gọi API /Restaurant/{restaurantId}/dishes.
3. Nếu API thành công -> ghi đè cache file và trả dữ liệu mới.
4. Nếu API fail/offline -> trả dữ liệu cache đã đọc.
5. Nếu nhiều call đồng thời cùng restaurantId:

- Dedupe bằng \_dishRequestsInFlight.
- Chỉ còn 1 request mạng + 1 lần ghi cache thực tế.

6. Ghi cache dishes dùng khóa file-path để tránh ghi chồng.

TTL:

- Không có expiration theo thời gian.
- Làm mới khi API gọi thành công.

## 7. Audio cache chi tiết

### 5.1 Vị trí và key

AudioService dùng thư mục:

- AppData/audio_cache

Key file:

- Theo audioId: SHA256("audio:{audioId}") + .mp3
- Theo language|path: SHA256("{language}|{normalizedInputLower}") + extension

### 5.2 Chiến lược đọc nguồn

Thứ tự source khi phát/prefetch:

- Cache local.
- App package asset (với flow language+file).
- Remote HTTP URL candidates.

### 5.3 Chính sách dung lượng

Giới hạn cứng:

- MaxAudioCacheBytes = 200MB.
- MinDeviceFreeSpaceBytes = 50MB.
- MinValidAudioBytes = 256 bytes.

Trước khi ghi file mới:

1. Kiểm tra file incoming không vượt quota 200MB.
2. Kiểm tra projected cache size.
3. Nếu thiếu chỗ thì chạy CleanupLruBytes.
4. Nếu sau cleanup vẫn thiếu thì từ chối cache file.

### 5.4 LRU eviction

Danh sách file cache được sort theo:

- LastAccessTimeUtc tăng dần.
- Sau đó LastWriteTimeUtc.

Mỗi lần file được dùng, TouchCacheFile cập nhật access/write time để file đó ít bị đẩy ra.

### 5.5 TTL audio cache

- Không có TTL theo thời gian.
- File tồn tại đến khi:
  - Bị LRU evict do quota/low free space.
  - User bấm xóa cache trong Settings.
  - App data bị clear/uninstall.

## 8. Audio library manifest và startup sync

AudioLibraryService lưu manifest tại:

- AppData/audio_manifest.json

Nội dung item:

- audioId, languageCode, audioUrl, version, updatedAtUtc.

Flow startup:

- Nếu audio_ready=false:
  - Có internet: full sync tất cả audio active.
  - Offline: set cờ để MainPage hiển thị thông báo.
- Nếu audio_ready=true:
  - Có internet: chỉ sync audio mới hơn version local.
  - Offline: giữ nguyên local cache.

TTL manifest:

- Không time-based TTL.
- Version-based invalidation (chỉ tải lại khi server version cao hơn hoặc local thiếu file).

## 9. Telemetry buffer cache

### 7.1 Location log buffer

LocationLogSyncService buffer trong RAM:

- MaxBufferSize = 2000.
- Flush mỗi 10 giây.
- Flush fail thì trả batch về buffer đầu danh sách.

Độ bền:

- Không persistent ra file.
- Mất dữ liệu nếu app process bị kill trước khi flush thành công.

### 7.2 Audio log gửi trực tiếp

AudioLogSyncService không có local persistent queue riêng.

- Có retry 1 lần trong tình huống session missing.
- Các lỗi khác chủ yếu log console, không lưu queue dài hạn.

## 10. Favorites và History

Favorites:

- Lưu trong Preferences dưới dạng JSON list string.
- Persistent qua restart app.
- Không có giới hạn số lượng rõ trong code.

History:

- Chỉ lưu in-memory.
- Max 50 mục gần nhất.
- Reset khi app process đóng.

## 11. Offline behavior tổng hợp

Khi không có internet:

- POI/Language:
  - Dùng file cache nếu đã có.
- POI images:
  - Dùng file trong image_cache nếu đã prefetch thành công trước đó.
- Dishes:
  - Dùng cache file theo restaurantId nếu đã từng tải thành công.
- Audio:
  - Phát được nếu file đã cache hoặc bundled package có file phù hợp.
  - Nếu chưa có local, prefetch remote sẽ fail.
- Startup first install:
  - Nếu audio_ready=false và offline, app queue thông báo yêu cầu kết nối internet.
- Telemetry:
  - Location buffer giữ trong RAM đến khi flush thành công hoặc bị drop do vượt giới hạn.

## 12. Câu hỏi thường gặp về "cache bao lâu"

Trả lời theo code hiện tại:

- POI cache in-memory: TTL 3 phút, chỉ làm mới mốc thời gian khi fetch API thành công.
- POI cache file (offline_cache/pois.json): không có TTL cứng, dùng fallback khi offline/network fail.
- POI image cache (image_cache): không có TTL cứng; dùng lại cho tới khi app data bị xóa hoặc file bị ghi đè.
- Dishes cache (offline_cache/dishes): không có TTL cứng; làm mới khi API dishes thành công.
- Language cache: không có thời hạn theo thời gian.
- Audio cache: không có thời hạn theo thời gian; tồn tại đến khi quota/LRU hoặc user xóa.
- Favorites và deviceId: lưu lâu dài trong Preferences.
- History: tồn tại trong phiên chạy app (in-memory).
- Telemetry location buffer: tồn tại trong RAM, flush theo chu kỳ 10 giây.
