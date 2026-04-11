# Tài liệu kiểm thử đơn vị MAUI (snapshot hiện tại)

## 1) Tổng quan

Tài liệu này phản ánh đúng bộ MAUI unit tests đang có trong repo tại thời điểm hiện tại.

- Loại test: Unit test
- Framework: xUnit + Moq + FluentAssertions
- Project: `test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj`
- Tổng số test hiện có: 68

## 2) Cấu trúc test

```text
test/maui-testing/FoodMarketNarrator.Maui.UnitTests/
├── unit-test.csproj
├── Models/
│   └── POI_Model_Tests.cs
└── Services/
    ├── HistoryService_Tests.cs
    ├── POIService_Tests.cs
    ├── NarrationFlowService_Tests.cs
    └── QrAccessService_Tests.cs
```

## 3) Phạm vi cover hiện tại

### 3.1 Models

- `POI_Model_Tests.cs`: 24 tests
- Cover chính:
  - audio selection theo language/active/version
  - display properties (status/opening/address/coordinates)
  - ảnh chính (primary image)

### 3.2 Services

- `HistoryService_Tests.cs`: 16 tests
  - add/remove/clear/isInHistory
  - max items

- `POIService_Tests.cs`: 14 tests
  - tính khoảng cách
  - nearest POI
  - geofence enter/exit/switch cơ bản

- `NarrationFlowService_Tests.cs`: 12 tests
  - start/stop narration
  - điều kiện trigger/bỏ qua
  - trạng thái phát trong phiên

- `QrAccessService_Tests.cs`: 2 tests (Theory)
  - deep link hợp lệ với scheme/host đúng
  - deep link không hợp lệ không làm crash

## 4) Cách chạy

Từ root repo:

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj
```

Hoặc từ thư mục project:

```bash
cd test/maui-testing/FoodMarketNarrator.Maui.UnitTests
dotnet test
```

Chạy theo nhóm:

```bash
dotnet test --filter "FullyQualifiedName~POI_Model_Tests"
dotnet test --filter "FullyQualifiedName~HistoryService_Tests"
dotnet test --filter "FullyQualifiedName~POIService_Tests"
dotnet test --filter "FullyQualifiedName~NarrationFlowService_Tests"
dotnet test --filter "FullyQualifiedName~QrAccessService_Tests"
```

## 5) Chưa cover / giới hạn

- Chưa có UI test cho XAML/views/navigation.
- Chưa có test trực tiếp cho platform-specific runtime:
  - LocationService (GPS/device permission runtime)
  - AudioService playback thực tế
- Chưa có bộ test hiệu năng cho narration/map rendering.

## 6) Ghi chú bảo trì

- Khi thêm test mới, cập nhật lại tổng số test và danh sách module trong file này.
- Khi đổi đường dẫn test project, cập nhật đồng thời:
  - `docs/testing/README.md`
  - `docs/testing/test-strategy.md`
  - `test-guide.md`
