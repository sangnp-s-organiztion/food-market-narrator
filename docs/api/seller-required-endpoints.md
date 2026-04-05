# Seller Required Endpoints

Tài liệu endpoint thực tế mà web saler đang sử dụng ở code hiện tại.

## 1. Auth và session

- POST /Auth/login
- GET /Auth/me
- POST /Auth/logout

Lưu ý:

- Dùng cookie auth.
- Frontend cần gửi `credentials: include`.
- Saler app chỉ chấp nhận user có role `saler`.

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

## 7. Ghi chú quan trọng

- Route `/Users/{userId}/restaurants` không còn là dependency bắt buộc trong saler frontend hiện tại.
- Endpoint images canonical đang dùng là `/Restaurant/{restaurantId}/images` (không dùng prefix /public).
- Dữ liệu `restaurantId` được xử lý theo string ở frontend và backend.
- Nghiệp vụ audio hiện tại: trong cùng một nhà hàng và một ngôn ngữ, chỉ có 1 bản ghi audio được active tại một thời điểm.
