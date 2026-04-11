# Tài liệu kiểm thử tích hợp API (snapshot hiện tại)

## 1) Tổng quan

Tài liệu này phản ánh đúng bộ API integration tests đang có trong repo tại thời điểm hiện tại.

- Loại test: Integration test
- Framework: xUnit + `Microsoft.AspNetCore.Mvc.Testing` + EF Core InMemory
- Project: `test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj`
- Test file: `test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/ApiIntegrationTests.cs`
- Tổng số test hiện có: 37

## 2) Môi trường test

- Database: InMemory DB, seed dữ liệu trong `ApiIntegrationTests`.
- Chạy độc lập không cần SQL Server thật.
- Test thông qua `WebApplicationFactory<Program>`.

## 3) Nhóm test đang cover

- Auth tests
  - login hợp lệ/không hợp lệ
  - me có/không cookie
  - logout
  - legacy password migration

- Users tests
  - create user (default password)
  - validate phone/email
  - khóa tài khoản admin hiện tại

- Language tests
  - GET all languages
  - GET by code

- Restaurant public tests
  - GET all restaurants
  - GET by id

- Public data tests
  - GET public dishes
  - GET public images
  - GET public audios

- Restaurant authorized tests
  - kiểm tra unauthorized với endpoint protected
  - update status
  - update thông tin nhà hàng

- Audio tests
  - list audio
  - list by restaurant
  - update active
  - delete

- Dishes tests
  - list dishes
  - create/update/delete

- Images tests
  - list images
  - set primary
  - delete
  - reorder

## 4) Cách chạy

Từ root repo:

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
```

Hoặc từ thư mục project:

```bash
cd test/maui-testing/FoodMarketNarrator.Api.IntegrationTests
dotnet test
```

Lệnh filter gợi ý:

```bash
dotnet test --filter "FullyQualifiedName~Auth"
dotnet test --filter "FullyQualifiedName~Users"
dotnet test --filter "FullyQualifiedName~Language"
dotnet test --filter "FullyQualifiedName~Restaurant"
dotnet test --filter "FullyQualifiedName~Audio"
dotnet test --filter "FullyQualifiedName~Dish"
dotnet test --filter "FullyQualifiedName~Images"
```

## 5) Lưu ý quan trọng

- Đây là tài liệu snapshot theo test hiện có, không phải ma trận test đầy đủ cho mọi endpoint trong backend.
- Khi đổi route/contract API, cần cập nhật đồng thời test code và tài liệu này.
- Nếu thêm file integration test mới, cần cập nhật lại tổng số test trong file.
