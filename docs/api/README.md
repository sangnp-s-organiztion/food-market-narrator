# API Docs

Tài liệu tổng quan cho FoodMarketNarrator.Api.

## Công nghệ

- ASP.NET Core Web API (.NET 10)
- EF Core SQL Server
- MongoDB cho logging và analytics
- Cookie Authentication + Role-based Authorization

## Base URLs local

- <http://localhost:5044>
- <https://localhost:7041>

## Layer architecture

- Controller -> Service -> Repository -> DbContext

Nguyên tắc:

- Controller mỏng, trả response và validate cơ bản.
- Business logic ở Service.
- Data access ở Repository.

## Nhóm endpoint chính

### Auth

- POST /Auth/login
- POST /Auth/admin/login
- GET /Auth/me
- GET /Auth/admin/me
- POST /Auth/logout
- POST /Auth/admin/logout

### Public visitor data

- GET /Restaurant
- GET /Restaurant/{id}
- GET /Language
- GET /Language/{languageCode}
- GET /Restaurant/{restaurantId}/images
- GET /Restaurant/{restaurantId}/dishes
- GET /Restaurant/{restaurantId}/audios
- GET /public/Restaurant/{restaurantId}/dishes
- GET /public/Restaurant/{restaurantId}/audios

### Seller/Admin management

- PATCH /Restaurant/{id}
- PATCH /Restaurant/{id}/status
- POST /Restaurant/{restaurantId}/dishes
- PUT /Dishes/{dishId}
- DELETE /Dishes/{dishId}
- POST /Restaurant/{restaurantId}/images
- DELETE /Images/{imageId}
- PATCH /Images/{imageId}/primary
- PATCH /Restaurant/{restaurantId}/images/reorder
- POST /Restaurant/{restaurantId}/audios
- POST /Restaurant/{restaurantId}/translate
- POST /Restaurant/{restaurantId}/audios/from-text
- PATCH /Audios/{audioId}/active
- DELETE /Audios/{audioId}

### Tour management (admin)

- GET /Tour
- GET /Tour/{id}
- POST /Tour
- PATCH /Tour/{id}
- POST /Tour/{id}/restaurants
- DELETE /Tour/{id}/restaurants/{restaurantId}
- PUT /Tour/{id}/stops/order
- POST /Tour/upload-image
- POST /Tour/{id}/upload-image

Lưu ý Dish payload hiện tại:

- Không còn field `description` trong bảng `Dish` và DTO/API.
- Body create/update dish dùng các field: `name`, `price`, `imageId`.

### Admin analytics và logs

- GET /api/analytics/kpis
- GET /api/analytics/heatmap
- GET /api/analytics/top-audios
- GET /api/analytics/top-restaurants
- GET /api/analytics/movement-paths
- GET /api/analytics/recent-activity
- GET /api/analytics/audio-stats
- GET /api/audit-logs
- GET /api/admin/stats/restaurants/count
- GET /api/admin/stats/audios/count
- GET /api/admin/stats/users/count
- GET /api/admin/stats/dishes/count

### User management

- GET /api/users
- GET /api/users/{id}
- POST /api/users
- PATCH /api/users/{id}/role
- PATCH /api/users/{id}/status

## Chạy local

```bash
cd FoodMarketNarrator.Api
dotnet restore
dotnet run
```

## Chạy test

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
```

## Tài liệu liên quan

- ../architecture/overview.md
- ../architecture/api-architecture.md
- ../setup/local-development.md
- ../testing/test-strategy.md
- seller-required-endpoints.md

## Ghi chú nghiệp vụ mới

- Với endpoint `PATCH /Audios/{audioId}/active`: khi bật active cho một audio, backend sẽ tự tắt các audio active khác cùng `restaurantId` và `languageId`.
