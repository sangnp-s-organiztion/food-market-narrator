# MongoDB Setup (Food Market Narrator)

Tài liệu này mô tả cách chạy MongoDB local và khởi tạo collection/index theo đúng script hiện có của dự án.

## 1. Thông tin MongoDB đang dùng

- Mongo image: `mongo:8.2.6`
- Username: `admin`
- Password: `root@1133`
- Port: `27017`
- Database name: `food_market_narrator`
- Auth source: `admin`

Connection string trong API hiện tại:

```text
mongodb://admin:root%401133@localhost:27017/?authSource=admin
```

## 2. Các file setup Mongo hiện có

- `setup-collections.mongo.js`: tạo collections và indexes
- `seed-data.mongo.js`: file seed dữ liệu (hiện tại đang trống)

## 3. Chạy Mongo bằng Docker (khớp cấu hình dự án)

```bash
docker run -d \
  --name mongo-fmn \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=root@1133 \
  mongo:8.2.6
```

## 4. Khởi tạo collections/indexes

Chạy script setup:

```bash
mongosh "mongodb://admin:root%401133@localhost:27017/admin" \
  --file setup-collections.mongo.js
```

Nếu cần seed dữ liệu:

```bash
mongosh "mongodb://admin:root%401133@localhost:27017/admin" \
  --file seed-data.mongo.js
```

## 5. Collections và indexes được tạo từ script

### 5.1 UserSessions

- Collection: `UserSessions`
- Purpose: lưu phiên ẩn danh theo thiết bị
- Required fields:
  - `device_id` (string, required, unique)
  - `device_info` (string, optional)
  - `created_at` (ISODate)
- Indexes:
  - `{ device_id: 1 }` with `unique: true`
  - `{ created_at: -1 }`

### 5.2 LocationLogs

- Collection: `LocationLogs`
- Indexes:
  - `{ session_id: 1 }`
  - `{ timestamp: -1 }`
  - `{ location: "2dsphere" }` (geo index)

### 5.3 AudioLogs

- Collection: `AudioLogs`
- Indexes:
  - `{ session_id: 1 }`
  - `{ restaurant_id: 1 }`
  - `{ timestamp: -1 }`
  - `{ restaurant_id: 1, timestamp: -1 }` (compound index)

### 5.4 Lưu ý quan trọng về index `device_id`

Nếu trước đây đã tạo index thường `{ device_id: 1 }` rồi mới chuyển sang unique, hãy drop index cũ trước khi tạo lại unique index để tránh xung đột.

```js
db.UserSessions.dropIndex("device_id_1");
db.UserSessions.createIndex(
  { device_id: 1 },
  { name: "ux_user_sessions_device_id", unique: true },
);
```

Khuyến nghị:

- `device_id` nên luôn lưu dưới dạng string (không lưu object).
- Có thể chuẩn hóa `device_id` bằng UUID hoặc fingerprint hash để tránh lộ định danh thô.

## 6. API test kết nối Mongo

Backend đã có endpoint test kết nối:

- Method: `GET`
- Path: `/Mongo/test-connect`
- Public endpoint: có

Ví dụ gọi local:

```text
http://localhost:5044/Mongo/test-connect
```

Kỳ vọng:

- `200 OK` khi kết nối Mongo thành công
- `503 Service Unavailable` khi kết nối thất bại

## 7. Lưu ý môi trường

- Nếu API chạy trực tiếp trên máy local, giữ `localhost:27017` như hiện tại.
- Nếu API chạy trong Docker, `localhost` sẽ không trỏ tới container Mongo. Khi đó cần đổi host trong connection string sang tên service/container Mongo.
