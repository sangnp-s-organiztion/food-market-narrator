# API Architecture

Tài liệu endpoint chi tiết theo policy auth đang được quản lý tại:

- `.claude/architecture/api-architecture.md`

File này đóng vai trò index trong `docs/` để gom cấu trúc theo nhóm tài liệu.

## Tóm tắt endpoint công khai quan trọng

- `GET /Restaurant`
- `GET /Restaurant/{id}`
- `GET /Language`
- `GET /Language/{languageCode}`
- `GET /Restaurant/{restaurantId}/images`
- `GET /public/Restaurant/{restaurantId}/dishes`
- `GET /public/Restaurant/{restaurantId}/audios`
- `GET /Mongo/test-connect`
- `POST /api/user-sessions/start`
- `GET /api/user-sessions/{sessionId}/qr-access`
- `POST /api/location-logs/batch`
- `POST /api/audio-logs`
