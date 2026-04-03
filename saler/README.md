# Saler Web App

Dashboard cho seller/chu quan trong he thong Food Market Narrator.

## Stack

- React 18 + TypeScript
- Vite 5
- TanStack Query
- Vitest + Testing Library

## Run local

```bash
cd saler
npm install
npm run dev
```

Dev server mac dinh: <http://localhost:8080>

## Environment

Bien moi truong chinh:

- `VITE_API_BASE_URL` (mac dinh `http://localhost:5044`)

Vi du `.env.local`:

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

## API su dung chinh

- Auth: `/Auth/login`, `/Auth/me`, `/Auth/logout`
- Restaurant: `/Restaurant`, `/Restaurant/{id}`, `/Restaurant/{id}/status`
- Dishes: `/public/Restaurant/{id}/dishes`, `/Restaurant/{id}/dishes`, `/Dishes/{dishId}`
- Images: `/Restaurant/{id}/images`, `/Images/{imageId}`, `/Images/{imageId}/primary`
- Audios: `/public/Restaurant/{id}/audios`, `/Restaurant/{id}/audios`, `/Audios/{audioId}`
- Languages: `/Language`

## Testing

```bash
cd saler
npm test
```

## Ghi chu

- Frontend gui cookie auth qua `credentials: include`.
- App chi chap nhan user role `saler` trong luong dang nhap.
