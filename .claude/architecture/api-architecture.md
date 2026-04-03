## API Endpoints

Luu y:

- API hien tai khong dung prefix /api.
- He thong dung cookie auth voi fallback policy: mac dinh endpoint yeu cau dang nhap, tru cac endpoint duoc danh dau public.

## Auth

- POST /Auth/login (public)
- POST /Auth/logout
- GET /Auth/me

## Language

- GET /Language (public)
- GET /Language/{languageCode} (public)

## Restaurant

- GET /Restaurant (public)
- GET /Restaurant/{id} (public)
- PATCH /Restaurant/{id}
- PATCH /Restaurant/{id}/status

## Users

- GET /Users/{userId:int}/restaurants

## Audio

- GET /Audio (public)
- GET /Restaurant/{restaurantId}/audios
- POST /Restaurant/{restaurantId}/audios
- PATCH /Audios/{audioId:int}/active
- DELETE /Audios/{audioId:int}

## Dishes

- GET /Restaurant/{restaurantId}/dishes
- POST /Restaurant/{restaurantId}/dishes
- PUT /Dishes/{dishId:int}
- DELETE /Dishes/{dishId:int}

## Images

- GET /Restaurant/{restaurantId}/images
- POST /Restaurant/{restaurantId}/images
- DELETE /Images/{imageId:int}
- PATCH /Images/{imageId:int}/primary
- PATCH /Restaurant/{restaurantId}/images/reorder

## Public Data

- GET /public/Restaurant/{restaurantId}/dishes (public)
- GET /public/Restaurant/{restaurantId}/images (public)
- GET /public/Restaurant/{restaurantId}/audios (public)
- GET /public/audios/{audioId:int}/file (public)

## Mongo

- GET /Mongo/test-connect (public)
- POST /api/user-sessions/start (public)
- POST /api/location-logs/batch (public)
- POST /api/audio-logs (public)

## Static Media URLs

- /maui-images/{fileName}
- /maui-audios/{fileName}
- /uploads/audios/{fileName}
