# Admin Web App

Dashboard quản trị cho hệ thống Food Market Narrator.

## Stack

- React 18 + TypeScript
- Vite 5
- TanStack Query
- Vitest + Testing Library

## Run local

```bash
cd admin
npm install
npm run dev
```

Dev server mặc định: `http://localhost:8080`

## Environment

Biến môi trường chính:

- `VITE_API_BASE_URL` (mặc định `http://localhost:5044`)

Ví dụ `.env.local`:

```env
VITE_API_BASE_URL=http://localhost:5044
```

## Scripts

```bash
npm run dev
npm run build
npm run lint
npm run test
npm run test:watch
```

## API sử dụng chính

- Auth admin: `/Auth/admin/login`, `/Auth/admin/me`, `/Auth/logout`
- Users: `/api/users`, `/api/users/{id}`, `/api/users/{id}/role`, `/api/users/{id}/status`
- Restaurants: `/restaurant`, `/restaurant/{id}`, `/restaurant/{id}/status`
- Analytics: `/api/analytics/*`
- Audit logs: `/api/audit-logs`

## Testing

```bash
cd admin
npm test
```

## Ghi chú

- Request cần gửi cookie auth (`credentials: include`).
- Nếu API chạy khác host/port, cập nhật `VITE_API_BASE_URL`.
