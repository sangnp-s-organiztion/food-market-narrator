# Local Development Setup

## Prerequisites

- .NET SDK 10+
- Node.js 20+
- SQL Server
- Docker (nếu chạy Mongo local)
- MAUI workload nếu build mobile: `dotnet workload install maui`

## Chạy nhanh

## Database setup

- SQL Server: bắt buộc cho backend chính. Chuẩn bị schema theo file `db.sql` ở root repo.
- MongoDB: xem hướng dẫn chi tiết tại [docs/api/mongodb-setup.md](../api/mongodb-setup.md).
- Nếu chạy Mongo bằng Docker, đảm bảo đã map cổng `27017:27017` trước khi chạy API.

### API

```bash
cd FoodMarketNarrator.Api
dotnet restore
dotnet run
```

### Saler

```bash
cd saler
npm install
npm run dev
```

### Admin

```bash
cd admin
dotnet restore
dotnet run
```

### MAUI Android

```bash
cd FoodMarketNarrator.Maui
dotnet restore
dotnet run -f net10.0-android
```

## Lưu ý MAUI khi test trên Android thật

- Sửa `LocalApiHost` trong `FoodMarketNarrator.Maui/Settings/AppSettings.cs`.
- Điện thoại và máy chạy API phải cùng mạng.
