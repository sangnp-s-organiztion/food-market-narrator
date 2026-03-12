# 🍜 Food Market Narrator

Ứng dụng hỗ trợ khám phá khu ẩm thực bằng bản đồ và thuyết minh tự động theo vị trí người dùng.

## 📌 Tổng quan

Dự án gồm 2 thành phần chính:

- 🧠 **Backend API** (`food_market_narrator_api`): cung cấp dữ liệu nhà hàng/điểm POI từ SQL Server.
- 📱 **Frontend MAUI** (`food-market-narrator-maui`): ứng dụng đa nền tảng (Android, iOS), hiển thị bản đồ và phát audio khi người dùng đến gần POI.

## ✨ Tính năng chính

- 🗺️ Hiển thị POI trên bản đồ (Google Maps trên Android).
- 🔎 Xem chi tiết các POIs.
- 📍 Theo dõi vị trí người dùng theo chu kỳ.
- 🎯 Tự động xác định POI gần nhất theo bán kính kích hoạt.
- 🔊 Tự động phát file thuyết minh theo ngôn ngữ đã chọn.
- 📶 Hoạt động ngay cả khi không có kết nối mạng

## 🏗️ Kiến trúc dự án

```text
food-market-narrator/
├─ food_market_narrator_api/       # ASP.NET Core Web API + EF Core + SQL Server
└─ food-market-narrator-maui/      # .NET MAUI app (maps + narration)
```

### ⚙️ Backend (ASP.NET Core)

- Framework: .NET 10 (`net10.0`)
- Data access: Entity Framework Core + SQL Server
- Mô hình tách lớp:
  - `Controllers/RestaurantController.cs`
  - `Services/RestaurantService.cs`
  - `Repositories/RestaurantRepository.cs`
  - `Data/Context/AppDbContext.cs`

### 📲 Mobile App (MAUI)

- Framework: .NET MAUI (.NET 10)
- Bản đồ: `Microsoft.Maui.Controls.Maps`
- Audio: `Plugin.Maui.Audio`
- Networking: `HttpClient`
- Luồng chính:
  1. Tải POI từ API.
  2. Hiển thị marker trên bản đồ.
  3. Theo dõi vị trí hiện tại.
  4. Khi vào vùng gần POI, phát audio tương ứng.

## 🧰 Yêu cầu môi trường

- .NET SDK 10.x
- SQL Server (Local hoặc remote)
- Workload .NET MAUI
- Android SDK/Emulator (nếu chạy Android)
- Visual Studio 2022 hoặc VS Code + MAUI toolchain

## 🔧 Cấu hình

### 1) 🗄️ API connection string

Cập nhật chuỗi kết nối trong:

- `food_market_narrator_api/appsettings.json`

Ví dụ:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=food_market_narrator;User Id=sa;Password=***;TrustServerCertificate=True;"
}
```

### 2) 🌐 API base URL cho MAUI

Hiện tại app MAUI gọi API qua địa chỉ Android emulator host:

- `http://10.0.2.2:5044/`

Cấu hình nằm trong:

- `food-market-narrator-maui/MauiProgram.cs`
- `food-market-narrator-maui/Services/POIService.cs`

> Nếu chạy trên Windows desktop app hoặc thiết bị thật, cần đổi sang địa chỉ server phù hợp (ví dụ `http://localhost:5044` hoặc LAN IP).

### 3) 🗝️ Google Maps API Key

Dự án Android đã khai báo key trong:

- `food-market-narrator-maui/Platforms/Android/AndroidManifest.xml`

Khuyến nghị:

- Key sử dụng ở đây chỉ là key hoạt động trên local và đã được hạn chế (restricted).
- Không hard-code key ở môi trường production.
- Chuyển sang cơ chế cấu hình bảo mật (build config/secret manager/CI variables).

## 🚀 Chạy dự án (local)

### Bước 1: ▶️ chạy backend API

```bash
cd food_market_narrator_api
dotnet restore
dotnet run
```

Mặc định API chạy ở:

- `http://localhost:5044`
- `https://localhost:7041`

### Bước 2: ▶️ chạy ứng dụng MAUI

```bash
cd food-market-narrator-maui
dotnet restore
dotnet build
```

Sau đó chạy target mong muốn (Android/iOS/Windows) từ IDE hoặc CLI.

Ví dụ Android emulator:

```bash
dotnet build -f net10.0-android
dotnet run -f net10.0-android
```

## 🔌 API hiện có

### GET `/api/restaurant`

Trả về danh sách nhà hàng/POI.

Một số trường dữ liệu chính:

- `restaurantId`
- `name`
- `description`
- `latitude`
- `longitude`
- `address`
- `isActive`
- `createdAt`

## 📚 Tài nguyên liên quan

- `maui-theory.md`
- `maui-ui-cheatsheet.md`

## 🛡️ Ghi chú phát triển

- Mã nguồn hiện có một số thông tin nhạy cảm ở dạng hard-code (ví dụ connection string, API key).
- Trước khi đưa lên môi trường dùng thật, nên:
  - Di chuyển secrets khỏi source code.
  - Thêm cấu hình theo môi trường (Development/Staging/Production).
  - Bật logging/monitoring phù hợp.

## 📄 License

Chưa khai báo. Thêm file `LICENSE` nếu bạn muốn công bố điều khoản sử dụng.

---

## Copyright

© 2026 **Nguyen Phuoc Sang** · **Nguyen Gia Thieu**  
All rights reserved.
