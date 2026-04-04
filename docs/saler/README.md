# Saler Docs

Tai lieu tong quan cho web saler.

## Muc tieu

Saler dashboard dung de:

- Dang nhap va quan ly phien lam viec
- Quan ly thong tin nha hang
- Quan ly menu mon an
- Quan ly hinh anh nha hang
- Quan ly audio thuyet minh theo ngon ngu

## Stack

- React + TypeScript + Vite
- TanStack Query
- Vitest

## Chay local

```bash
cd saler
npm install
npm run dev
```

## Build va test

```bash
cd saler
npm run build
npm run lint
npm test
```

## API chinh

- Auth: /Auth/login, /Auth/me, /Auth/logout
- Restaurant: /Restaurant, /Restaurant/{id}, /Restaurant/{id}/status
- Dishes: /public/Restaurant/{id}/dishes, /Restaurant/{id}/dishes, /Dishes/{dishId}
- Images: /Restaurant/{id}/images, /Images/{imageId}, /Images/{imageId}/primary
- Audios: /public/Restaurant/{id}/audios, /Restaurant/{id}/audios, /Audios/{audioId}
- Languages: /Language

## Luu y

- Frontend gui cookie auth qua credentials include.
- App chi chap nhan tai khoan role saler trong auth flow.
- Endpoint images da dung route canonical /Restaurant/{id}/images.

## Tai lieu lien quan

- ../api/seller-required-endpoints.md
- ../testing/test-strategy.md
- ../../saler/README.md
