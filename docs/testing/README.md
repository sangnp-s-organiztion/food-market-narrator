# Testing Docs

Tong hop tai lieu test cho toan bo workspace (trang thai hien tai).

## Test suites hien co

- API integration tests (xUnit): 37 tests
- MAUI unit tests (xUnit): 68 tests
- Admin frontend tests (Vitest): 4 tests
- Saler frontend tests (Vitest): 5 tests

## Chay nhanh

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj

cd admin
npm test

cd ../saler
npm test
```

## Luu y

- Ca admin va saler deu co cau hinh Playwright, nhung hien chua co bo test e2e Playwright commit trong repo.
- CI dang chay 4 nhom test tren workflow: `.github/workflows/ci.yml`.

## Tai lieu chi tiet

- [test-strategy.md](test-strategy.md)
- [integration/api-integration-test-cases.md](integration/api-integration-test-cases.md)
- [unit/maui-unit-test-cases.md](unit/maui-unit-test-cases.md)
- [../../test-guide.md](../../test-guide.md)
