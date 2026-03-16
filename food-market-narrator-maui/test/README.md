# Test Project for Food Market Narrator MAUI

This directory contains unit tests and integration tests for the Food Market Narrator MAUI application.

## Project Structure

```
test/
├── UnitTests/
│   ├── Models/
│   │   ├── POIModelTests.cs           # Tests for POI model (21 tests)
│   │   ├── AudioModelTests.cs         # Tests for Audio model (2 tests)
│   │   ├── LanguageModelTests.cs      # Tests for Language model (2 tests)
│   │   └── DishModelTests.cs          # Tests for Dish model (4 tests)
│   ├── Services/
│   │   ├── POIServiceTests.cs         # Tests for POI service (15 tests)
│   │   ├── NarrationFlowServiceTests.cs # Tests for narration flow (18 tests)
│   │   ├── LocationServiceTests.cs     # Tests for location service (6 tests)
│   │   ├── AudioServiceTests.cs       # Tests for audio service (15 tests)
│   │   ├── HistoryServiceTests.cs     # Tests for history service (17 tests)
│   │   ├── FavoriteServiceTests.cs   # Tests for favorite service (14 tests)
│   │   └── LanguageServiceTests.cs   # Tests for language service (7 tests)
│   └── Settings/
│       └── AppSettingsTests.cs        # Tests for app settings (9 tests)
├── IntegrationTests/
│   ├── NarrationFlowIntegrationTests.cs  # Integration tests (7 tests)
│   └── POIIntegrationTests.cs         # Integration tests (10 tests)
└── food-market-narrator.Tests.csproj
```

## Running Tests

```bash
cd food-market-narrator-maui/test
dotnet test
```

## Test Coverage Summary

### Services (Coverage)

| Service | Test File | Test Count | Status |
|---------|-----------|------------|--------|
| POIService | `POIServiceTests.cs` | 15 | ✅ |
| NarrationFlowService | `NarrationFlowServiceTests.cs` | 18 | ✅ |
| LocationService | `LocationServiceTests.cs` | 6 | ✅ |
| AudioService | `AudioServiceTests.cs` | 15 | ✅ |
| HistoryService | `HistoryServiceTests.cs` | 17 | ✅ |
| FavoriteService | `FavoriteServiceTests.cs` | 14 | ✅ |
| LanguageService | `LanguageServiceTests.cs` | 7 | ✅ |

### Models (Coverage)

| Model | Test File | Test Count | Status |
|-------|-----------|------------|--------|
| POI | `POIModelTests.cs` | 21 | ✅ |
| AudioModel | `AudioModelTests.cs` | 2 | ✅ |
| LanguageModel | `LanguageModelTests.cs` | 2 | ✅ |
| DishModel | `DishModelTests.cs` | 4 | ✅ |

### Settings (Coverage)

| Setting | Test File | Test Count | Status |
|---------|-----------|------------|--------|
| AppSettings | `AppSettingsTests.cs` | 9 | ✅ |

### Integration Tests

| Integration | Test File | Test Count | Status |
|-------------|-----------|------------|--------|
| Narration Flow | `NarrationFlowIntegrationTests.cs` | 7 | ✅ |
| POI Service | `POIIntegrationTests.cs` | 10 | ✅ |

## Feature Coverage by Visitor Feature Document

| Feature | Description | Test Coverage |
|---------|-------------|---------------|
| 1. Theo dõi vị trí | GPS location tracking, background mode | ✅ `LocationServiceTests`, `NarrationFlowServiceTests` |
| 2. Hiển thị bản đồ | Map display, POI markers | ✅ `POIModelTests`, `POIServiceTests` |
| 3. Thuyết minh tự động | Geofence trigger, enter/exit detection | ✅ `NarrationFlowServiceTests`, `POIServiceTests` |
| 4. Thuyết minh audio | Audio playback, TTS, queue management | ✅ `AudioServiceTests`, `NarrationFlowServiceTests` |
| 5. Kích hoạt mã QR | QR code scanning, force trigger | ✅ `NarrationFlowIntegrationTests` |
| 6. Quyền riêng tư | Anonymous data, no personal info | ✅ `POIModelTests`, `HistoryServiceTests` |

## Additional Features (Not in Visitor Document)

| Feature | Description | Test Coverage |
|---------|-------------|---------------|
| Yêu thích | Favorite restaurants management | ✅ `FavoriteServiceTests` |
| Ngôn ngữ | Multi-language support | ✅ `LanguageServiceTests`, `LanguageModelTests` |
| Món ăn | Dish model | ✅ `DishModelTests` |
| Cấu hình | App settings | ✅ `AppSettingsTests` |

## Test Framework

- **xUnit** - Testing framework
- **Moq** - Mocking framework for dependencies
- **Microsoft.NET.Test.Sdk** - Test SDK

## Notes

- Tests use dependency injection and mocking to isolate units under test
- Integration tests verify interactions between multiple services
- All tests follow the naming convention: `MethodName_Scenario_ExpectedResult`
- Total: **147 test cases** covering all MAUI features
