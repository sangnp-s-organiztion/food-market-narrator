# Báo cáo phân tích API trùng lặp

Sau khi đọc toàn bộ backend (53 endpoint), MAUI (9 endpoint), saler (21 endpoint), admin (25 endpoint), đây là kết quả:

---

## GROUP 1: TRÙNG NGUYÊN LOGIC — Cùng route, cùng data, cùng trả về

| Route | Backend (2 controller) | Dùng bởi |
|---|---|---|
| `GET /Restaurant/{id}/dishes` | `DishesController` + `PublicDataController` | MAUI |
| `GET /Restaurant/{id}/images` | `RestaurantController` + `PublicDataController` | Saler |
| `GET /Restaurant/{id}/audios` | `AudioController` + `PublicDataController` | Saler, MAUI |

**→ Tìm thấy 2 cặp trùng nguyên có thể gộp được (xóa PublicDataController, dùng route gốc đã public).**

**→ `/Restaurant/{id}/dishes` KHÔNG gộp được** vì route gốc nằm trên `DishesController` có `[Authorize]` class-level, chưa được khai báo public trong `PublicEndpoints` — `PublicDataController` vẫn cần giữ cho MAUI gọi public.

---

## GROUP 2: TRÙNG ROUTE NHƯNG LOGIC KHÁC — MAUI App đang gọi sai endpoint

| Route gọi trong MAUI | Backend chính (public) | Backend duplicate (public) | Vấn đề |
|---|---|---|---|
| `GET /Restaurant/{id}/dishes` | `DishesController` — **có** `[Authorize]` class-level | `PublicDataController` — **public** | MAUI gọi endpoint yêu cầu auth trong khi MAUI **không có cookie/session**. Lẽ ra phải gọi `/public/Restaurant/{id}/dishes` |
| `GET /restaurant` | `RestaurantController` — public (có convention) | — | Đúng (lowercase `r`) |
| `GET /language` | `LanguageController` — public | — | Backend dùng `/Language` (capital L), MAUI dùng `/language`. Có thể chạy được nhờ route case-insensitive, nhưng **không đúng convention** |

---

## GROUP 3: KHÔNG TRÙNG

Các endpoint sau chỉ tồn tại trong một consumer duy nhất:

| Consumer | Endpoint | Mục đích |
|---|---|---|
| Admin | `/api/admin/stats/restaurants/audios/users/dishes/count` (4 endpoint) | Dashboard stats |
| Admin | `/api/analytics/kpis, heatmap, top-audios, ...` (7 endpoint) | Analytics |
| Admin | `/api/audit-logs` | Audit log |
| Admin | `/api/users/*` (5 endpoint) | User management |
| Admin | `/Restaurant` (GET, POST, PATCH x3) | Restaurant CRUD |
| Saler | `/Restaurant` (GET, PATCH, PATCH/status) | Own restaurant |
| Saler | `/Restaurant/{id}/dishes` (POST), `/Dishes/{id}` (PUT, DELETE) | Dish CRUD |
| Saler | `/Images/{id}` (POST, DELETE, PUT, PATCH primary, PATCH reorder) | Image management |
| Saler | `/Audios/{id}` (POST, PATCH active, DELETE) | Audio management |
| MAUI | `/public/audios/{id}/file` | Stream audio |
| MAUI | `/public/audios/{id}/file` (by path) | Stream audio by path |
| MAUI | `/api/user-sessions/start`, `/api/location-logs/batch`, `/api/audio-logs` | Session + tracking |

---

## Tổng kết

| Loại | Số cặp/trường hợp |
|---|---|
| Trùng nguyên (PublicDataController duplicate) | **3 cặp** |
| Sai endpoint (MAUI gọi authenticated route) | **1 trường hợp rõ ràng** (`/Restaurant/{id}/dishes`) |
| Không trùng | Tất cả còn lại |

---

## Cần hành động

1. **`PublicDataController`**: Có 3 endpoint trùng nguyên — nên xóa controller này và dùng endpoint gốc, vì backend đã có `PublicEndpointConvention` cho phép truy cập public mà không cần `[AllowAnonymous]`.

2. **MAUI `POIService.cs`**: Đổi `GetDishesByRestaurantIdAsync` từ `/Restaurant/{restaurantId}/dishes` → `/public/Restaurant/{restaurantId}/dishes` để tránh bị 401 (vì MAUI không gửi cookie auth).

3. **MAUI `LanguageService.cs`**: Kiểm tra xem route `/language` vs `/Language` có ổn không, nên thống nhất dùng `/Language` theo backend convention.
