# Test Strategy

## Mục tiêu

- Đảm bảo API contract ổn định cho MAUI/Seller/Admin.
- Giảm regression ở luồng narration + geofence.
- Giữ chất lượng release với smoke test trước deploy.

## Tầng test

- Unit test: logic đơn lẻ.
- Integration test: Controller -> Service -> Repository -> DB.
- Frontend unit/smoke test: API client behavior bằng Vitest.
- Manual E2E: MAUI navigation, geofence, audio playback.

## Bộ test hiện tại

Tóm tắt trạng thái hiện tại:

- API integration: 37 tests
- MAUI unit: 68 tests
- Admin Vitest: 4 tests
- Saler Vitest: 5 tests

### API

- Project: `test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj`
- Mục tiêu: kiểm thử endpoint auth/language/restaurant/dish/image/audio/users.
- Lệnh chạy:

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
```

### MAUI

- Project: `test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj`
- Mục tiêu: kiểm thử POI model, geofence logic, narration flow, history và QR deep link parsing.
- Lệnh chạy:

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj
```

### Admin

- Tooling: Vitest + jsdom.
- Mục tiêu: smoke test API client cho auth/analytics.
- Lệnh chạy:

```bash
cd admin
npm test
```

### Saler

- Tooling: Vitest + jsdom.
- Mục tiêu: smoke test API client mapping + endpoint contract cho saler.
- Lệnh chạy:

```bash
cd saler
npm test
```

## CI gate

CI hiện chạy 4 nhóm test:

- MAUI unit tests
- API integration tests
- Admin tests
- Saler tests

Pull request chỉ nên merge khi 4 nhóm đều pass.

## E2E status

- Cấu hình Playwright có tồn tại ở `admin/playwright.config.ts` và `saler/playwright.config.ts`.
- Hien tai chua co bo Playwright spec duoc commit trong repo, nen E2E tu dong chua la gate chinh.

## Tài liệu test case hiện có

- `docs/testing/unit/maui-unit-test-cases.md`
- `docs/testing/integration/api-integration-test-cases.md`
- `test-guide.md`
