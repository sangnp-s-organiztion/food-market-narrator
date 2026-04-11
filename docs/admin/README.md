# Admin Docs

Tài liệu tổng quan cho web admin.

## Mục tiêu

Admin dashboard dùng để:

- Quản lý users và role/status
- Quản lý restaurants
- Quản lý hành trình (tour)
- Theo dõi analytics (kpi, heatmap, top audios, movement)
- Xem audit logs
- Quản lý chi phí dịch/TTS
- Quản lý QR mở app
- Quản lý tài khoản admin

## Stack

- React + TypeScript + Vite
- TanStack Query
- Vitest

## Chạy local

```bash
cd admin
npm install
npm run dev
```

## Build và test

```bash
cd admin
npm run build
npm run lint
npm test
```

## API chính

- Auth: /Auth/admin/login, /Auth/admin/me, /Auth/admin/logout, /Auth/admin/qr-code
- Users: /api/users/\*
- Restaurants: /restaurant, /restaurant/{id}, /restaurant/{id}/status
- Tours: /Tour, /Tour/{id}, /Tour/{id}/restaurants, /Tour/{id}/stops/order
- Admin stats: /api/admin/stats/\*
- Analytics: /api/analytics/\*
- Audit logs: /api/audit-logs
- Translation billing: /api/admin/translation-billing/\*

## Lưu ý

- Cần gửi cookie auth (`credentials: include`).
- Login fail được chuẩn hóa về một thông báo không hợp lệ.
- Movement paths hỗ trợ `sessionLimit = all` (gửi giá trị `0`).

## Tài liệu liên quan

- ../architecture/overview.md
- ../testing/test-strategy.md
- ../../admin/README.md
