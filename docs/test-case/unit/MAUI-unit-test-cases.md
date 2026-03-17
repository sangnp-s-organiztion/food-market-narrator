# Tài Liệu Kiểm Thử Đơn Vị (Unit Tests)

## Food Market Narrator - Unit Test Documentation

---

### 1. Tổng Quan

#### 1.1 Mục Đích

Tài liệu này mô tả chi tiết các test case kiểm thử đơn vị (Unit Tests) cho phần logic của ứng dụng **Food Market Narrator** (MAUI Mobile App). Các test case được thiết kế nhằm xác minh tính đúng đắn của các service và model phía client.

#### 1.2 Phạm Vi Kiểm Thử

| Phạm vi                | Mô tả                                          |
| ---------------------- | ---------------------------------------------- |
| **Tầng kiểm thử**      | Unit Test (kiểm thử đơn vị)                    |
| **Môi trường**         | .NET 10.0 (không cần thiết bị/giả lập)         |
| **Framework**          | xUnit + Moq + FluentAssertions                 |
| **Số lượng test case** | 64                                             |

#### 1.3 Các Module Được Kiểm Thử

```
MAUI Mobile App (Client)
├── Models
│   └── POI (Point of Interest)
├── Services
│   ├── POIService (Quản lý POI, tính khoảng cách, geofence)
│   ├── NarrationFlowService (Luồng thuyết minh tự động)
│   └── HistoryService (Lịch sử xem quán)
```

**Lưu ý:** Không kiểm thử các thành phần yêu cầu platform-specific:
- LocationService (cần GPS device)
- AudioService (cần audio player)
- LanguageService (cần Preferences API)

---

### 2. Môi Trường Kiểm Thử

#### 2.1 Cấu Trúc Thư Mục

```
test/
└── maui-testing/
    ├── FoodMarketNarrator.Maui.UnitTests/
    │   ├── unit-test.csproj                    # Project file
    │   ├── Models/
    │   │   └── POI_Model_Tests.cs              # Tests cho POI model
    │   └── Services/
    │       ├── HistoryService_Tests.cs          # Tests cho HistoryService
    │       ├── POIService_Tests.cs             # Tests cho POIService
    │       └── NarrationFlowService_Tests.cs   # Tests cho NarrationFlowService
    └── FoodMarketNarrator.Api.IntegrationTests/ # Integration tests cho API
```

#### 2.2 Dependencies

| Package                                | Phiên bản | Mục đích                  |
| ------------------------------------- | ---------- | ------------------------- |
| xunit                                  | 2.9.3      | Test framework            |
| Moq                                    | 4.20.72    | Mocking framework         |
| FluentAssertions                       | 8.9.0      | Assertions fluent         |
| Microsoft.NET.Test.Sdk                 | 17.14.1    | Test SDK                  |
| sqlite-net-pcl                         | 1.9.172    | SQLite attributes         |
| Microsoft.Maui.Essentials              | 10.0.40    | Location class            |
| Mapsui.Maui                           | 5.0.2      | Map types                 |

#### 2.3 Cách Chạy Tests

```bash
# Chạy tất cả tests
cd test/maui-testing/FoodMarketNarrator.Maui.UnitTests
dotnet test

# Chạy tests cho từng module
dotnet test --filter "FullyQualifiedName~POI_Model_Tests"
dotnet test --filter "FullyQualifiedName~HistoryService_Tests"
dotnet test --filter "FullyQualifiedName~POIService_Tests"
dotnet test --filter "FullyQualifiedName~NarrationFlowService_Tests"
```

---

### 3. Test Cases Chi Tiết

---

#### 3.1 POI Model Tests (`POI_Model_Tests`)

**Tổng số test:** 23

##### 3.1.1 Tests cho GetAudioUrl()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | GetAudioUrl_WithMatchingLanguage_ReturnsCorrectAudio | Kiểm tra lấy đúng audio khi ngôn ngữ khớp | ✅ Pass |
| 2 | GetAudioUrl_WithCaseInsensitiveLanguage_ReturnsCorrectAudio | Kiểm tra không phân biệt hoa thường | ✅ Pass |
| 3 | GetAudioUrl_WithNoMatchingLanguage_ReturnsFirstActiveAudio | Khi không có ngôn ngữ khớp, trả về audio đầu tiên | ✅ Pass |
| 4 | GetAudioUrl_WithNoActiveAudios_ReturnsNull | Không có audio active thì trả về null | ✅ Pass |
| 5 | GetAudioUrl_WithInactiveAudios_ReturnsNull | Audio inactive thì không được chọn | ✅ Pass |
| 6 | GetAudioUrl_WithMultipleVersions_ReturnsHighestVersion | Chọn version cao nhất | ✅ Pass |
| 7 | GetAudioUrl_WithEmptyAudioUrl_ReturnsEmptyString | Audio URL rỗng trả về chuỗi rỗng | ✅ Pass |

##### 3.1.2 Tests cho PrimaryImage

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | PrimaryImage_WithPrimaryImage_ReturnsPrimaryImage | Ưu tiên lấy ảnh primary | ✅ Pass |
| 2 | PrimaryImage_WithNoPrimaryImage_ReturnsFirstBySortOrder | Không có primary thì lấy theo SortOrder | ✅ Pass |
| 3 | PrimaryImage_WithNoImages_ReturnsDefaultImage | Không có ảnh thì trả về mặc định | ✅ Pass |
| 4 | PrimaryImage_WithPathPrefix_RemovesPrefix | Loại bỏ prefix đường dẫn | ✅ Pass |

##### 3.1.3 Tests cho Display Properties

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | StatusText_WhenActive_ReturnsOpenText | Trạng thái mở cửa | ✅ Pass |
| 2 | StatusText_WhenInactive_ReturnsClosedText | Trạng thái đóng cửa | ✅ Pass |
| 3 | OpeningHoursDisplay_WithValue_ReturnsValue | Giờ mở cửa có giá trị | ✅ Pass |
| 4 | OpeningHoursDisplay_WithoutValue_ReturnsDefault | Giờ mở cửa mặc định | ✅ Pass |
| 5 | AddressDisplay_WithValue_ReturnsValue | Địa chỉ có giá trị | ✅ Pass |
| 6 | AddressDisplay_WithoutValue_ReturnsDefault | Địa chỉ mặc định | ✅ Pass |
| 7 | AudioLanguagesDisplay_WithActiveAudios_ReturnsLanguageNames | Hiển thị danh sách ngôn ngữ | ✅ Pass |
| 8 | AudioLanguagesDisplay_WithoutLanguageName_ReturnsLanguageCode | Hiển thị mã ngôn ngữ | ✅ Pass |
| 9 | AudioLanguagesDisplay_WithNoActiveAudios_ReturnsDefaultText | Không có ngôn ngữ | ✅ Pass |
| 10 | AudioSummaryDisplay_WithActiveAudios_ReturnsCount | Đếm số audio active | ✅ Pass |
| 11 | AudioSummaryDisplay_WithNoActiveAudios_ReturnsDefaultText | Không có audio | ✅ Pass |
| 12 | CoordinatesDisplay_ReturnsFormattedCoordinates | Định dạng tọa độ | ✅ Pass |

---

#### 3.2 HistoryService Tests (`HistoryService_Tests`)

**Tổng số test:** 16

##### 3.2.1 Tests cho AddToHistory()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | AddToHistory_NewItem_AddsToBeginning | Thêm mới vào đầu danh sách | ✅ Pass |
| 2 | AddToHistory_MultipleItems_AddsInOrder | Thêm nhiều items theo thứ tự | ✅ Pass |
| 3 | AddToHistory_ExistingItem_MovesToBeginning | Item đã tồn tại chuyển lên đầu | ✅ Pass |
| 4 | AddToHistory_EmptyString_DoesNothing | Chuỗi rỗng không thêm | ✅ Pass |
| 5 | AddToHistory_NullString_DoesNothing | Null không thêm | ✅ Pass |
| 6 | AddToHistory_WhitespaceString_DoesNothing | Khoảng trắng không thêm | ✅ Pass |

##### 3.2.2 Tests cho GetHistory()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | GetHistory_Empty_ReturnsEmptyList | Danh sách trống | ✅ Pass |
| 2 | GetHistory_ReturnsCopy_NotReference | Trả về bản sao, không reference | ✅ Pass |

##### 3.2.3 Tests cho RemoveFromHistory()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | RemoveFromHistory_ExistingItem_RemovesItem | Xóa item tồn tại | ✅ Pass |
| 2 | RemoveFromHistory_NonExistingItem_DoesNothing | Xóa item không tồn tại | ✅ Pass |

##### 3.2.4 Tests cho ClearHistory()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | ClearHistory_WithItems_ClearsAll | Xóa tất cả | ✅ Pass |
| 2 | ClearHistory_Empty_DoesNothing | Danh sách trống | ✅ Pass |

##### 3.2.5 Tests cho IsInHistory()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | IsInHistory_ExistingItem_ReturnsTrue | Item tồn tại | ✅ Pass |
| 2 | IsInHistory_NonExistingItem_ReturnsFalse | Item không tồn tại | ✅ Pass |
| 3 | IsInHistory_AfterRemove_ReturnsFalse | Sau khi xóa | ✅ Pass |

##### 3.2.6 Tests cho MaxHistoryLimit

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | AddToHistory_ExceedsMaxLimit_RemovesOldest | Giới hạn 50 items | ✅ Pass |

---

#### 3.3 POIService Tests (`POIService_Tests`)

**Tổng số test:** 13

##### 3.3.1 Tests cho GetDistanceMeters()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | GetDistanceMeters_SameLocation_ReturnsZero | Cùng vị trí = 0m | ✅ Pass |
| 2 | GetDistanceMeters_DifferentLocations_ReturnsDistance | Vị trí khác tính đúng khoảng cách | ✅ Pass |

##### 3.3.2 Tests cho GetNearestPOI()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | GetNearestPOI_EmptyList_ReturnsNull | Danh sách rỗng | ✅ Pass |
| 2 | GetNearestPOI_NullList_ReturnsNull | Null | ✅ Pass |
| 3 | GetNearestPOI_SinglePOI_ReturnsThatPOI | Một POI duy nhất | ✅ Pass |
| 4 | GetNearestPOI_MultiplePOIs_ReturnsNearest | Nhiều POI - trả về gần nhất | ✅ Pass |
| 5 | GetNearestPOI_WithCoordinates_ReturnsNearest | Dùng tọa độ trực tiếp | ✅ Pass |

##### 3.3.3 Tests cho UpdateNearestPOI() - Geofence Logic

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | UpdateNearestPOI_FirstEnter_ReturnsPOI | Lần đầu vào vùng (30m) | ✅ Pass |
| 2 | UpdateNearestPOI_OutsideRadius_ReturnsNull | Ngoài vùng (30m) | ✅ Pass |
| 3 | UpdateNearestPOI_EnterThenStay_ReturnsNull | Vào rồi đứng yên | ✅ Pass |
| 4 | UpdateNearestPOI_EnterThenExit_ReturnsNull | Vào rồi ra (40m) | ✅ Pass |
| 5 | UpdateNearestPOI_EmptyPOIs_ReturnsNull | Danh sách rỗng | ✅ Pass |
| 6 | UpdateNearestPOI_NullPOIs_ReturnsNull | Null | ✅ Pass |

---

#### 3.4 NarrationFlowService Tests (`NarrationFlowService_Tests`)

**Tổng số test:** 12

##### 3.4.1 Tests cho StartNarration()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | StartNarration_FirstTime_SetsNarrationEnabled | Bắt đầu thuyết minh | ✅ Pass |
| 2 | StartNarration_AlreadyEnabled_DoesNotStartAgain | Đã bắt đầu rồi | ✅ Pass |

##### 3.4.2 Tests cho StopNarration()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | StopNarration_WhileEnabled_DisablesNarration | Tắt thuyết minh | ✅ Pass |
| 2 | StopNarration_WhileEnabled_StopsAudio | Dừng audio | ✅ Pass |
| 3 | StopNarration_WhileDisabled_DoesNothing | Chưa bắt đầu mà tắt | ✅ Pass |

##### 3.4.3 Tests cho CheckAndNarrateAsync()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | CheckAndNarrateAsync_WhenAudioPlaying_DoesNotTrigger | Đang phát audio thì không trigger | ✅ Pass |
| 2 | CheckAndNarrateAsync_WithNoPOIs_DoesNotPlay | Không có POI | ✅ Pass |
| 3 | CheckAndNarrateAsync_WithNoLocation_DoesNotPlay | Không có location | ✅ Pass |
| 4 | CheckAndNarrateAsync_NoAudioAvailable_DoesNotPlay | Không có audio | ✅ Pass |
| 5 | CheckAndNarrateAsync_OutsideTriggerDistance_DoesNotPlay | Ngoài khoảng cách trigger (30m) | ✅ Pass |
| 6 | CheckAndNarrateAsync_UsesCurrentLanguageFromService | Sử dụng ngôn ngữ hiện tại | ✅ Pass |

##### 3.4.4 Tests cho ResetPlayedPOIs()

| STT | Test Case | Mô tả | Kết quả |
|-----|-----------|-------|---------|
| 1 | ResetPlayedPOIs_ResetsInternalState | Reset trạng thái đã phát | ✅ Pass |

---

### 4. Tổng Kết

#### 4.1 Thống Kê

| Module                   | Số Test Cases | Status |
| ------------------------ |-------------- |--------|
| POI Model               | 23            | ✅ Pass |
| HistoryService          | 16            | ✅ Pass |
| POIService              | 13            | ✅ Pass |
| NarrationFlowService    | 12            | ✅ Pass |
| **Tổng cộng**          | **64**        | **✅ Pass** |

#### 4.2 Coverage Areas

| Chức năng              | Đã Cover | Chưa Cover |
| ---------------------- |---------- |-----------|
| POI Model logic        | ✅        |            |
| Audio selection         | ✅        |            |
| Distance calculation    | ✅        |            |
| Geofence transition    | ✅        |            |
| History management      | ✅        |            |
| Narration flow          | ✅        |            |
| Location tracking       | ❌        | Device-specific |
| Audio playback          | ❌        | Device-specific |
| Language switching      | ❌        | Platform-specific |

#### 4.3 Hạn Chế

1. **Không test được các thành phần platform-specific:**
   - LocationService cần GPS device thực
   - AudioService cần audio player
   - LanguageService cần Preferences API

2. **Không test được UI:**
   - Views và XAML
   - Navigation flows

3. **Không test được network/cache:**
   - API calls (đã có Integration Test)
   - Offline caching

#### 4.4 Khuyến Nghị

1. **Tăng coverage:** Thêm tests cho FavoriteService nếu cần
2. **Integration Tests:** Đã có sẵn cho API ở `test/Integration`
3. **UI Tests:** Cân nhắc thêm UI testing framework (nếu cần)

---

### 5. Thông Tin Bổ Sung

- **Ngày tạo:** 2026-03-17
- **Người tạo:** Claude Sonnet 4.6
- **Framework:** .NET 10.0 + xUnit
- **Source:** `test/maui-testing/FoodMarketNarrator.Maui.UnitTests/`

---

*Document version: 1.0*
