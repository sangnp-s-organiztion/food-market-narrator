# RESTful API Design Standard

## 1. Naming (URL)

- Dùng **danh từ (noun)**, không dùng động từ
- Dùng **số nhiều (plural)**
- Dùng chữ thường, có thể dùng `-`

**Đúng:**

```
/products
/orders
/users
/order-items
```

**Sai:**

```
/getProducts
/createOrder
/deleteUser
```

---

## 2. HTTP Methods

| Method | Ý nghĩa           |
| ------ | ----------------- |
| GET    | Lấy dữ liệu       |
| POST   | Tạo mới           |
| PUT    | Cập nhật toàn bộ  |
| PATCH  | Cập nhật một phần |
| DELETE | Xóa               |

**Ví dụ:**

```
GET    /products
GET    /products/{id}
POST   /products
PUT    /products/{id}
PATCH  /products/{id}
DELETE /products/{id}
```

---

## 3. Status Code

| Code | Ý nghĩa               |
| ---- | --------------------- |
| 200  | OK                    |
| 201  | Created               |
| 400  | Bad Request           |
| 401  | Unauthorized          |
| 403  | Forbidden             |
| 404  | Not Found             |
| 500  | Internal Server Error |

---

## 4. Quy tắc chung

- URL = resource
- Method = hành động
- Không dùng động từ trong URL
- Trả về đúng HTTP status code

## 5. Public API (for MAUI App)

API dùng cho ứng dụng .NET MAUI phải là Public API vì APP không yêu cầu đăng nhập
API public không được để /public trong tên API
Quy tắc:

Base URL:
https://api.yourdomain.com/api/
Không dùng localhost (trừ môi trường dev)
Bật CORS cho mobile client
Sử dụng HTTPS bắt buộc
