# Testing Docs

Tong hop tai lieu test cho toan bo workspace.

## Test suites

- API integration tests
- MAUI unit tests
- Admin frontend tests
- Saler frontend tests

## Chay nhanh

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Api.IntegrationTests/IntegrationTests.csproj
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj

cd admin && npm test
cd ../saler && npm test
```

## Tai lieu chi tiet

- test-strategy.md
- integration/api-integration-test-cases.md
- unit/maui-unit-test-cases.md
- ../../test-guide.md
