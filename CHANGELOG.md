# Changelog

Tài liệu ghi lại thay đổi quan trọng của dự án theo từng đợt phát hành.

Format áp dụng:

- Added: tính năng mới.
- Changed: thay đổi hành vi, refactor có tác động.
- Fixed: sửa lỗi.
- Removed: loại bỏ tính năng/điểm cũ.

## [Unreleased]

### Added - Unreleased

- Mở rộng CI để chạy test cho đủ 4 mảng: API, MAUI, admin, saler.
- Bổ sung tài liệu hướng dẫn chạy test tổng hợp tại test-guide.md.

### Changed - Unreleased

- Chuẩn hóa lại bộ test theo trạng thái code mới của auth, analytics và image endpoints.
- Cải thiện độ ổn định test MAUI bằng cách loại bỏ phụ thuộc vào giờ chạy thực tế.

## [v1.2.0] - 2026-04-03

### Added - v1.2.0

- Hỗ trợ session limit "Tất cả" cho phân tích tuyến di chuyển.
- Mở rộng kiểm thử cho admin/saler API clients và bổ sung test API, MAUI.

### Changed - v1.2.0

- Nâng cấp movement-paths aggregation: gom theo session trước khi limit, xử lý dữ liệu tọa độ thiếu/null.
- Cập nhật chuẩn endpoint ảnh, bỏ route trùng và đồng bộ tài liệu liên quan.
- Chuẩn hóa luồng quản lý user: role gating, default password, kiểm soát trạng thái tài khoản.

### Fixed - v1.2.0

- Sửa lỗi phân trang tuyến di chuyển bị snap về trang chứa session đã chọn.
- Sửa lỗi thông báo đăng nhập admin không nhất quán.
- Sửa lỗi tải audio và tương thích tên file ảnh với MAUI.

## [v1.1.0] - 2026-04-02

### Added - v1.1.0

- Thêm logging cho location sync, audio logs, database connection.
- Thêm heatmap, trajectory view và các thành phần phân tích hành vi nghe audio.
- Bổ sung schema và seed data cho dữ liệu tracking/session.

### Changed - v1.1.0

- Cải thiện xử lý session thiếu dữ liệu bằng retry logic.
- Tinh chỉnh giao diện Settings/Heatmap/Trajectory cho khả năng sử dụng tốt hơn.

### Fixed - v1.1.0

- Sửa layout và scrolling issues trong các màn analytics.

## [v1.0.0] - 2026-04-01

### Added - v1.0.0

- Bổ sung admin authentication và role-based access.
- Thêm admin stats API và analytics endpoints cho dashboard.
- Thêm audit log đầy đủ: entity, middleware, service, controller.
- Bổ sung MongoDB health check, setup docs và seed data.

### Changed - v1.0.0

- Chuyển admin dashboard từ mock sang real API.
- Migrate audit logging từ SQL sang MongoDB.

### Fixed - v1.0.0

- Sửa xử lý dữ liệu activity ở admin logs.
- Loại bỏ trường thừa trong audit schema/migration.

## [v0.2.0] - 2026-03-21

### Added - v0.2.0

- Thêm integration tests và unit tests cho API/MAUI.
- Bổ sung hỗ trợ background location permission ở mobile.
- Mở rộng CI cho các luồng build/test chính.

### Changed - v0.2.0

- Refactor endpoint và DTO để đồng nhất contract.
- Cập nhật MAUI build/restore flow theo Android target thực tế.

### Fixed - v0.2.0

- Sửa lỗi CI workflow (restore/build permissions và command).

## [v0.1.0] - 2026-03-20

### Added - v0.1.0

- Thiết lập CI workflow ban đầu cho dự án.
- Bổ sung tài liệu test và hướng dẫn local run.

### Changed - v0.1.0

- Chuẩn hóa naming conventions ở một số DTO/controller.

## [v0.0.1] - 2026-03-17

### Added - v0.0.1

- Bản nền cho test automation: API integration tests, MAUI unit tests.

### Changed - v0.0.1

- Điều chỉnh SettingsPage language flow và cấu trúc test project ban đầu.

## Maintenance Guide

- Mỗi PR có thay đổi hành vi người dùng hoặc API contract cần cập nhật changelog.
- Không ghi commit kỹ thuật nhỏ lẻ không ảnh hưởng hành vi.
- Ghi vào `[Unreleased]` trước, chuyển vào version khi cắt release.
