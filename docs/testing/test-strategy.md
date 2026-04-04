# Test Strategy

## Mục tiêu

- Đảm bảo API contract ổn định cho MAUI/Seller/Admin.
- Giảm regression ở luồng narration + geofence.
- Giữ chất lượng release với smoke test trước deploy.

## Tầng test

- Unit test: logic đơn lẻ.
- Integration test: Controller -> Service -> Repository -> DB.
- Manual E2E: MAUI navigation, geofence, audio playback.

## Bộ test hiện tại

### API

- Project: `test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj`
- Mục tiêu: kiểm thử endpoint auth/language/restaurant/dish/image/audio/users.
- Lệnh chạy:

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
```

### MAUI

- Project: `test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj`
- Mục tiêu: kiểm thử POI model, geofence logic, narration flow, history.
- Lệnh chạy:

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj
```

### Admin

- Tooling: Vitest + jsdom.
- Mục tiêu: API client logic cho auth/admin/analytics.
- Lệnh chạy:

```bash
cd admin
npm test
```

### Saler

- Tooling: Vitest + jsdom.
- Mục tiêu: API client mapping và endpoint contract cho saler.
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

## Tài liệu test case hiện có

- `testing/unit/maui-unit-test-cases.md`
- `testing/integration/api-integration-test-cases.md`
- `../test-guide.md`
