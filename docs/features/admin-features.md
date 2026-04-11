# Tính năng Admin (React + TypeScript + Vite)

Cập nhật lần cuối: 2026-04-01

## 1. Phạm vi

Tài liệu này mô tả **trạng thái đã triển khai hiện tại** của frontend Admin mới trong `admin/`.

Nội dung tập trung vào:

- các route và trang
- luồng xác thực
- trạng thái nguồn dữ liệu (API thật so với mock)
- trạng thái tích hợp analytics
- các khoảng trống đã biết và bước tiếp theo an toàn

## 2. Công nghệ sử dụng (Admin)

- React 18 + TypeScript + Vite
- React Router v6
- TanStack Query
- shadcn/ui + Tailwind
- Bản đồ Leaflet cho heatmap và đường di chuyển

## 3. Điều hướng và định tuyến

Các route bảo vệ được khai báo trong `admin/src/App.tsx` và được chặn bởi `AuthProvider` + `ProtectedRoute`.

- `/login`: trang đăng nhập
- `/`: dashboard (tổng quan + widget analytics)
- `/restaurants`: quản lý nhà hàng
- `/users`: quản lý người dùng
- `/trajectory`: theo dõi tuyến di chuyển visitor
- `/tours`: quản lý hành trình (tour)
- `/translation-billing`: theo dõi chi phí dịch/TTS
- `/qr-code`: quản lý QR mở app
- `/account`: quản lý tài khoản admin
- `/logs`: hoạt động nghe gần đây

Menu sidebar được định nghĩa trong `admin/src/components/AdminSidebar.tsx`.

## 4. Xác thực (dùng Cookie)

Luồng hiện tại sử dụng API xác thực backend và phiên cookie:

- `POST /Auth/admin/login`
- `GET /Auth/admin/me`
- `POST /Auth/admin/logout`

Các file triển khai:

- `admin/src/lib/authApi.ts`
- `admin/src/contexts/AuthContext.tsx`
- `admin/src/pages/LoginPage.tsx`

Hành vi:

- Khi app khởi động, frontend gọi `/Auth/admin/me` để khôi phục trạng thái đăng nhập từ cookie.
- Route bảo vệ sẽ chờ hoàn tất bootstrap (`isLoading`) trước khi chuyển hướng.
- Logout gọi API theo cơ chế best-effort, sau đó xóa trạng thái xác thực cục bộ.

## 5. Trạng thái nguồn dữ liệu (Quan trọng)

### 5.1 Các tính năng đã dùng API thật

1. Trang quản lý người dùng (`/users`)

- GET `/api/users`
- POST `/api/users`
- PATCH `/api/users/{id}/role`
- PATCH `/api/users/{id}/status`

2. Trang quản lý nhà hàng (`/restaurants`)

- GET `/restaurant`
- GET `/restaurant/{id}`
- PATCH `/restaurant/{id}`
- PATCH `/restaurant/{id}/status`

3. Widget analytics trên dashboard (`/`)

- GET `/api/analytics/kpis`
- GET `/api/analytics/top-restaurants`
- GET `/api/analytics/heatmap`
- GET `/api/analytics/movement-paths`

4. Trang nhật ký hoạt động (`/logs`)

- GET `/api/analytics/recent-activity`
- GET `/api/audit-logs`

5. Trang tuyến di chuyển (`/trajectory`)

- GET `/api/analytics/movement-paths`

6. Trang hành trình (`/tours`)

- GET `/Tour`
- GET `/Tour/{id}`
- POST `/Tour`
- PATCH `/Tour/{id}`
- POST `/Tour/{id}/restaurants`
- DELETE `/Tour/{id}/restaurants/{restaurantId}`
- PUT `/Tour/{id}/stops/order`
- POST `/Tour/upload-image`
- POST `/Tour/{id}/upload-image`

7. Trang chi phí dịch (`/translation-billing`)

- GET `/api/admin/translation-billing/monthly`
- GET `/api/admin/translation-billing/usage`
- GET `/api/admin/translation-billing/audio-usage`

8. Trang QR (`/qr-code`)

- POST `/Auth/admin/qr-code`

### 5.2 Các phần vẫn dùng dữ liệu tĩnh/mock

1. Các thẻ KPI thực thể trên dashboard (tổng nhà hàng/audio/người dùng/món ăn)

- vẫn dùng hằng số cục bộ và import `mockData`

2. Biểu đồ dashboard "Lượt nghe theo ngày"

- hiện đang hard-code dataset ngay trong component trang

3. Fallback marker POI cho heatmap

- nếu dữ liệu POI từ API rỗng, bản đồ sẽ fallback sang danh sách nhà hàng mock

4. `admin/src/lib/mockData.ts`

- vẫn còn tồn tại và còn được tham chiếu một phần cho hành vi fallback/placeholder UI

## 6. Hợp đồng Analytics API được Admin sử dụng

Định nghĩa client nằm ở `admin/src/lib/analyticsApi.ts` và kiểu response kỳ vọng nằm ở `admin/src/types/analytics.ts`.

### 6.1 Endpoint đang dùng

- `GET /api/analytics/kpis`
- `GET /api/analytics/heatmap?hours={number}`
- `GET /api/analytics/top-audios?limit={number}`
- `GET /api/analytics/top-restaurants?limit={number}`
- `GET /api/analytics/movement-paths?sessionLimit={number}`
- `GET /api/analytics/recent-activity?limit={number}`
- `GET /api/analytics/audio-stats`

### 6.2 Cách UI dùng theo trang

- Dashboard hiện dùng: kpis, heatmap, top-restaurants, movement-paths
- Trang logs dùng: recent-activity (tự làm mới mỗi 30 giây)
- API top-audios/audio-stats đã có trong client nhưng chưa render thành trang/widget riêng

## 7. Ảnh chụp nhanh tính năng theo từng trang

### 7.1 Dashboard (`/`)

Đã triển khai:

- layout tổng quan hệ thống
- thẻ KPI analytics (tổng lượt phát hợp lệ, thời gian nghe trung bình)
- biểu đồ cột top nhà hàng (API)
- khu vực heatmap (điểm API)
- bản đồ đường di chuyển ẩn danh (phiên API)

Triển khai một phần / placeholder:

- thẻ KPI thực thể vẫn là dữ liệu tĩnh
- biểu đồ vùng lượt nghe theo ngày vẫn là dataset tĩnh

### 7.2 Nhà hàng (`/restaurants`)

Đã triển khai:

- lấy danh sách nhà hàng từ API
- tìm kiếm theo tên/địa chỉ (phía client)
- khóa/mở khóa nhà hàng qua status API
- trạng thái loading/empty/error

### 7.3 Người dùng (`/users`)

Đã triển khai:

- lấy danh sách người dùng từ API
- tạo người dùng
- khóa/mở khóa người dùng
- đổi vai trò (mapping admin/editor)
- trạng thái loading/empty/error

### 7.4 Nhật ký (`/logs`)

Đã triển khai:

- đọc hoạt động gần đây từ analytics API
- hiển thị nhãn hành động suy luận theo thời lượng
- tự làm mới mỗi 30 giây
- trạng thái loading/empty/error

### 7.5 Tuyến di chuyển (`/trajectory`)

Đã triển khai:

- hiển thị movement paths theo session ẩn danh
- dùng `sessionLimit = 100` trong UI hiện tại
- trạng thái loading/empty/error

### 7.6 Hành trình (`/tours`)

Đã triển khai:

- tạo tour mới (name/description/estimatedDuration/image)
- xem danh sách và xem chi tiết tour
- thêm/xóa nhà hàng trong tour
- đổi thứ tự stop trong tour
- bật/tắt trạng thái hoạt động tour
- tải ảnh cover chung hoặc ảnh cho tour cụ thể

### 7.7 Chi phí dịch (`/translation-billing`)

Đã triển khai:

- bảng tổng hợp chi phí theo tháng và seller
- bảng chi tiết usage dịch văn bản
- bảng chi tiết usage audio/TTS
- filter theo tháng + seller + phân trang

### 7.8 Mã QR (`/qr-code`)

Đã triển khai:

- upload file QR PNG cho admin portal
- thay thế bản QR hiện tại dùng để mở app visitor

### 7.9 Tài khoản (`/account`)

Đã triển khai:

- xem và cập nhật profile admin
- đổi mật khẩu
- đồng bộ trạng thái user sau cập nhật

## 8. Cấu hình môi trường

Tất cả API client đều dùng:

- `VITE_API_BASE_URL` (nếu được cung cấp)
- fallback: `http://localhost:5044`

Áp dụng cho:

- `authApi`
- `adminApi`
- `analyticsApi`

Mọi request đều gửi `credentials: include` để hỗ trợ cookie auth.

## 9. Rủi ro tích hợp đã biết

1. Nếu cách đặt tên route backend khác `/api/...`, frontend sẽ lỗi cho đến khi đồng bộ route/base-path.
2. Nếu chính sách cookie CORS chưa cấu hình đúng, request cần xác thực sẽ thất bại dù đã đăng nhập.
3. Dashboard vẫn đang trộn analytics thật và thẻ tĩnh, có thể gây nhiễu cho người vận hành khi số liệu lệch nhau.

## 10. Bước tiếp theo khuyến nghị (theo hướng bổ sung)

1. Thay các thẻ KPI thực thể tĩnh bằng số liệu đếm từ API thật.
2. Thay biểu đồ lượt nghe theo ngày tĩnh bằng endpoint analytics dạng timeseries.
3. Bỏ fallback mock của heatmap khi dữ liệu production đã ổn định.
4. Thêm widget/trang riêng cho top audio bằng các hàm client sẵn có `getTopAudios` hoặc `getAudioStats`.

## 11. Checklist nghiệm thu

- [x] Đăng nhập dùng API cookie auth từ backend
- [x] Trang users đọc/ghi API thật
- [x] Trang restaurants đọc/ghi API thật
- [x] Trang logs đọc API analytics thật
- [x] Dashboard đọc các API analytics cốt lõi
- [ ] Toàn bộ chỉ số dashboard đều real-time hoàn toàn (vẫn còn phần tĩnh)
- [ ] Dữ liệu mock đã được loại bỏ hoàn toàn khỏi runtime path
