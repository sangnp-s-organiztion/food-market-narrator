# Environment Variables

## Backend API

Thông tin quan trọng nằm trong:

- `FoodMarketNarrator.Api/appsettings.json`
- `FoodMarketNarrator.Api/appsettings.Development.json`

## SQL Server

Sử dụng `ConnectionStrings:DefaultConnection`.

## MongoDB

Thông số đang dùng:

- Host: `localhost:27017`
- Auth source: `admin`
- Username: `admin`
- Password: `root@1133`

Khuyến nghị:

- Không hardcode secret cho môi trường production.
- Đưa credential sang environment variables hoặc secret manager.
