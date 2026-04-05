# MAUI API and Sync Contracts

## 1. Mục tiêu

Tài liệu này liệt kê chính xác các endpoint mà MAUI app đang sử dụng trong code hiện tại, kèm payload sync và các điểm cần lưu ý tương thích.

## 2. Cấu hình endpoint gốc

AppSettings:

- RestaurantEndpoint = restaurant
- LanguageEndpoint = language
- UserSessionsStartEndpoint = api/user-sessions/start
- LocationLogsBatchEndpoint = api/location-logs/batch
- AudioLogsEndpoint = api/audio-logs

Base URL:

- Android emulator: http://10.0.2.2:5044/
- Android device: http://192.168.1.8:5044/
- Fallback: HTTP + HTTPS cùng host

## 3. Endpoint tiêu thụ bởi MAUI

### 3.1 POI and content

POIService:

- GET /restaurant
  - Dùng cho danh sách POI chính (bao gồm metadata, images, audios trong model trả về).
- GET /Restaurant/{restaurantId}/dishes
  - Dùng trong POIDetailPage để load dish list.

Lưu ý:

- URL dish hiện đang dùng nhánh /Restaurant/.../dishes trong code.
- Theo chính sách public endpoint cho mobile visitor, cần đảm bảo backend cho phép anonymous hoặc có endpoint public tương đương.

### 3.2 Language

LanguageService:

- GET /language
- Có hàm GetLanguageByCodeAsync nhưng đọc từ list local đã tải, không gọi endpoint riêng theo code.

### 3.3 Audio file download

AudioService (theo audioId):

- GET /public/audios/{audioId}/file

AudioService (theo language + file path):

- Tạo remote URL từ relative path resolved của audio input.
- Có thể hit URL trực tiếp nếu payload AudioUrl là absolute URL.

### 3.4 Session and logs

LocationLogSyncService:

- POST /api/user-sessions/start
- POST /api/location-logs/batch

AudioLogSyncService:

- POST /api/user-sessions/start
- POST /api/audio-logs

## 4. Payload contracts

### 4.1 User session start request

Được dùng bởi cả LocationLogSyncService và AudioLogSyncService:

- sessionId: string (guid N format)
- deviceId: string (persistent Preferences key tracking_device_id)
- deviceInfo: string (Manufacturer Model, Platform Version)

### 4.2 Location logs batch request

LocationLogBatchRequest:

- items: LocationLogItem[]

LocationLogItem:

- sessionId: string
- timestamp: DateTime UTC
- location: GeoPointPayload | null

GeoPointPayload:

- type: "Point"
- coordinates: [longitude, latitude]

### 4.3 Audio log create request

AudioLogCreateRequest:

- sessionId: string
- restaurantId: string
- audioId: int
- startTime: DateTime UTC
- endTime: DateTime UTC
- duration: int (giây)

## 5. Retry and consistency behavior

### 5.1 Location logs

- Flush định kỳ 10 giây.
- Nếu gửi batch fail: insert lại batch vào đầu buffer để retry.
- Buffer tối đa 2000 item, overflow thì drop item cũ nhất.

### 5.2 Audio logs

- Gửi trực tiếp khi playback kết thúc.
- Nếu response là 404 với message Session not found:
  - Gọi lại user-sessions/start.
  - Flush location logs ngay.
  - Retry gửi audio log 1 lần.

## 6. Data model contract (client side)

POI model được kỳ vọng có:

- restaurantId, name, description, latitude, longitude
- address, openingHours, phone, category
- images: RestaurantImageModel[]
- audios: AudioModel[]
- dishes: DishModel[] (POIDetail nạp thêm từ endpoint dishes)

AudioModel được app sử dụng các field:

- audioId
- languageCode
- languageName
- audioUrl
- version
- dateGeneration
- isActive

LanguageModel:

- languageId
- languageName
- languageCode

## 7. Serialization assumptions

- JsonSerializerOptions chủ yếu bật PropertyNameCaseInsensitive.
- Không cấu hình custom naming policy trong các service này.
- API payload cần tương thích tên property hiện tại hoặc map case-insensitive được.

## 8. Network and certificate notes

HttpClientHandler đang bật accept-any-certificate.

Tác động:

- Thuận tiện khi dev nội bộ/self-signed cert.
- Cần thay bằng chính sách xác thực chứng chỉ phù hợp khi production.

## 9. Checklist khi thay đổi backend API

Khi backend đổi endpoint hoặc payload, cần rà lại tối thiểu:

1. AppSettings endpoint constants.
2. POIService URL cho restaurant và dishes.
3. AudioService URL builder cho public audio file.
4. LocationLogSyncService và AudioLogSyncService payload mapping.
5. Logic parse lỗi Session not found trong AudioLogSyncService.
6. Tài liệu docs/architecture/maui-caching-and-offline.md nếu thay đổi ảnh hưởng cache/invalidation.
