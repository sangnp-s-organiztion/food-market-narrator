# API cho saler (cap nhat implementation)

Tai lieu nay tong hop cac API can co de chay day du cac man trong module saler, doi chieu voi backend hien tai.
Cap nhat moi nhat: da implement day du nhom API P0 va P1 ben duoi.

## 1) API hien co (food_market_narrator_api)

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

Nhan xet:

- Nhom API phuc vu saler da co du theo danh sach yeu cau P0/P1.
- Auth hien tai dang dung cookie session (khong tra JWT access token).

## 2) Feature saler va API da implement

### 2.1 Dang nhap + xac thuc

UI lien quan: LoginPage, DashboardSidebar (logout)

Da implement:

- `POST /Auth/login`
- `POST /Auth/logout` (neu dung cookie/session)
- `GET /Auth/me`

Request goi y:

- `POST /Auth/login`
  - body: `{ "username": "...", "password": "..." }`

Response hien tai:

- `POST /Auth/login` tra thong tin user va set cookie session (`fmn_saler_auth`).
- `GET /Auth/me` doc username tu claim trong cookie (khong truyen username qua query/body).

### 2.2 Chon nha hang theo user

UI lien quan: SelectRestaurantPage, DashboardLayout

Da implement:

- `GET /Users/{userId}/restaurants`
  - Tra danh sach nha hang ma user duoc quan ly.

Luu y DB:

- Neu khong co bang `UserRestaurant`, co the map truc tiep bang `Restaurant.user_id`.

### 2.3 Trang Nha hang (RestaurantPage)

UI lien quan: cap nhat profile nha hang, gio mo cua, trang thai is_active

Da implement:

- `PATCH /Restaurant/{id}`
- `PATCH /Restaurant/{id}/status`

Request goi y:

- `PATCH /Restaurant/{id}` body:
  - `name, description, phone, address, latitude, longitude, open_time, close_time`
- `PATCH /Restaurant/{id}/status` body:
  - `{ "is_active": true }`

### 2.4 Trang Thuc don (DishesPage)

UI lien quan: danh sach mon, them/sua/xoa

Da implement:

- `GET /Restaurant/{restaurantId}/dishes`
- `POST /Restaurant/{restaurantId}/dishes`
- `PUT /Dishes/{dishId}`
- `DELETE /Dishes/{dishId}`

Goi y toi uu:

- Ho tro phan trang cho list: `?page=1&pageSize=20`

### 2.5 Trang Hinh anh (ImagesPage)

UI lien quan: them anh, xoa anh, set anh chinh, doi thu tu

Da implement:

- `GET /Restaurant/{restaurantId}/images`
- `POST /Restaurant/{restaurantId}/images` (multipart/form-data)
- `DELETE /Images/{imageId}`
- `PATCH /Images/{imageId}/primary`
- `PATCH /Restaurant/{restaurantId}/images/reorder`

Request goi y:

- `PATCH /Images/{imageId}/primary` body: `{ "is_primary": true }`
- `PATCH /Restaurant/{restaurantId}/images/reorder` body:
  - `{ "items": [{ "image_id": 1, "sort_order": 1 }, ...] }`

### 2.6 Trang Audio (AudioPage)

UI lien quan: list theo nha hang, them audio theo ngon ngu, bat/tat, xoa

Da implement:

- `GET /Restaurant/{restaurantId}/audios`
- `POST /Restaurant/{restaurantId}/audios` (multipart/form-data)
- `PATCH /Audios/{audioId}/active`
- `DELETE /Audios/{audioId}`

Request goi y:

- `POST /Restaurant/{restaurantId}/audios`
  - fields: `language_id`, `file`
- `PATCH /Audios/{audioId}/active`
  - body: `{ "is_active": true }`

## 3) Uu tien implement de chay duoc saler nhanh

Muc toi thieu de thay mock bang API that:

Trang thai: da xong toan bo P0 va P1.

P0 (bat buoc):

- `POST /Auth/login`
- `GET /Auth/me`
- `GET /Users/{userId}/restaurants`
- `PATCH /Restaurant/{id}`
- `PATCH /Restaurant/{id}/status`
- `GET /Restaurant/{restaurantId}/dishes`
- `POST /Restaurant/{restaurantId}/dishes`
- `PUT /Dishes/{dishId}`
- `DELETE /Dishes/{dishId}`

P1 (nen co som):

- `GET /Restaurant/{restaurantId}/images`
- `POST /Restaurant/{restaurantId}/images`
- `DELETE /Images/{imageId}`
- `PATCH /Images/{imageId}/primary`
- `PATCH /Restaurant/{restaurantId}/images/reorder`
- `GET /Restaurant/{restaurantId}/audios`
- `POST /Restaurant/{restaurantId}/audios`
- `PATCH /Audios/{audioId}/active`
- `DELETE /Audios/{audioId}`

## 4) Luu y quan trong ve model du lieu

Can thong nhat kieu `restaurant_id` giua frontend va backend:

- Frontend saler hien dang dung `number`.
- DB/backend hien co nhieu cho dang `string/varchar`.

Khuyen nghi:

- Chon mot chuan duy nhat (nen theo DB la `string`) va dong bo TypeScript types + DTO API.

Neu khong dong bo som, ban se gap loi parse ID, route sai kieu, va update/xoa khong trung ban ghi.

## 5) API contract chi tiet (request/response)

Luu y chung:

- JSON key dang camelCase.
- Tat ca API ben duoi (tru `POST /Auth/login`) can cookie auth hop le.
- Doi voi frontend web, can gui kem cookie (`credentials: include`).

### 5.1 Auth

#### `POST /Auth/login`

- Auth: khong can.
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

- Response 400: thieu username/password.
- Response 401: sai tai khoan/mat khau hoac tai khoan bi khoa.
- Ghi chu: response set cookie `fmn_saler_auth`.

#### `POST /Auth/logout`

- Auth: can.
- Body: khong co.
- Response 200:

```json
{
  "message": "Logged out successfully."
}
```

#### `GET /Auth/me`

- Auth: can.
- Body: khong co.
- Response 200:

```json
{
  "userId": 12,
  "username": "seller01",
  "role": "Saler"
}
```

- Response 401: chua login hoac cookie het han.

### 5.2 User restaurants

#### `GET /Users/{userId}/restaurants`

- Auth: can.
- Path param:
  - `userId` (int)
- Body: khong co.
- Response 200 (mang nha hang):

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

- Auth: can.
- Path param:
  - `id` (string, restaurant_id)
- Body (JSON):

```json
{
  "name": "La Trattoria Bella",
  "description": "Nha hang Y",
  "phone": "0123456789",
  "address": "123 Pho Hue",
  "latitude": 21.0285,
  "longitude": 105.8542,
  "openTime": "08:00:00",
  "closeTime": "22:00:00"
}
```

- Response 200: tra lai object restaurant da cap nhat (cung schema voi `GET /Users/{userId}/restaurants`).
- Response 400: body khong hop le.
- Response 404: khong tim thay restaurant.

#### `PATCH /Restaurant/{id}/status`

- Auth: can.
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

- Response 404: khong tim thay restaurant.

### 5.4 Dishes

#### `GET /Restaurant/{restaurantId}/dishes?page=1&pageSize=20`

- Auth: can.
- Path param:
  - `restaurantId` (string)
- Query params:
  - `page` (int, mac dinh 1)
  - `pageSize` (int, mac dinh 20)
- Body: khong co.
- Response 200:

```json
[
  {
    "dishId": 10,
    "name": "Margherita Pizza",
    "price": 14.99,
    "description": "Pizza co dien",
    "restaurantId": "res_001",
    "imageId": 100,
    "createdAt": "2026-03-15T10:00:00Z"
  }
]
```

#### `POST /Restaurant/{restaurantId}/dishes`

- Auth: can.
- Path param:
  - `restaurantId` (string)
- Body (JSON):

```json
{
  "name": "Tiramisu",
  "price": 9.99,
  "description": "Mon trang mieng Y",
  "imageId": null
}
```

- Response 200: tra ve dish vua tao (schema nhu GET dishes item).
- Response 400: body khong hop le.

#### `PUT /Dishes/{dishId}`

- Auth: can.
- Path param:
  - `dishId` (int)
- Body (JSON):

```json
{
  "name": "Tiramisu size M",
  "price": 10.99,
  "description": "Cap nhat mo ta",
  "imageId": 101
}
```

- Response 200: tra ve dish da cap nhat.
- Response 400: body khong hop le.
- Response 404: khong tim thay dish.

#### `DELETE /Dishes/{dishId}`

- Auth: can.
- Path param:
  - `dishId` (int)
- Body: khong co.
- Response 200:

```json
{
  "message": "Dish deleted successfully."
}
```

- Response 404: khong tim thay dish.

### 5.5 Images

#### `GET /Restaurant/{restaurantId}/images`

- Auth: can.
- Path param:
  - `restaurantId` (string)
- Body: khong co.
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

- Auth: can.
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

- Response 400: thieu file.

#### `DELETE /Images/{imageId}`

- Auth: can.
- Path param:
  - `imageId` (int)
- Body: khong co.
- Response 200:

```json
{
  "message": "Image deleted successfully."
}
```

- Response 404: khong tim thay image.

#### `PATCH /Images/{imageId}/primary`

- Auth: can.
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

- Response 404: khong tim thay image.

#### `PATCH /Restaurant/{restaurantId}/images/reorder`

- Auth: can.
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

- Response 404: khong tim thay restaurant hoac khong co image.

### 5.6 Audios

#### `GET /Restaurant/{restaurantId}/audios`

- Auth: can.
- Path param:
  - `restaurantId` (string)
- Body: khong co.
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

- Auth: can.
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

- Response 400: thieu file.

#### `PATCH /Audios/{audioId}/active`

- Auth: can.
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

- Response 404: khong tim thay audio.

#### `DELETE /Audios/{audioId}`

- Auth: can.
- Path param:
  - `audioId` (int)
- Body: khong co.
- Response 200:

```json
{
  "message": "Audio deleted successfully."
}
```

- Response 404: khong tim thay audio.

---

Ket luan:

- Them `analytics_tables` khong giup cho saler module.
- Nhom API Auth + UserRestaurants + Restaurant update + Dishes CRUD + Images CRUD + Audio CRUD da duoc implement.
