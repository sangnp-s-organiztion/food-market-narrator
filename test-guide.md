# Test Guide

Tài liệu này hướng dẫn cách chạy test cho 4 phần chính của repo:

- API integration tests
- MAUI unit tests
- Admin frontend tests
- Saler frontend tests

Mục tiêu là có một file ngắn gọn nhưng đủ để chạy test local hoặc trong CI, đúng theo cấu trúc hiện tại của dự án.

## 1. Prerequisites

### 1.1 Bắt buộc

- .NET SDK 10.x
- Node.js 20.x hoặc mới hơn
- npm
- SQL Server nếu bạn muốn chạy API/app local ngoài integration test

### 1.2 Riêng cho MAUI

- MAUI workload đã cài
- Nếu build/run app MAUI thật, cần Android SDK hoặc Visual Studio có MAUI toolchain

### 1.3 Kiểm tra nhanh môi trường

```powershell
dotnet --version
node --version
npm --version
```

## 2. Cấu trúc test

```text
test/
└── maui-testing/
    ├── FoodMarketNarrator.Api.IntegrationTests/
    │   ├── IntegrationTests.csproj
    │   └── ApiIntegrationTests.cs
    └── FoodMarketNarrator.Maui.UnitTests/
        ├── unit-test.csproj
        ├── Models/
        └── Services/

admin/
└── src/test/

saler/
└── src/test/
```

## 3. Chạy toàn bộ test

Nếu muốn kiểm tra toàn bộ 4 phần một lần, chạy lần lượt các lệnh sau từ root repo:

```powershell
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj
cd admin; npm test
cd ..\saler; npm test
```

## 4. API Integration Tests

### 4.1 Mục đích

Kiểm thử API từ Controller -> Service -> Repository -> InMemory DB. Bộ test này không cần SQL Server thật vì dùng InMemory database.

### 4.2 Vị trí

- Project: [test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj](test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj)
- Test file: [test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/ApiIntegrationTests.cs](test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/ApiIntegrationTests.cs)

### 4.3 Chạy test

```powershell
cd test/maui-testing/FoodMarketNarrator.Api.IntegrationTests
dotnet test
```

Hoặc chạy từ root:

```powershell
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
```

### 4.4 Chạy một nhóm test cụ thể

```powershell
dotnet test --filter "FullyQualifiedName~Auth"
dotnet test --filter "FullyQualifiedName~Language"
dotnet test --filter "FullyQualifiedName~Restaurant"
dotnet test --filter "FullyQualifiedName~Audio"
dotnet test --filter "FullyQualifiedName~Dish"
dotnet test --filter "FullyQualifiedName~Images"
```

### 4.5 Các luồng được cover

- Login / Logout / Me
- Language public endpoints
- Restaurant public endpoints
- Public data cho dishes, images, audios
- Audio / Dish / Image quản trị
- User create / status / role

### 4.6 Lưu ý

- Test API đang seed dữ liệu bằng InMemory DB riêng cho từng run.
- Nếu thấy fail do endpoint đổi route, kiểm tra lại cả controller và test case tương ứng.

## 5. MAUI Unit Tests

### 5.1 Mục đích

Kiểm thử logic client phía MAUI, đặc biệt là:

- POI distance / nearest POI
- geofence transition
- narration flow
- history handling

### 5.2 Vị trí

- Project: [test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj](test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj)
- Test file chính:
  - [test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Models/POI_Model_Tests.cs](test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Models/POI_Model_Tests.cs)
  - [test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Services/HistoryService_Tests.cs](test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Services/HistoryService_Tests.cs)
  - [test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Services/POIService_Tests.cs](test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Services/POIService_Tests.cs)
  - [test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Services/NarrationFlowService_Tests.cs](test/maui-testing/FoodMarketNarrator.Maui.UnitTests/Services/NarrationFlowService_Tests.cs)

### 5.3 Chạy test

```powershell
cd test/maui-testing/FoodMarketNarrator.Maui.UnitTests
dotnet test
```

Hoặc từ root:

```powershell
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj
```

### 5.4 Chạy một nhóm test cụ thể

```powershell
dotnet test --filter "FullyQualifiedName~POI_Model_Tests"
dotnet test --filter "FullyQualifiedName~HistoryService_Tests"
dotnet test --filter "FullyQualifiedName~POIService_Tests"
dotnet test --filter "FullyQualifiedName~NarrationFlowService_Tests"
```

### 5.5 Lưu ý

- Test MAUI unit hiện có thể xuất warning xUnit1031 do một số test vẫn dùng `task.Wait(...)`.
- Warning này chưa làm fail suite, nhưng nếu muốn sạch hơn thì nên đổi sang async/await.

## 6. Admin Tests

### 6.1 Mục đích

Kiểm thử frontend admin bằng Vitest, chủ yếu tập trung vào API client và logic gọi API.

### 6.2 Vị trí

- Project: [admin/package.json](admin/package.json)
- Test file hiện tại: [admin/src/test/example.test.ts](admin/src/test/example.test.ts)

### 6.3 Chạy test

```powershell
cd admin
npm test
```

Hoặc:

```powershell
cd admin
npm run test
```

### 6.4 Chạy lint trước khi test

```powershell
cd admin
npm run lint
```

### 6.5 Test đang cover gì

- `analyticsApi.getMovementPaths("all")` phải gửi `sessionLimit=0`
- `authApi.login(...)` phải ném đúng thông báo đăng nhập không hợp lệ

### 6.6 Lưu ý

- Admin hiện dùng Vitest + jsdom.
- Test đang mock `fetch`, nên không cần backend chạy thật.

## 7. Saler Tests

### 7.1 Mục đích

Kiểm thử frontend saler bằng Vitest, tập trung vào API client và mapping dữ liệu.

### 7.2 Vị trí

- Project: [saler/package.json](saler/package.json)
- Test file hiện tại: [saler/src/test/example.test.ts](saler/src/test/example.test.ts)

### 7.3 Chạy test

```powershell
cd saler
npm test
```

Hoặc:

```powershell
cd saler
npm run test
```

### 7.4 Chạy lint trước khi test

```powershell
cd saler
npm run lint
```

### 7.5 Test đang cover gì

- Endpoint ảnh canonical: `/Restaurant/{restaurantId}/images`
- Mapping `role` từ response login
- Normalize đường dẫn ảnh tương đối thành URL tuyệt đối

### 7.6 Lưu ý

- Saler hiện cũng dùng Vitest + jsdom.
- Test đang mock `fetch`, nên không cần backend chạy thật.

## 8. CI / Local parity

Workflow CI hiện được thiết kế để chạy cùng bộ lệnh như local:

- API: `dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj`
- MAUI: `dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj`
- Admin: `cd admin; npm test`
- Saler: `cd saler; npm test`

Nếu test local pass nhưng CI fail, thường là do:

- khác version SDK / Node
- thiếu MAUI workload trên runner
- thay đổi route/API contract nhưng chưa update test

## 9. Troubleshooting

### 9.1 API test fail do route hoặc seed data

- Kiểm tra controller route hiện tại.
- Kiểm tra lại dữ liệu seed trong `ApiIntegrationTests.cs`.
- Đảm bảo test đang gọi đúng endpoint public/authorized.

### 9.2 MAUI test fail do compile

- Kiểm tra `unit-test.csproj` có link đầy đủ các file interface/model từ project MAUI chưa.
- Nếu thêm service mới trong MAUI, nhớ thêm file đó vào test project.

### 9.3 Admin/Saler test fail do alias hoặc setup

- Kiểm tra `vitest.config.ts` có alias `@` trỏ đúng `src`.
- Kiểm tra file `src/test/setup.ts` có `jest-dom` và `matchMedia`.

### 9.4 Muốn chạy một test cụ thể

```powershell
dotnet test --filter "FullyQualifiedName~TênTest"
npm test -- --testNamePattern "TênTest"
```

## 10. Ghi chú

- Đây là file hướng dẫn chạy test, không phải test case chi tiết.
- Nếu cần test case mức chi tiết hơn, xem thêm:
  - [docs/testing/test-strategy.md](docs/testing/test-strategy.md)
  - [docs/testing/integration/api-integration-test-cases.md](docs/testing/integration/api-integration-test-cases.md)
  - [docs/testing/unit/maui-unit-test-cases.md](docs/testing/unit/maui-unit-test-cases.md)
