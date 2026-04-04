# Food Market Narrator

Food Market Narrator là hệ sinh thái gồm mobile app + backend API + 2 web apps (admin/saler) để hỗ trợ trải nghiệm thuyết minh tự động theo vị trí tại phố ẩm thực Vĩnh Khánh.

## 1. Thành phần hệ thống

- FoodMarketNarrator.Api: ASP.NET Core Web API, dữ liệu chính trên SQL Server, analytics/logs trên MongoDB.
- FoodMarketNarrator.Maui: ứng dụng mobile visitor (Android) với GPS tracking, geofence và phát audio tự động.
- admin: dashboard quản trị (React + Vite + TypeScript).
- saler: dashboard người bán/chủ quán (React + Vite + TypeScript).
- test: bộ test cho API và MAUI.
- docs: tài liệu kỹ thuật, setup, testing, kiến trúc.

## 2. Kiến trúc nhanh

```text
MAUI (visitor) / Admin web / Saler web
                |
                v
      FoodMarketNarrator.Api (REST + Cookie Auth)
                |
        +-------+---------------------+
        |                             |
   SQL Server                    MongoDB
 (nghiep vu)               (session/log/analytics)
```

## 3. Tech stack

- Backend: .NET 10, ASP.NET Core Web API, EF Core SQL Server, MongoDB.Driver.
- Mobile: .NET MAUI (net10.0-android), Mapsui, Plugin.Maui.Audio.
- Admin/Saler: React 18, TypeScript, Vite 5, TanStack Query, Vitest.

## 4. Local prerequisites

- .NET SDK 10.x
- Node.js 20+ và npm
- SQL Server
- MongoDB (khuyến nghị chạy local qua Docker)
- MAUI workload (nếu build app): `dotnet workload install maui`

## 5. Chạy local

### 5.1 Backend API

```bash
cd FoodMarketNarrator.Api
dotnet restore
dotnet run
```

Mặc định:

- <http://localhost:5044>
- <https://localhost:7041>

### 5.2 Admin web

```bash
cd admin
npm install
npm run dev
```

### 5.3 Saler web

```bash
cd saler
npm install
npm run dev
```

### 5.4 MAUI app (Android)

```bash
cd FoodMarketNarrator.Maui
dotnet restore
dotnet build
dotnet run -f net10.0-android
```

## 6. Test nhanh

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj

cd admin && npm test
cd ../saler && npm test
```

Xem chi tiết tại `test-guide.md`.

## 7. API public chính cho MAUI

- GET /Restaurant
- GET /Restaurant/{id}
- GET /Language
- GET /Language/{languageCode}
- GET /Restaurant/{restaurantId}/images
- GET /public/Restaurant/{restaurantId}/dishes
- GET /public/Restaurant/{restaurantId}/audios

## 8. Tài liệu

- docs/README.md
- docs/architecture/overview.md
- docs/setup/local-development.md
- docs/testing/test-strategy.md
- docs/api/README.md
- docs/mobile/README.md
- docs/admin/README.md
- docs/saler/README.md

## 9. Ghi chú bảo mật

- Credentials trong repo chỉ dùng cho local/dev.
- Không dùng trực tiếp cho production.
- Khi deploy thật phải thay secrets bằng môi trường production.
