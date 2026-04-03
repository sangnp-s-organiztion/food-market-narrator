## Backend Architecture

Backend sử dụng ASP.NET Web API + SQL Server (EF Core) + Cookie Authentication.

## 1) Layer hiện tại

- Controllers
- Services
- Repositories
- Models/DTOs
- Data (AppDbContext)

## 2) Luồng xử lý chuẩn

1. Client gọi HTTP endpoint tại Controller.
2. Controller validate request cơ bản và chuyển cho Service.
3. Service xử lý business logic.
4. Service gọi Repository để đọc/ghi dữ liệu.
5. Repository tương tác AppDbContext.
6. Service trả DTO, Controller trả response JSON.

## 3) Auth model

- Dùng Cookie Authentication scheme.
- Fallback policy yêu cầu authenticated user cho toàn bộ endpoint.
- Public endpoint được whitelist qua PublicEndpoints + PublicEndpointConvention.

## 4) Static media serving

Backend đang publish các đường dẫn static chính:

- /maui-images (ảnh từ MAUI Resources/Images)
- /maui-audios (audio từ MAUI Resources/Narration/audio)
- /uploads/audios (audio upload runtime)
- /public/audios/{audioId:int}/file (public controller endpoint cho mobile download/playback)

## 5) Nguyên tắc mở rộng

- Endpoint mới phải xác định rõ public hay private.
- Nếu thay đổi contract response, cập nhật đồng thời mobile/frontend docs.
- Ưu tiên tương thích ngược để tránh ảnh hưởng app mobile đang chạy.
