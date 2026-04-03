# Seller Required Endpoints

Tai lieu endpoint thuc te ma web saler dang su dung o code hien tai.

## 1. Auth va session

- POST /Auth/login
- GET /Auth/me
- POST /Auth/logout

Luu y:

- Dung cookie auth.
- Frontend can gui `credentials: include`.
- Saler app chi chap nhan user co role `saler`.

## 2. Restaurant

- GET /Restaurant
- PATCH /Restaurant/{restaurantId}
- PATCH /Restaurant/{restaurantId}/status

## 3. Dishes

- GET /public/Restaurant/{restaurantId}/dishes
- POST /Restaurant/{restaurantId}/dishes
- PUT /Dishes/{dishId}
- DELETE /Dishes/{dishId}

## 4. Images

- GET /Restaurant/{restaurantId}/images
- POST /Restaurant/{restaurantId}/images
- PUT /Images/{imageId}
- DELETE /Images/{imageId}
- PATCH /Images/{imageId}/primary
- PATCH /Restaurant/{restaurantId}/images/reorder

## 5. Audios

- GET /public/Restaurant/{restaurantId}/audios
- GET /Restaurant/{restaurantId}/audios
- POST /Restaurant/{restaurantId}/audios
- PATCH /Audios/{audioId}/active
- DELETE /Audios/{audioId}

## 6. Languages

- GET /Language
- GET /Language/{languageCode}

## 7. Notes quan trong

- Route `/Users/{userId}/restaurants` khong con la dependency bat buoc trong saler frontend hien tai.
- Endpoint images canonical dang dung la `/Restaurant/{restaurantId}/images` (khong dung prefix /public).
- Du lieu `restaurantId` duoc xu ly theo string o frontend va backend.
