# Audio cache lưu ở đâu

Tài liệu này mô tả chính xác vị trí lưu audio đã tải và cơ chế cache/offline theo code hiện tại của MAUI app.

## Tóm tắt

- Audio được lưu trong thư mục con audio_cache bên trong FileSystem.AppDataDirectory.
- Tên file cache là hash SHA256 (từ language + đường dẫn audio) kèm extension, không giữ tên gốc từ API.
- Luồng lấy audio theo thứ tự: cache cục bộ -> app package -> tải từ network.

## Bằng chứng trong code

- Tạo root cache: Services/AudioService.cs, hàm GetAudioCacheRootPath().
- Tạo file cache theo hash: Services/AudioService.cs, hàm GetAudioCachePath(...).
- Luồng đọc cache/package/network: Services/AudioService.cs, hàm ResolvePlayableStreamAsync(...).

## Đường dẫn thực tế theo nền tảng

1. Android (emulator/device)

- /data/user/0/com.companyname.foodmarketnarrator/files/audio_cache

2. Windows

- Nằm trong vùng LocalAppData của ứng dụng MAUI, dưới thư mục files/audio_cache.

3. iOS

- Nằm trong sandbox của app (vùng dữ liệu ứng dụng), có thư mục audio_cache.

## Cơ chế cache hiện tại

1. Nếu file cache hợp lệ (>= 256 bytes), phát trực tiếp từ cache.
2. Nếu chưa có cache, thử mở file từ app package bằng FileSystem.OpenAppPackageFileAsync(...), sau đó lưu vào cache.
3. Nếu không có trong package, thử tải từ các URL ứng viên (BaseAddress và ApiFallbackBaseUrls), lưu cache nếu đủ điều kiện.
4. Nếu không thể ghi cache (thiếu dung lượng), app vẫn có thể phát online-only qua MemoryStream nếu tải được.

## Chính sách dung lượng

- Giới hạn tổng cache audio: 200 MB.
- Luôn chừa tối thiểu 50 MB dung lượng trống thiết bị.
- Có cơ chế dọn LRU (xóa file ít truy cập trước) khi gần đầy.

## Lưu ý vận hành

- Phát offline chỉ hoạt động với file đã có sẵn trong package hoặc đã từng tải thành công trước đó.
- Nếu cache rỗng và package không có file tương ứng, app cần mạng để phát.

## Cách kiểm tra nhanh

1. Mở app, phát thử một bài thuyết minh.
2. Kiểm tra thư mục audio_cache trên thiết bị/emulator.
3. Xác nhận có file mới được tạo và kích thước > 0.
4. Tắt mạng, phát lại cùng nội dung để xác nhận offline playback.
