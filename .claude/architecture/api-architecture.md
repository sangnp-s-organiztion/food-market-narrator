## API Endpoints

Luu y:

- API hien tai khong dung prefix /api.
- He thong dung cookie auth voi fallback policy: mac dinh endpoint yeu cau dang nhap, tru cac endpoint duoc danh dau public.
- Auth da tach 2 cookie rieng cho saler va admin de co the dang nhap dong thoi trong cung browser.

## Auth

- POST /Auth/login (public)
- POST /Auth/admin/login (public)
- POST /Auth/forgot-password/send-otp (public)
- POST /Auth/forgot-password/verify-otp (public)
- POST /Auth/forgot-password/reset (public)
- POST /Auth/logout
- POST /Auth/admin/logout
- GET /Auth/me
- GET /Auth/admin/me
- POST /Auth/admin/qr-code (admin upload PNG, overwrite fixed file)

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
- GET /api/users (admin/saler)
- GET /api/users/visitors (admin only)

## Admin Translation Billing

- GET /api/admin/translation-billing/monthly
- GET /api/admin/translation-billing/usage
- GET /api/admin/translation-billing/audio-usage

## Seller Translation Billing

- GET /api/translation-billing/my-usage
- GET /api/translation-billing/my-audio-usage

## Audio

- GET /Audio (public)
- GET /Restaurant/{restaurantId}/audios
- POST /Restaurant/{restaurantId}/audios
- POST /Restaurant/{restaurantId}/translate
- POST /Restaurant/{restaurantId}/audios/from-text
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

## Tour

- GET /Tour
- GET /Tour/{id:int}
- POST /Tour
- PATCH /Tour/{id:int}
- POST /Tour/{id:int}/restaurants
- PUT /Tour/{id:int}/stops/order
- POST /Tour/upload-image
- POST /Tour/{id:int}/upload-image

## Public Data

- GET /public/Restaurant/{restaurantId}/dishes (public)
- GET /Restaurant/{restaurantId}/images (public)
- GET /public/Restaurant/{restaurantId}/audios (public)
- GET /public/translations?languageCode={code}&entityType={restaurant|dish}&entityIds={id1,id2,...} (public)
- GET /public/audios/{audioId:int}/file (public)

## Mongo

- GET /Mongo/test-connect (public)
- POST /api/user-sessions/start (public)
- GET /api/user-sessions/{sessionId}/qr-access (public)
- POST /api/location-logs/batch (public)
- POST /api/audio-logs (public)

Ghi chu them:

- GET /api/admin/stats/users/count hien tra tong so nguoi dung gom: admin + saler (SQL Users) + visitor (Mongo UserSessions).

## Static Media URLs

- /maui-images/{fileName}
- /maui-audios/{fileName}
- /uploads/audios/{fileName}
