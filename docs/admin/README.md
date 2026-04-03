# Admin Docs

Tai lieu tong quan cho web admin.

## Muc tieu

Admin dashboard dung de:

- Quan ly users va role/status
- Quan ly restaurants
- Theo doi analytics (kpi, heatmap, top audios, movement)
- Xem audit logs

## Stack

- React + TypeScript + Vite
- TanStack Query
- Vitest

## Chay local

```bash
cd admin
npm install
npm run dev
```

## Build va test

```bash
cd admin
npm run build
npm run lint
npm test
```

## API chinh

- Auth: /Auth/admin/login, /Auth/admin/me, /Auth/logout
- Users: /api/users/\*
- Restaurants: /restaurant, /restaurant/{id}, /restaurant/{id}/status
- Admin stats: /api/admin/stats/\*
- Analytics: /api/analytics/\*
- Audit logs: /api/audit-logs

## Luu y

- Can gui cookie auth (`credentials: include`).
- Login fail duoc chuan hoa ve mot thong bao khong hop le.
- Movement paths ho tro sessionLimit = all (gui gia tri 0).

## Tai lieu lien quan

- ../architecture/overview.md
- ../testing/test-strategy.md
- ../../admin/README.md
