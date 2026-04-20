# API Architecture

Tài liệu endpoint chi tiết theo policy auth đang được quản lý tại:

- `.claude/architecture/api-architecture.md`

File này đóng vai trò index trong `docs/` để gom cấu trúc theo nhóm tài liệu.

## Tóm tắt endpoint công khai quan trọng

- `GET /Restaurant`
- `GET /Restaurant/{id}`
- `GET /Tour`
- `GET /Tour/{id}`
- `GET /Language`
- `GET /Language/{languageCode}`
- `GET /Restaurant/{restaurantId}/images`
- `GET /public/Restaurant/{restaurantId}/dishes`
- `GET /public/Restaurant/{restaurantId}/audios`
- `GET /public/translations?languageCode={code}&entityType={restaurant|dish|tour}&entityIds={id1,id2,...}`
- `POST /Auth/forgot-password/send-otp`
- `POST /Auth/forgot-password/verify-otp`
- `POST /Auth/forgot-password/reset`
- `GET /Mongo/test-connect`
- `POST /api/user-sessions/start`
- `GET /api/user-sessions/{sessionId}/qr-access`
- `POST /api/location-logs/batch`
- `POST /api/audio-logs`

Ghi chú:

- Luồng QR hiện tại trên MAUI chỉ mở app qua `foodmarketnarrator://open` và không gọi endpoint `GET /api/user-sessions/{sessionId}/qr-access`.
- Endpoint `qr-access` vẫn được liệt kê tại đây vì thuộc API backend hiện có.
