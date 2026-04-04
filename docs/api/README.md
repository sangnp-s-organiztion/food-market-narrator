# API Docs

Tai lieu tong quan cho FoodMarketNarrator.Api.

## Cong nghe

- ASP.NET Core Web API (.NET 10)
- EF Core SQL Server
- MongoDB cho logging va analytics
- Cookie Authentication + Role-based Authorization

## Base URLs local

- <http://localhost:5044>
- <https://localhost:7041>

## Layer architecture

- Controller -> Service -> Repository -> DbContext

Nguyen tac:

- Controller mong, tra response va validate co ban.
- Business logic o Service.
- Data access o Repository.

## Nhom endpoint chinh

### Auth

- POST /Auth/login
- POST /Auth/admin/login
- GET /Auth/me
- GET /Auth/admin/me
- POST /Auth/logout

### Public visitor data

- GET /Restaurant
- GET /Restaurant/{id}
- GET /Language
- GET /Language/{languageCode}
- GET /Restaurant/{restaurantId}/images
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
- PATCH /Audios/{audioId}/active
- DELETE /Audios/{audioId}

### Admin analytics va logs

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

## Chay local

```bash
cd FoodMarketNarrator.Api
dotnet restore
dotnet run
```

## Chay test

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
```

## Tai lieu lien quan

- ../architecture/overview.md
- ../architecture/api-architecture.md
- ../setup/local-development.md
- ../testing/test-strategy.md
- seller-required-endpoints.md
