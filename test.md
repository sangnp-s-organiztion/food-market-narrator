# API Endpoints checklist

Dưới đây là danh sách các endpoint trong `food_market_narrator_api` cần test, kèm thông tin nhanh về method, route, body/query, và lưu ý.

---

## Auth
- **POST /Auth/login** — Body: `LoginRequestDto` (username, password). AllowAnonymous. (tested?)
- **POST /Auth/logout** — Authorized. No body. (requires cookie/session)
- **GET /Auth/me** — Authorized. Returns `MeResponseDto`.




## Language
- **GET /Language** — Lấy tất cả language. (may return 404 nếu rỗng)
- **GET /Language/{languageCode}** — Lấy language theo code.




## Restaurant
- **GET /Restaurant** — Lấy tất cả restaurants. (Authorized)
- **GET /Restaurant/{id}** — Lấy restaurant theo id. (Authorized)
- **PATCH /Restaurant/{id}** — Cập nhật thông tin nhà hàng. Body: `UpdateRestaurantRequestDto`. (Authorized)
- **PATCH /Restaurant/{id}/status** — Cập nhật trạng thái active/inactive. Body: `UpdateRestaurantStatusRequestDto` (IsActive). (Authorized)




## Images
- **GET /Restaurant/{restaurantId}/images** — Lấy images của nhà hàng. (Authorized)
- **POST /Restaurant/{restaurantId}/images** — Upload image. Form-data: `file` (IFormFile), `is_primary` (bool), `sort_order` (int). `RequestSizeLimit(50_000_000)`. (Authorized)
- **DELETE /Images/{imageId}** — Xoá image theo id. (Authorized)
- **PATCH /Images/{imageId}/primary** — Đặt/huỷ primary cho image. Body: `SetPrimaryImageRequestDto` (IsPrimary). (Authorized)
- **PATCH /Restaurant/{restaurantId}/images/reorder** — Reorder images. Body: `ReorderImagesRequestDto` (Items). (Authorized)




## Dishes
- **GET /Restaurant/{restaurantId}/dishes** — Lấy dishes theo restaurant. Query: `page` (default 1), `pageSize` (default 20). (Authorized)
- **POST /Restaurant/{restaurantId}/dishes** — Tạo món mới. Body: `CreateDishRequestDto`. (Authorized)
- **PUT /Dishes/{dishId}** — Cập nhật món. Body: `UpdateDishRequestDto`. (Authorized)
- **DELETE /Dishes/{dishId}** — Xoá món. (Authorized)




## Users
- **GET /Users/{userId}/restaurants** — Lấy danh sách restaurants thuộc user. (Authorized)




## Audio
- **GET /Audio** — Lấy tất cả audio. (Authorized)
- **GET /Restaurant/{restaurantId}/audios** — Lấy audio theo restaurant. (Authorized)
- **POST /Restaurant/{restaurantId}/audios** — Upload audio. Form-data: `language_id` (int), `file` (IFormFile). `RequestSizeLimit(50_000_000)`. (Authorized)
- **PATCH /Audios/{audioId}/active** — Bật/tắt trạng thái audio. Body: `UpdateAudioActiveRequestDto` (IsActive). (Authorized)
- **DELETE /Audios/{audioId}** — Xoá audio. (Authorized)




---

Notes / Lưu ý khi test:
- Các endpoint có `[Authorize]` cần đăng nhập trước (POST `/Auth/login`) và gửi cookie hoặc header xác thực như app đang dùng.
- Upload endpoints (`/images`, `/audios`) dùng `multipart/form-data` và có giới hạn kích thước (`RequestSizeLimit`). Kiểm tra upload file hợp lệ + file quá lớn.
- Các endpoint PATCH/PUT cần test cả trường hợp dữ liệu hợp lệ và validation error (ModelState invalid).
- Các endpoint GET by id / DELETE nên test cả trường hợp resource không tồn tại (expect 404).

---

Nếu bạn muốn, tôi có thể tiếp tục và: 
- Sinh checklist Postman/Insomnia collection với các request mẫu.
- Tạo một file `tests/` mẫu (xUnit) để tự động chạy một số bài kiểm tra cơ bản.

File này được tạo/ghi bởi trợ lý tự động. Nếu cần thay đổi định dạng hoặc thêm thông tin request bodies DTO, cho tôi biết.
