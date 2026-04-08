# Release Notes

Tài liệu tổng hợp thay đổi theo đợt release.

Mẫu ghi:

## 2026-04-06

- Changed:
  - MAUI: đổi ngôn ngữ trong Settings không còn reset AppShell, giữ nguyên trang hiện tại.
  - MAUI: tối ưu MainPage để nút thuyết minh tự động xuất hiện nhanh hơn khi quay lại từ trang khác.
- Added:
  - MAUI: thêm persistence file cho location-log buffer (offline_cache/location_logs_buffer.json), tự nạp lại khi app start và tiếp tục sync khi có mạng.
- Notes:
  - Cơ chế flush location logs vẫn theo batch 10 giây, giới hạn buffer 2000 bản ghi.

## YYYY-MM-DD

- Added:
- Changed:
- Fixed:
- Notes:
