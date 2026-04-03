# MSSQL Setup (Food Market Narrator)

Tài liệu này mô tả cách cài đặt SQL Server (MSSQL) local và khởi tạo database/table theo cấu trúc dự án.

---

## 1. Thông tin MSSQL đang dùng

- SQL Server: `SQL Server 2022` (hoặc mới hơn)
- Host: `localhost`
- Port: `1433`
- Username: `sa`
- Password: `root@1133`
- Database name: `food_market_narrator`

Connection string trong API:

```text
Server=localhost,1433;Database=food_market_narrator;User Id=sa;Password=root@1133;TrustServerCertificate=True;
```

---

## 2. Các file setup MSSQL

- `mssql-setup.sql`: tạo database + tables + indexes
- `seed-data.sql`: seed dữ liệu (optional)

---

## 3. Chạy MSSQL bằng Docker

```bash
docker run -d \
  --name mssql-fmn \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=root@1133" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

---

## 4. Khởi tạo database & tables

Kết nối vào SQL Server bằng `Dbeaver` hoặc công cụ tương tự, sau đó chạy:

```sql
CREATE DATABASE food_market_narrator;
GO

USE food_market_narrator;
GO
```

Chạy file setup:

```bash
sqlcmd -S localhost,1433 -U sa -P root@1133 -i mssql-setup.sql
```

Seed dữ liệu (nếu có):

```bash
sqlcmd -S localhost,1433 -U sa -P root@1133 -i seed-data.sql
```

---

## 5. Tables và Indexes

### 5.1 Products

- Table: `products`

- Columns:
  - `product_id` (INT, PK, IDENTITY)
  - `product_name` (NVARCHAR)
  - `price` (DECIMAL)

- Indexes:
  - PK clustered on `product_id`

---

### 5.2 Orders

- Table: `orders`

- Columns:
  - `order_id` (INT, PK, IDENTITY)
  - `user_id` (INT)
  - `created_at` (DATETIME)

- Indexes:
  - `{ user_id }`
  - `{ created_at DESC }`

---

### 5.3 OrderItems

- Table: `order_items`

- Columns:
  - `id` (INT, PK, IDENTITY)
  - `order_id` (INT, FK)
  - `product_id` (INT, FK)
  - `quantity` (INT)

- Indexes:
  - `{ order_id }`
  - `{ product_id }`

---

## 6. Lưu ý quan trọng

- Luôn sử dụng `NVARCHAR` để hỗ trợ Unicode (tiếng Việt)
- Sử dụng `IDENTITY` cho khóa chính tự tăng
- Đặt tên table dạng **plural** (products, orders)
- Sử dụng `DATETIME` hoặc `DATETIME2` cho timestamp

---

## 7. API test kết nối MSSQL

Backend nên có endpoint test:

- Method: `GET`
- Path: `/sql/test-connect`
- Public endpoint: có

Ví dụ:

```text
http://localhost:5044/sql/test-connect
```

Kỳ vọng:

- `200 OK` → kết nối thành công
- `503 Service Unavailable` → lỗi kết nối

---

## 8. Lưu ý môi trường

- Nếu chạy local: dùng `localhost,1433`
- Nếu chạy Docker:
  - Không dùng `localhost`
  - Dùng tên container: `mssql-fmn`

Ví dụ:

```text
Server=mssql-fmn,1433;Database=food_market_narrator;User Id=sa;Password=YourStrong!Pass123;
```

---

END OF DOCUMENT
