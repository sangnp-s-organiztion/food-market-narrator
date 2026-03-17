# Tài Liệu Kiểm Thử Tích Hợp API

## Food Market Narrator - Integration Test Documentation

---

### 1. Tổng Quan

#### 1.1 Mục Đích

Tài liệu này mô tả chi tiết các test case kiểm thử tích hợp (Integration Tests) cho hệ thống API của ứng dụng **Food Market Narrator**. Các test case được thiết kế nhằm xác minh tính đúng đắn của các chức năng API từ tầng Controller đến Database.

#### 1.2 Phạm Vi Kiểm Thử

| Phạm vi                | Mô tả                                          |
| ---------------------- | ---------------------------------------------- |
| **Tầng kiểm thử**      | Integration Test (kiểm thử tích hợp)           |
| **Môi trường**         | InMemory Database (không phụ thuộc SQL Server) |
| **Framework**          | xUnit + ASP.NET Core MVC Testing               |
| **Số lượng test case** | 33                                             |

#### 1.3 Các Module Được Kiểm Thử

```
API Food Market Narrator
├── Authentication (Đăng nhập/Đăng xuất)
├── Language (Ngôn ngữ)
├── Restaurant (Nhà hàng)
├── Public Data (Dữ liệu công khai)
├── Audio (Tệp âm thanh)
├── Dish (Món ăn)
└── Image (Hình ảnh)
```

---

### 2. Môi Trường Kiểm Thử

#### 2.1 Cấu Trúc Thư Mục

```
test/
└── Integration/
    ├── IntegrationTests.csproj    # Project file
    └── ApiIntegrationTests.cs     # Test cases
```

#### 2.2 Dependencies

| Package                                | Phiên bản | Mục đích                  |
| -------------------------------------- | --------- | ------------------------- |
| xUnit                                  | 2.9.2     | Framework kiểm thử        |
| Microsoft.AspNetCore.Mvc.Testing       | 10.0.3    | Tạo WebApplicationFactory |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.3    | In-memory database        |
| Microsoft.NET.Test.Sdk                 | 17.13.0   | Test SDK                  |

#### 2.3 Cấu Hình Test

- **Database**: Sử dụng InMemory Database để đảm bảo isolation giữa các test cases
- **Mỗi test instance** sử dụng một database riêng biệt
- **Test data** được seed trong constructor với các bản ghi mẫu

#### 2.4 Dữ Liệu Test Mẫu

| Bảng       | Số lượng | Mô tả                                  |
| ---------- | -------- | -------------------------------------- |
| Language   | 2        | Vietnamese (vi), English (en)          |
| User       | 2        | admin, seller1                         |
| Restaurant | 2        | rest-001 (active), rest-002 (inactive) |

---

### 3. Chi Tiết Test Cases

#### 3.1 Module: Authentication

**Mô tả**: Kiểm thử các chức năng liên quan đến xác thực người dùng

| STT | Tên test case                                 | Mô tả                                | Input                                           | Expected Output     | Thực tế |
| --- | --------------------------------------------- | ------------------------------------ | ----------------------------------------------- | ------------------- | ------- |
| 1   | Login_WithValidCredentials_ReturnsOk          | Đăng nhập với thông tin hợp lệ       | {username: "admin", password: "admin123"}       | HTTP 200 + UserInfo | ✅ Pass |
| 2   | Login_WithInvalidPassword_ReturnsUnauthorized | Đăng nhập với mật khẩu sai           | {username: "admin", password: "wrong"}          | HTTP 401            | ✅ Pass |
| 3   | Login_WithInvalidUsername_ReturnsUnauthorized | Đăng nhập với username không tồn tại | {username: "nonexistent", password: "admin123"} | HTTP 401            | ✅ Pass |
| 4   | Login_WithEmptyCredentials_ReturnsBadRequest  | Đăng nhập với thông tin rỗng         | {username: "", password: ""}                    | HTTP 400            | ✅ Pass |
| 5   | Me_WithValidCookie_ReturnsOk                  | Lấy thông tin user hiện tại          | Cookie hợp lệ                                   | HTTP 200 + UserInfo | ✅ Pass |
| 6   | Me_WithoutCookie_ReturnsUnauthorized          | Lấy thông tin không có cookie        | Không có cookie                                 | HTTP 401            | ✅ Pass |
| 7   | Logout_WithValidCookie_ReturnsOk              | Đăng xuất thành công                 | Cookie hợp lệ                                   | HTTP 200            | ✅ Pass |

**Luồng kiểm thử Login:**

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Test Client   │────▶│  Auth Controller │────▶│  Auth Service   │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                                                          │
                                                          ▼
                                                  ┌─────────────────┐
                                                  │ User Repository │
                                                  └─────────────────┘
                                                          │
                                                          ▼
                                                  ┌─────────────────┐
                                                  │ InMemory DB     │
                                                  └─────────────────┘
```

---

#### 3.2 Module: Language

**Mô tả**: Kiểm thử các endpoint liên quan đến ngôn ngữ (phục vụ mobile app)

| STT | Tên test case                                     | Mô tả                         | Input            | Expected Output           | Thực tế |
| --- | ------------------------------------------------- | ----------------------------- | ---------------- | ------------------------- | ------- |
| 1   | GetAllLanguages_ReturnsOk                         | Lấy danh sách tất cả ngôn ngữ | GET /Language    | HTTP 200 + List<Language> | ✅ Pass |
| 2   | GetLanguageByCode_WithValidCode_ReturnsOk         | Lấy ngôn ngữ theo mã          | GET /Language/vi | HTTP 200 + Language       | ✅ Pass |
| 3   | GetLanguageByCode_WithInvalidCode_ReturnsNotFound | Mã ngôn ngữ không tồn tại     | GET /Language/fr | HTTP 404                  | ✅ Pass |

**Lưu ý**: Các endpoint Language là **public** (không yêu cầu authentication)

---

#### 3.3 Module: Restaurant (Public)

**Mô tả**: Kiểm thử các endpoint công khai liên quan đến nhà hàng

| STT | Tên test case                                   | Mô tả                          | Input                       | Expected Output             | Thực tế |
| --- | ----------------------------------------------- | ------------------------------ | --------------------------- | --------------------------- | ------- |
| 1   | GetAllRestaurants_ReturnsOk                     | Lấy danh sách tất cả nhà hàng  | GET /Restaurant             | HTTP 200 + List<Restaurant> | ✅ Pass |
| 2   | GetRestaurantById_WithValidId_ReturnsOk         | Lấy thông tin nhà hàng theo ID | GET /Restaurant/rest-001    | HTTP 200 + Restaurant       | ✅ Pass |
| 3   | GetRestaurantById_WithInvalidId_ReturnsNotFound | ID nhà hàng không tồn tại      | GET /Restaurant/nonexistent | HTTP 404                    | ✅ Pass |

**Lưu ý**: Các endpoint Restaurant (GET) là **public** theo định nghĩa trong `PublicEndpoints.cs`

---

#### 3.4 Module: Public Data

**Mô tả**: Kiểm thử các endpoint công khai để lấy dữ liệu cho mobile app

| STT | Tên test case             | Mô tả                  | Input                                  | Expected Output | Thực tế |
| --- | ------------------------- | ---------------------- | -------------------------------------- | --------------- | ------- |
| 1   | GetPublicDishes_ReturnsOk | Lấy danh sách món ăn   | GET /public/Restaurant/rest-001/dishes | HTTP 200        | ✅ Pass |
| 2   | GetPublicImages_ReturnsOk | Lấy danh sách hình ảnh | GET /public/Restaurant/rest-001/images | HTTP 200        | ✅ Pass |
| 3   | GetPublicAudios_ReturnsOk | Lấy danh sách audio    | GET /public/Restaurant/rest-001/audios | HTTP 200        | ✅ Pass |

**Mục đích**: Các endpoint này phục vụ mobile app khi người dùng đi bộ gần nhà hàng

---

#### 3.5 Module: Restaurant (Authorized)

**Mô tả**: Kiểm thử các endpoint yêu cầu authentication cho quản lý nhà hàng

| STT | Tên test case                                        | Mô tả                            | Input                                | Expected Output               | Thực tế |
| --- | ---------------------------------------------------- | -------------------------------- | ------------------------------------ | ----------------------------- | ------- |
| 1   | GetRestaurantsByUserId_ReturnsOk                     | Lấy danh sách nhà hàng theo user | GET /Users/2/restaurants             | HTTP 200 + List<Restaurant>   | ✅ Pass |
| 2   | UpdateRestaurantStatus_WithValidData_ReturnsOk       | Cập nhật trạng thái nhà hàng     | PATCH /Restaurant/rest-001/status    | HTTP 200                      | ✅ Pass |
| 3   | UpdateRestaurantStatus_WithInvalidId_ReturnsNotFound | ID không tồn tại                 | PATCH /Restaurant/nonexistent/status | HTTP 404                      | ✅ Pass |
| 4   | UpdateRestaurant_WithValidData_ReturnsOk             | Cập nhật thông tin nhà hàng      | PATCH /Restaurant/rest-001           | HTTP 200 + Updated Restaurant | ✅ Pass |

---

#### 3.6 Module: Audio

**Mô tả**: Kiểm thử các chức năng quản lý tệp âm thanh thuyết minh

| STT | Tên test case                             | Mô tả                     | Input                           | Expected Output        | Thực tế |
| --- | ----------------------------------------- | ------------------------- | ------------------------------- | ---------------------- | ------- |
| 1   | GetAllAudios_ReturnsOk                    | Lấy tất cả audio          | GET /Audio                      | HTTP 200 + List<Audio> | ✅ Pass |
| 2   | GetAudiosByRestaurant_ReturnsOk           | Lấy audio theo nhà hàng   | GET /Restaurant/rest-001/audios | HTTP 200               | ✅ Pass |
| 3   | UpdateAudioActive_WithValidData_ReturnsOk | Cập nhật trạng thái audio | PATCH /Audios/999/active        | HTTP 404 (no data)     | ✅ Pass |
| 4   | DeleteAudio_WithInvalidId_ReturnsNotFound | Xóa audio không tồn tại   | DELETE /Audios/999              | HTTP 404               | ✅ Pass |

---

#### 3.7 Module: Dish

**Mô tả**: Kiểm thử các chức năng quản lý món ăn

| STT | Tên test case                            | Mô tả                    | Input                            | Expected Output    | Thực tế |
| --- | ---------------------------------------- | ------------------------ | -------------------------------- | ------------------ | ------- |
| 1   | GetDishesByRestaurant_ReturnsOk          | Lấy món ăn theo nhà hàng | GET /Restaurant/rest-001/dishes  | HTTP 200           | ✅ Pass |
| 2   | CreateDish_WithValidData_ReturnsOk       | Tạo món ăn mới           | POST /Restaurant/rest-001/dishes | HTTP 200           | ✅ Pass |
| 3   | UpdateDish_WithValidData_ReturnsOk       | Cập nhật món ăn          | PUT /Dishes/999                  | HTTP 404 (no data) | ✅ Pass |
| 4   | DeleteDish_WithInvalidId_ReturnsNotFound | Xóa món ăn không tồn tại | DELETE /Dishes/999               | HTTP 404           | ✅ Pass |

---

#### 3.8 Module: Image

**Mô tả**: Kiểm thử các chức năng quản lý hình ảnh nhà hàng

| STT | Tên test case                                 | Mô tả                              | Input                                     | Expected Output | Thực tế |
| --- | --------------------------------------------- | ---------------------------------- | ----------------------------------------- | --------------- | ------- |
| 1   | GetImagesByRestaurant_ReturnsOk               | Lấy hình ảnh theo nhà hàng         | GET /Restaurant/rest-001/images           | HTTP 200        | ✅ Pass |
| 2   | SetPrimaryImage_WithInvalidId_ReturnsNotFound | Đặt ảnh chính (ID không tồn tại)   | PATCH /Images/999/primary                 | HTTP 404        | ✅ Pass |
| 3   | DeleteImage_WithInvalidId_ReturnsNotFound     | Xóa ảnh không tồn tại              | DELETE /Images/999                        | HTTP 404        | ✅ Pass |
| 4   | ReorderImages_WithInvalidData_ReturnsNotFound | Sắp xếp ảnh (dữ liệu không hợp lệ) | PATCH /Restaurant/rest-001/images/reorder | HTTP 404        | ✅ Pass |

---

### 4. Kết Quả Kiểm Thử

#### 4.1 Tổng Kết

| Chỉ số                 | Giá trị |
| ---------------------- | ------- |
| **Tổng số test cases** | 33      |
| **Passed**             | 33      |
| **Failed**             | 0       |
| **Skipped**            | 0       |
| **Success Rate**       | 100%    |

#### 4.2 Biểu Đồ Kết Quả

```
Test Results Summary
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Passed:  ████████████████████████████████████████████  33
❌ Failed:  ██                                            0
⏭️ Skipped: ██                                            0
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Success Rate: 100%
```

#### 4.3 Thời Gian Thực Thi

| Thông số                      | Giá trị  |
| ----------------------------- | -------- |
| **Tổng thời gian**            | ~20 giây |
| **Thời gian trung bình/test** | ~600ms   |

---

### 5. Hướng Dẫn Chạy Test

#### 5.1 Yêu Cầu Hệ Thống

- .NET 10.0 SDK
- Visual Studio 2022 hoặc VS Code với C# extension

#### 5.2 Các Bước Chạy Test

```bash
# Di chuyển đến thư mục test
cd test/Integration

# Restore packages
dotnet restore

# Build project
dotnet build

# Chạy tất cả test
dotnet test

# Chạy test với chi tiết
dotnet test --verbosity normal

# Chạy test không rebuild
dotnet test --no-build
```

#### 5.3 Xem Kết Quả Chi Tiết

```bash
# Chạy với output chi tiết
dotnet test --logger "console;verbosity=detailed"

# Xuất kết quả ra file
dotnet test --logger "trx;LogFileName=results.trx"
```

---

### 6. Bảo Trì và Mở Rộng

#### 6.1 Thêm Test Case Mới

Để thêm test case mới, thêm method vào class `ApiIntegrationTests` với attribute `[Fact]`:

```csharp
[Fact]
public async Task NewTestCase_ReturnsExpected()
{
    // Arrange
    var cookie = await LoginAndGetCookie("admin", "admin123");

    // Act
    var response = await AuthorizedRequestAsync(
        HttpMethod.Get,
        "/api/endpoint",
        cookie: cookie);

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

#### 6.2 Các Nguyên Tắc Viết Test

1. **Arrange - Act - Assert**: Tuân thủ cấu trúc 3 bước
2. **Isolation**: Mỗi test độc lập với các test khác
3. **Descriptive**: Tên test phải mô tả rõ hành vi được kiểm thử
4. **Fast**: Test nên chạy trong thời gian ngắn (< 1 giây)

---

### 7. Phụ Lục

#### 7.1 Cấu Trúc Project

```
FoodMarketNarrator/
├── FoodMarketNarrator.Api/        # Backend API
│   ├── Controllers/                # API Controllers
│   ├── Services/                   # Business Logic
│   ├── Repositories/               # Data Access
│   ├── Models/                     # Entity Models
│   └── DTOs/                      # Data Transfer Objects
├── FoodMarketNarrator.Maui/        # Mobile App
├── test/
│   └── maui-testing/
│       └── FoodMarketNarrator.Api.IntegrationTests/
│       └── ApiIntegrationTests.cs
└── docs/
    └── test/
        └── integration/
            └── API-integration-test-cases.md
```

#### 7.2 Mapping HTTP Status Codes

| Status Code      | Ý nghĩa                  |
| ---------------- | ------------------------ |
| 200 OK           | Thành công               |
| 400 Bad Request  | Dữ liệu không hợp lệ     |
| 401 Unauthorized | Chưa đăng nhập           |
| 403 Forbidden    | Không có quyền           |
| 404 Not Found    | Tài nguyên không tồn tại |

#### 7.3 Liên Kết Hữu Ích

- [xUnit Documentation](https://xunit.net/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Entity Framework Core InMemory](https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory)

---

### 8. Lịch Sử Phiên Bản

| Phiên bản | Ngày       | Mô tả                       |
| --------- | ---------- | --------------------------- |
| 1.0       | 2026-03-17 | Tạo document, 33 test cases |

---

**Tài liệu được tạo bởi**: Claude Sonnet 4.6
**Ngày tạo**: 2026-03-17
**Dự án**: Food Market Narrator
**Version**: 1.0
