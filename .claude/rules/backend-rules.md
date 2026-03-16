# Backend Rules

## 1) Kiến trúc bắt buộc

- Giữ đúng luồng: Controller -> Service -> Repository -> DbContext.
- Controller mỏng: chỉ nhận request, validate cơ bản, trả response.
- Business logic đặt ở Service.
- Repository chỉ xử lý truy cập dữ liệu, không chứa business rule.

## 2) Routing và endpoint

- Tuân thủ naming route theo code hiện tại (ví dụ: /Restaurant, /Language, /Auth).
- Endpoint public phải được khai báo trong danh sách PublicEndpoints.
- Khi thêm endpoint mới:
  - Cập nhật controller.
  - Cập nhật PublicEndpoints nếu endpoint cần anonymous.
  - Cập nhật docs/.claude/architecture/api-architecture.md.

## 3) Auth và phân quyền

- Mặc định endpoint yêu cầu xác thực theo fallback policy.
- Dùng cookie auth cho saler/admin flow.
- Không tự bỏ [Authorize] nếu chưa xác nhận rõ nghiệp vụ.

## 4) DTO và validate

- Request/response qua DTO, không trả trực tiếp entity DB.
- Validate ModelState ở controller cho request có body.
- Trả mã lỗi nhất quán:
  - 400: dữ liệu không hợp lệ.
  - 401/403: chưa đăng nhập hoặc không đủ quyền.
  - 404: không tìm thấy tài nguyên.

## 5) Media và file upload

- Ảnh và audio upload phải đi qua service hiện có.
- Giữ tương thích với static paths:
  - /maui-images
  - /maui-audios
  - /uploads/audios

## 6) Chất lượng code

- Không viết logic trùng; tái sử dụng service/repository hiện tại.
- Tránh thay đổi API contract đang dùng bởi mobile/frontend nếu không có migration plan.
- Ưu tiên thay đổi nhỏ, kiểm soát rủi ro và giữ backward compatibility.
