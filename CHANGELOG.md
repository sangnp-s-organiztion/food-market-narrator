# Changelog

Tài liệu này ghi lại thay đổi theo từng đợt release dựa trên lịch sử commit thực tế của dự án.

## 2026-04-03

### Added

- Thêm test và CI cho admin và saler.
- Thêm hỗ trợ session limit "Tất cả" cho trang tuyến di chuyển.

### Changed

- Cải thiện truy vấn movement paths: gom theo session trước, xử lý tọa độ null và hỗ trợ dữ liệu cũ.
- Chuẩn hóa luồng đăng nhập và quản lý người dùng: mặc định mật khẩu `123456`, hash mật khẩu, và giới hạn role cho saler.
- Loại bỏ route ảnh trùng `/public` và giữ endpoint canonical cho MAUI/saler.
- Refactor cấu trúc code và cập nhật tài liệu liên quan.

### Fixed

- Sửa lỗi hiển thị thông báo đăng nhập không nhất quán.
- Sửa lỗi tải ảnh và tên file ảnh chưa tương thích với MAUI.
- Sửa lỗi phân trang tuyến di chuyển và logic session all.

## 2026-04-02

### Added

- Thêm logging cho kết nối database, đồng bộ vị trí, và audio logging.
- Thêm heatmap, trajectory view, và session tracking cho visitor/admin.
- Thêm schema và seed data ban đầu cho database.

### Changed

- Cải thiện cơ chế xử lý session bị thiếu bằng retry logic.
- Điều chỉnh layout SettingsPage và ẩn nút logout trong ngữ cảnh visitor.

### Fixed

- Cải thiện layout, scrolling và labels trong các thành phần heatmap/trajectory.

## 2026-04-01

### Added

- Thêm admin login và role-based access control.
- Thêm admin stats API và analytics endpoints cho dashboard.
- Thêm audit log entity, middleware, controller và service.
- Thêm MongoDB connection, health check, setup guide và seed data.
- Thêm pagination cho recent activity.

### Changed

- Chuyển admin dashboard từ mock data sang real API.
- Migrate audit logging sang MongoDB.
- Cập nhật AuthController để ghi LOGIN/LOGOUT audit events.

### Fixed

- Sửa xử lý dữ liệu activity trong LogsPage.
- Loại bỏ field thừa `TargetName` khỏi AuditLog.

## 2026-03-21

### Added

- Thêm integration tests và unit tests cho POIService, HistoryService và API.
- Thêm StatusToColorConverter và cải thiện trạng thái POI.
- Thêm hỗ trợ background location permission cho visitor.
- Thêm thay đổi CI workflow và README test cho admin/saler.

### Changed

- Cải tiến logic dish/image visibility và các endpoint liên quan.
- Điều chỉnh MAUI build/restore để phù hợp Android target.
- Refactor test API để khớp DTO response mới.

### Fixed

- Sửa workflow CI, quyền workflow, và các vấn đề restore/build trên MAUI.

## 2026-03-20

### Added

- Thiết lập CI workflow đầu tiên cho MAUI và API testing.
- Thêm tài liệu và README cho các luồng test.

### Changed

- Chuẩn hóa DTO và controller naming conventions.
- Điều chỉnh LocalApiHost IP và layout FavoritePage.

### Fixed

- Sửa formatting inconsistencies trong CI workflow.

## 2026-03-17

### Added

- Thêm integration tests cho Food Market Narrator API.
- Thêm unit tests cho POIService và setup testing project.
- Thêm integration/unit tests cho POI và history services.

### Changed

- Refactor SettingsPage language loading logic.
- Chuẩn hóa project file và tooling phục vụ testing.

## Notes

- Đây là changelog theo hướng release notes, ưu tiên thay đổi có ý nghĩa với người dùng và maintainer.
- Các commit kỹ thuật nhỏ, commit merge hoặc commit sinh ảnh được gộp lại theo chủ đề để dễ đọc.
