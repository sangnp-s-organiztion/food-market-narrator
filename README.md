# Food Market Narrator

Nền tảng thuyết minh tự động cho phố ẩm thực Vĩnh Khánh, giúp khách tham quan khám phá quán ăn qua bản đồ, vị trí thời gian thực và audio theo ngôn ngữ.

## 1. Tổng quan

Workspace này gồm nhiều ứng dụng chạy cùng một hệ sinh thái:

- `food_market_narrator_api`: Backend ASP.NET Core Web API + EF Core + SQL Server.
- `food-market-narrator-maui`: Ứng dụng mobile cho visitor (MAUI).
- `saler`: Web app cho seller/chủ quán (React + Vite + TypeScript).
- `admin`: Cổng quản trị nội bộ (ASP.NET Core MVC, có thể chuyển dần sang React theo roadmap tài liệu).
- `docs`: Tài liệu sản phẩm, kiến trúc, feature, PRD.

## 2. Kiến trúc nhanh

```text
Mobile MAUI / Seller Web / Admin Portal
                |
                v
     food_market_narrator_api (REST)
                |
                +--> SQL Server (dữ liệu nghiệp vụ)
                +--> Static media (/images, /audios, /audios)
```

## 3. Công nghệ chính

- Backend: .NET 10, ASP.NET Core Web API, EF Core SQL Server.
- Mobile: .NET MAUI (Android/iOS/Windows), Mapsui, Plugin.Maui.Audio.
- Seller Web: React 18, Vite 5, TypeScript, React Query.
- Admin: ASP.NET Core MVC (.NET 10).
- API format: REST/JSON.
- Auth: Cookie authentication + role-based authorization.

## 4. Cấu trúc thư mục

```text
food-market-narrator/
├─ food_market_narrator_api/
├─ food-market-narrator-maui/
├─ saler/
├─ admin/
├─ docs/
├─ db.sql
└─ README.md
```

## Quick Start

### Prerequisites

- .NET 10 SDK
- Node.js 20+ và npm
- SQL Server (local hoặc remote)
- MAUI workload (nếu chạy mobile): `dotnet workload install maui`

### Setup

```bash
# Clone source
git clone <repo-url>
cd food-market-narrator

# Backend API
cd food_market_narrator_api
dotnet restore
dotnet run

# Seller frontend (terminal mới)
cd ../saler
npm install
npm run dev

# Admin portal (terminal mới)
cd ../admin
dotnet restore
dotnet run

# MAUI app (terminal mới, optional)
cd ../food-market-narrator-maui
dotnet restore
dotnet run -f net10.0-android
```

### Services

| Service               | URL                    |
| --------------------- | ---------------------- |
| Backend API (HTTP)    | http://localhost:5044  |
| Backend API (HTTPS)   | https://localhost:7041 |
| Seller Web (Vite dev) | http://localhost:8080  |
| Admin Portal (HTTP)   | http://localhost:5104  |
| Admin Portal (HTTPS)  | https://localhost:7168 |

### Commands

```bash
# API
cd food_market_narrator_api
dotnet run                     # Run API
dotnet build                   # Build API

# Seller frontend
cd saler
npm run dev                    # Start dev server (port 8080)
npm run build                  # Production build
npm run lint                   # ESLint
npm run test                   # Unit tests (Vitest)

# Admin portal
cd admin
dotnet run                     # Run admin portal
dotnet build                   # Build admin portal

# MAUI app
cd food-market-narrator-maui
dotnet build                   # Build MAUI app
dotnet run -f net10.0-android  # Run on Android target
```

## 5. Yêu cầu môi trường

### 5.1 Bắt buộc

- .NET SDK 10.x
- SQL Server (local hoặc remote)
- Node.js 20+ (khuyến nghị LTS)
- npm hoặc bun (repo có `bun.lockb`, nhưng npm vẫn dùng được)

### 5.2 Cho MAUI

- MAUI workload: `dotnet workload install maui`
- Android SDK/Emulator (nếu chạy Android)
- Visual Studio 2022 hoặc toolchain MAUI đầy đủ

## 6. Cấu hình

### 6.1 API connection string

Sửa tại:

- `food_market_narrator_api/appsettings.json`
- `food_market_narrator_api/appsettings.Development.json`

Ví dụ:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=food_market_narrator;User Id=sa;Password=***;TrustServerCertificate=True;"
  }
}
```

### 6.2 Base URL cho MAUI

Sửa tại `food-market-narrator-maui/Settings/AppSettings.cs`.

- Android emulator: `http://10.0.2.2:5044/`
- Desktop/local: `http://localhost:5044/`

## 7. Chạy local nhanh

### 7.1 Chạy backend API

```bash
cd food_market_narrator_api
dotnet restore
dotnet run
```

Mặc định:

- `http://localhost:5044`
- `https://localhost:7041`

### 7.2 Chạy Seller web

```bash
cd saler
npm install
npm run dev
```

### 7.3 Chạy Admin portal

```bash
cd admin
dotnet restore
dotnet run
```

### 7.4 Chạy MAUI app

```bash
cd food-market-narrator-maui
dotnet restore
dotnet build
```

Ví dụ chạy Android:

```bash
dotnet run -f net10.0-android
```

## 8. API public cho visitor

Các endpoint đọc dữ liệu công khai chính:

- `GET /Restaurant`
- `GET /Restaurant/{id}`
- `GET /Language`
- `GET /Language/{languageCode}`
- `GET /public/Restaurant/{restaurantId}/images`
- `GET /public/Restaurant/{restaurantId}/dishes`
- `GET /public/Restaurant/{restaurantId}/audios`

## 9. Luồng dữ liệu chính

### 9.1 Visitor

1. App MAUI gọi `GET /Restaurant` để lấy POI.
2. Người dùng chọn POI, app gọi các endpoint public chi tiết (ảnh/món/audio).
3. Khi bật narration, app theo dõi GPS, tìm POI gần nhất và phát audio theo ngôn ngữ.
4. Nếu mạng lỗi, app fallback về cache local (POI/audio).

### 9.2 Seller/Admin

1. Đăng nhập qua cookie auth.
2. Gọi protected APIs để CRUD restaurant, dish, image, audio, user/role.
3. API ghi SQL + cập nhật media storage.
4. Dữ liệu mới được visitor nhận ở lần đồng bộ tiếp theo.

## 10. Tài liệu liên quan

- `docs/prd.md`
- `docs/architecture.md`
- `docs/feature/visitor-feature.md`
- `docs/maui/`
- `docs/saler/`

## 11. Lưu ý bảo mật

- Không commit secrets thật (connection string production, API key, token).
- Ưu tiên dùng biến môi trường hoặc secret manager.
- Kiểm tra kỹ quyền truy cập endpoint trước khi deploy.

## 12. Đóng góp

Quy trình khuyến nghị:

1. Tạo branch theo feature/bugfix.
2. Cập nhật code + tài liệu liên quan trong `docs`.
3. Chạy test/lint/build tương ứng từng app trước khi tạo PR.

## 13. License

Chưa khai báo. Nếu cần public dự án, thêm file `LICENSE` tại thư mục gốc.
