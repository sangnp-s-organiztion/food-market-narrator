# Frontend Rules

Áp dụng cho dự án saler (React + TypeScript + Vite).

## 1) Nguyên tắc chung

- Dùng TypeScript nghiêm ngặt, tránh any nếu không thật sự cần.
- Tách rõ UI component, API layer, state handling.
- Không hard-code URL API trong component; dùng config tập trung.

## 2) Tương tác API

- Bám đúng endpoint trong docs/.claude/architecture/api-architecture.md.
- Với endpoint cần đăng nhập cookie, request phải gửi kèm credentials.
- Chuẩn hóa xử lý lỗi API (401, 403, 404, 500) tại layer chung.

## 3) UI/UX

- Ưu tiên giao diện rõ ràng cho tác vụ quản trị nội dung POI.
- Form upload ảnh/audio cần thông báo trạng thái rõ: đang tải, thành công, thất bại.
- Không đổi layout tổng thể nếu chưa có yêu cầu thiết kế mới.

## 4) Cấu trúc component

- Component trình bày (presentational) tách khỏi logic gọi API.
- Tránh component quá lớn; tách theo feature (restaurants, images, dishes, audios).
- Giữ tên file và tên component nhất quán.

## 5) Kiểm thử và chất lượng

- Chạy lint trước khi hoàn tất thay đổi.
- Nếu có logic quan trọng (filter, mapping dữ liệu), thêm test khi phù hợp.
- Không sửa nhiều khu vực không liên quan trong cùng một PR nhỏ.
