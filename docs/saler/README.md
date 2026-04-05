# Saler Docs

Tài liệu tổng quan cho web saler.

## Mục tiêu

Saler dashboard dùng để:

- Đăng nhập và quản lý phiên làm việc
- Quản lý thông tin nhà hàng
- Quản lý menu món ăn
- Quản lý hình ảnh nhà hàng
- Quản lý audio thuyết minh theo ngôn ngữ

## Stack

- React + TypeScript + Vite
- TanStack Query
- Vitest

## Chạy local

```bash
cd saler
npm install
npm run dev
```

## Build và test

```bash
cd saler
npm run build
npm run lint
npm test
```

## API chính

- Auth: /Auth/login, /Auth/me, /Auth/logout
- Restaurant: /Restaurant, /Restaurant/{id}, /Restaurant/{id}/status
- Dishes: /public/Restaurant/{id}/dishes, /Restaurant/{id}/dishes, /Dishes/{dishId}
- Images: /Restaurant/{id}/images, /Images/{imageId}, /Images/{imageId}/primary
- Audios: /public/Restaurant/{id}/audios, /Restaurant/{id}/audios, /Audios/{audioId}
- Languages: /Language

## Lưu ý

- Frontend gửi cookie auth qua `credentials: include`.
- App chỉ chấp nhận tài khoản role `saler` trong auth flow.
- Endpoint images đã dùng route canonical `/Restaurant/{id}/images`.
- Mỗi ngôn ngữ chỉ nên có 1 audio active cho mỗi nhà hàng; backend đang enforce quy tắc này.
- Khi upload/toggle/delete audio, UI cần refresh dữ liệu từ server để phản ánh trạng thái active mới nhất.

## Tài liệu liên quan

- ../api/seller-required-endpoints.md
- ../testing/test-strategy.md
- ../../saler/README.md
