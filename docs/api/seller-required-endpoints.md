# API cho seller (cập nhật implementation)

Tài liệu này tổng hợp các API cần có để chạy đầy đủ các màn trong module seller, đối chiếu với backend hiện tại.
Cập nhật mới nhất: đã implement đầy đủ nhóm API P0 và P1 bên dưới.

## 1) API hiện có (FoodMarketNarrator.Api)

- `POST /Auth/login`
- `POST /Auth/logout`
- `GET /Auth/me`
- `GET /Users/{userId}/restaurants`
- `GET /Restaurant`
- `GET /Restaurant/{id}`
- `PATCH /Restaurant/{id}`
- `PATCH /Restaurant/{id}/status`
- `GET /Restaurant/{restaurantId}/dishes`
- `POST /Restaurant/{restaurantId}/dishes`
- `PUT /Dishes/{dishId}`
- `DELETE /Dishes/{dishId}`
- `GET /Restaurant/{restaurantId}/images`
- `POST /Restaurant/{restaurantId}/images`
- `DELETE /Images/{imageId}`
- `PATCH /Images/{imageId}/primary`
- `PATCH /Restaurant/{restaurantId}/images/reorder`
- `GET /Audio`
- `GET /Restaurant/{restaurantId}/audios`
- `POST /Restaurant/{restaurantId}/audios`
- `PATCH /Audios/{audioId}/active`
- `DELETE /Audios/{audioId}`
- `GET /Language`
- `GET /Language/{languageCode}`

Nhận xét:

- Nhóm API phục vụ seller đã có đủ theo danh sách yêu cầu P0/P1.
- Auth hiện tại đang dùng cookie session (không trả JWT access token).

## 2) Feature seller và API đã implement

### 2.1 Đăng nhập + xác thực

UI liên quan: LoginPage, DashboardSidebar (logout)

Đã implement:

- `POST /Auth/login`
- `POST /Auth/logout` (nếu dùng cookie/session)
- `GET /Auth/me`

Request gợi ý:

- `POST /Auth/login`
  - body: `{ "username": "...", "password": "..." }`

Response hiện tại:

- `POST /Auth/login` trả thông tin user và set cookie session (`fmn_saler_auth`).
- `GET /Auth/me` đọc username từ claim trong cookie (không truyền username qua query/body).

### 2.2 Chọn nhà hàng theo user

UI liên quan: SelectRestaurantPage, DashboardLayout

Đã implement:

- `GET /Users/{userId}/restaurants`
  - Trả danh sách nhà hàng mà user được quản lý.

Lưu ý DB:

- Nếu không có bảng `UserRestaurant`, có thể map trực tiếp bằng `Restaurant.user_id`.

### 2.3 Trang nhà hàng (RestaurantPage)

UI liên quan: cập nhật profile nhà hàng, giờ mở cửa, trạng thái is_active

Đã implement:

- `PATCH /Restaurant/{id}`
- `PATCH /Restaurant/{id}/status`

Request gợi ý:

- `PATCH /Restaurant/{id}` body:
  - `name, description, phone, address, latitude, longitude, open_time, close_time`
- `PATCH /Restaurant/{id}/status` body:
  - `{ "is_active": true }`

### 2.4 Trang Thực đơn (DishesPage)

UI liên quan: danh sách món, Thêm/sửa/xóa

Đã implement:

- `GET /Restaurant/{restaurantId}/dishes`
- `POST /Restaurant/{restaurantId}/dishes`
- `PUT /Dishes/{dishId}`
- `DELETE /Dishes/{dishId}`

Gợi ý tối ưu:

- Hỗ trợ phân trang cho list: `?page=1&pageSize=20`

### 2.5 Trang Hình ảnh (ImagesPage)

UI liên quan: Thêm ảnh, xóa ảnh, set ảnh chính, đổi thứ tự

Đã implement:

- `GET /Restaurant/{restaurantId}/images`
- `POST /Restaurant/{restaurantId}/images` (multipart/form-data)
- `DELETE /Images/{imageId}`
- `PATCH /Images/{imageId}/primary`
- `PATCH /Restaurant/{restaurantId}/images/reorder`

Request gợi ý:

- `PATCH /Images/{imageId}/primary` body: `{ "is_primary": true }`
- `PATCH /Restaurant/{restaurantId}/images/reorder` body:
  - `{ "items": [{ "image_id": 1, "sort_order": 1 }, ...] }`

### 2.6 Trang Audio (AudioPage)

UI liên quan: list theo nhà hàng, Thêm audio theo ngôn ngữ, bật/tắt, xóa

Đã implement:

- `GET /Restaurant/{restaurantId}/audios`
- `POST /Restaurant/{restaurantId}/audios` (multipart/form-data)
- `PATCH /Audios/{audioId}/active`
- `DELETE /Audios/{audioId}`

Request gợi ý:

- `POST /Restaurant/{restaurantId}/audios`
  - fields: `language_id`, `file`
- `PATCH /Audios/{audioId}/active`
  - body: `{ "is_active": true }`

## 3) Ưu tiên implement để chạy được seller nhanh

Mức tối thiểu để thay mock bằng API thật:

Trạng thái: đã xong toàn bộ P0 và P1.

P0 (bắt buộc):

- `POST /Auth/login`
- `GET /Auth/me`
- `GET /Users/{userId}/restaurants`
- `PATCH /Restaurant/{id}`
- `PATCH /Restaurant/{id}/status`
- `GET /Restaurant/{restaurantId}/dishes`
- `POST /Restaurant/{restaurantId}/dishes`
- `PUT /Dishes/{dishId}`
- `DELETE /Dishes/{dishId}`

P1 (nên có sớm):

- `GET /Restaurant/{restaurantId}/images`
- `POST /Restaurant/{restaurantId}/images`
- `DELETE /Images/{imageId}`
- `PATCH /Images/{imageId}/primary`
- `PATCH /Restaurant/{restaurantId}/images/reorder`
- `GET /Restaurant/{restaurantId}/audios`
- `POST /Restaurant/{restaurantId}/audios`
- `PATCH /Audios/{audioId}/active`
- `DELETE /Audios/{audioId}`

## 4) Lưu ý quan trọng về model dữ liệu

Cần thống nhất kiểu `restaurant_id` giữa frontend và backend:

- Frontend seller hiện đang dùng `number`.
- DB/backend hiện có nhiều chỗ dạng `string/varchar`.

Khuyến nghị:

- Chọn một chuẩn duy nhất (nên theo DB là `string`) và đồng bộ TypeScript types + DTO API.

Nếu không đồng bộ sớm, bạn sẽ gặp lỗi parse ID, route sai kiểu, và update/xóa không trúng bản ghi.

## 5) API contract chi tiết (request/response)

Lưu ý chung:

- JSON key dạng camelCase.
- Tất cả API bên dưới (trừ `POST /Auth/login`) cần cookie auth hợp lệ.
- Đối với frontend web, cần gửi kèm cookie (`credentials: include`).

### 5.1 Auth

#### `POST /Auth/login`

- Auth: không cần.
- Body (JSON):

```json
{
  "username": "seller01",
  "password": "secret"
}
```

- Response 200:

```json
{
  "userId": 12,
  "username": "seller01",
  "role": "Saler",
  "isActive": true
}
```

- Response 400: thiếu username/password.
- Response 401: sai tài khoản/mật khẩu hoặc tài khoản bị khóa.
- Ghi chú: response set cookie `fmn_saler_auth`.

#### `POST /Auth/logout`

- Auth: cần.
- Body: không có.
- Response 200:

```json
{
  "message": "Logged out successfully."
}
```

#### `GET /Auth/me`

- Auth: cần.
- Body: không có.
- Response 200:

```json
{
  "userId": 12,
  "username": "seller01",
  "role": "Saler"
}
```

- Response 401: chưa login hoặc cookie hết hạn.

### 5.2 User restaurants

#### `GET /Users/{userId}/restaurants`

- Auth: cần.
- Path param:
  - `userId` (int)
- Body: không có.
- Response 200 (mảng nhà hàng):

```json
[
  {
    "restaurantId": "res_001",
    "name": "La Trattoria Bella",
    "description": "...",
    "latitude": 21.0285,
    "longitude": 105.8542,
    "address": "123 Pho Hue",
    "phone": "0123456789",
    "isActive": true,
    "userId": 12,
    "openTime": "08:00:00",
    "closeTime": "22:00:00",
    "createdAt": "2026-03-15T09:30:00Z",
    "images": [],
    "audios": []
  }
]
```

### 5.3 Restaurant

#### `PATCH /Restaurant/{id}`

- Auth: cần.
- Path param:
  - `id` (string, restaurant_id)
- Body (JSON):

```json
{
  "name": "La Trattoria Bella",
  "description": "nhà hàng Y",
  "phone": "0123456789",
  "address": "123 Pho Hue",
  "latitude": 21.0285,
  "longitude": 105.8542,
  "openTime": "08:00:00",
  "closeTime": "22:00:00"
}
```

- Response 200: trả lại object restaurant đã cập nhật (cùng schema với `GET /Users/{userId}/restaurants`).
- Response 400: body không hợp lệ.
- Response 404: không tìm thấy restaurant.

#### `PATCH /Restaurant/{id}/status`

- Auth: cần.
- Path param:
  - `id` (string, restaurant_id)
- Body (JSON):

```json
{
  "isActive": true
}
```

- Response 200:

```json
{
  "message": "Restaurant status updated."
}
```

- Response 404: không tìm thấy restaurant.

### 5.4 Dishes

#### `GET /Restaurant/{restaurantId}/dishes?page=1&pageSize=20`

- Auth: cần.
- Path param:
  - `restaurantId` (string)
- Query params:
  - `page` (int, mặc định 1)
  - `pageSize` (int, mặc định 20)
- Body: không có.
- Response 200:

```json
[
  {
    "dishId": 10,
    "name": "Margherita Pizza",
    "price": 14.99,
    "description": "Pizza có điền",
    "restaurantId": "res_001",
    "imageId": 100,
    "createdAt": "2026-03-15T10:00:00Z"
  }
]
```

#### `POST /Restaurant/{restaurantId}/dishes`

- Auth: cần.
- Path param:
  - `restaurantId` (string)
- Body (JSON):

```json
{
  "name": "Tiramisu",
  "price": 9.99,
  "description": "Món tráng miệng Y",
  "imageId": null
}
```

- Response 200: trả về dish vừa tạo (schema như GET dishes item).
- Response 400: body không hợp lệ.

#### `PUT /Dishes/{dishId}`

- Auth: cần.
- Path param:
  - `dishId` (int)
- Body (JSON):

```json
{
  "name": "Tiramisu size M",
  "price": 10.99,
  "description": "cập nhật mô tả",
  "imageId": 101
}
```

- Response 200: trả về dish đã cập nhật.
- Response 400: body không hợp lệ.
- Response 404: không tìm thấy dish.

#### `DELETE /Dishes/{dishId}`

- Auth: cần.
- Path param:
  - `dishId` (int)
- Body: không có.
- Response 200:

```json
{
  "message": "Dish deleted successfully."
}
```

- Response 404: không tìm thấy dish.

### 5.5 Images

#### `GET /Restaurant/{restaurantId}/images`

- Auth: cần.
- Path param:
  - `restaurantId` (string)
- Body: không có.
- Response 200:

```json
[
  {
    "imageId": 100,
    "imageUrl": "/uploads/images/abc.jpg",
    "isPrimary": true,
    "sortOrder": 1
  }
]
```

#### `POST /Restaurant/{restaurantId}/images` (multipart/form-data)

- Auth: cần.
- Path param:
  - `restaurantId` (string)
- Form-data fields:
  - `file` (required, binary)
  - `is_primary` (optional, bool, default false)
  - `sort_order` (optional, int, default 0)
- Response 200:

```json
{
  "imageId": 101,
  "imageUrl": "/uploads/images/def.jpg",
  "isPrimary": false,
  "sortOrder": 2
}
```

- Response 400: thiếu file.

#### `DELETE /Images/{imageId}`

- Auth: cần.
- Path param:
  - `imageId` (int)
- Body: không có.
- Response 200:

```json
{
  "message": "Image deleted successfully."
}
```

- Response 404: không tìm thấy image.

#### `PATCH /Images/{imageId}/primary`

- Auth: cần.
- Path param:
  - `imageId` (int)
- Body (JSON):

```json
{
  "isPrimary": true
}
```

- Response 200:

```json
{
  "message": "Image primary status updated."
}
```

- Response 404: không tìm thấy image.

#### `PATCH /Restaurant/{restaurantId}/images/reorder`

- Auth: cần.
- Path param:
  - `restaurantId` (string)
- Body (JSON):

```json
{
  "items": [
    {
      "imageId": 100,
      "sortOrder": 1
    },
    {
      "imageId": 101,
      "sortOrder": 2
    }
  ]
}
```

- Response 200:

```json
{
  "message": "Images reordered successfully."
}
```

- Response 404: không tìm thấy restaurant hoặc không có image.

### 5.6 Audios

#### `GET /Restaurant/{restaurantId}/audios`

- Auth: cần.
- Path param:
  - `restaurantId` (string)
- Body: không có.
- Response 200:

```json
[
  {
    "audioId": 200,
    "restaurantId": "res_001",
    "languageId": 1,
    "audioUrl": "/uploads/audios/xyz.mp3",
    "version": 1,
    "isActive": true,
    "dateGeneration": "2026-03-15T10:30:00Z"
  }
]
```

#### `POST /Restaurant/{restaurantId}/audios` (multipart/form-data)

- Auth: cần.
- Path param:
  - `restaurantId` (string)
- Form-data fields:
  - `language_id` (required, int)
  - `file` (required, binary)
- Response 200:

```json
{
  "audioId": 201,
  "restaurantId": "res_001",
  "languageId": 1,
  "audioUrl": "/uploads/audios/new.mp3",
  "version": 1,
  "isActive": true,
  "dateGeneration": "2026-03-15T10:40:00Z"
}
```

- Response 400: thiếu file.

#### `PATCH /Audios/{audioId}/active`

- Auth: cần.
- Path param:
  - `audioId` (int)
- Body (JSON):

```json
{
  "isActive": true
}
```

- Response 200:

```json
{
  "message": "Audio active status updated."
}
```

- Response 404: không tìm thấy audio.

#### `DELETE /Audios/{audioId}`

- Auth: cần.
- Path param:
  - `audioId` (int)
- Body: không có.
- Response 200:

```json
{
  "message": "Audio deleted successfully."
}
```

- Response 404: không tìm thấy audio.

---

Kết luận:

- Thêm `analytics_tables` không giúp cho seller module.
- Nhóm API Auth + UserRestaurants + Restaurant update + Dishes CRUD + Images CRUD + Audio CRUD đã được implement.
