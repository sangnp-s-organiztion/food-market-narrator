# Khắc phục cập nhật POI gần nhau theo thời gian thực

Tài liệu này mô tả các thay đổi đã triển khai để giảm hiện tượng map hiển thị chậm khi người dùng di chuyển giữa các POI nằm sát nhau.

## 1) Vấn đề quan sát được

Trong khu vực có mật độ POI dày, từng xuất hiện các triệu chứng:

- Marker người dùng đã di chuyển nhưng highlight POI chưa đổi ngay.
- POI nổi bật đôi khi chỉ đúng sau khi chuyển trang hoặc render lại.
- Trải nghiệm “nearest POI” thiếu ổn định khi khoảng cách giữa các quán rất gần.

## 2) Nguyên nhân kỹ thuật chính

1. Tần suất cập nhật vị trí và nhiễu GPS làm nearest dao động liên tục.
2. Layer map chưa được invalidate đủ mạnh trong một số trường hợp style thay đổi nhưng viewport gần như không đổi.
3. Marker highlight có thể bị che nếu không được ưu tiên thứ tự vẽ.
4. Cần tách ngưỡng highlight map với ngưỡng trigger thuyết minh để giảm nhấp nháy.

## 3) Các thay đổi đã áp dụng

### 3.1 Tách ngưỡng highlight riêng cho map

- File: FoodMarketNarrator.Maui/Settings/AppSettings.cs
- Giá trị hiện tại:
  - MapHighlightDistanceMeters = 20
  - TriggerDistanceMeters = 30

Ý nghĩa: map chỉ highlight POI khi rất gần (20m), trong khi trigger thuyết minh/floating button vẫn theo 30m.

### 3.2 Tính nearest POI ổn định theo khoảng cách thực

- File: FoodMarketNarrator.Maui/Services/POIService.cs
- Hàm GetNearestPOI(...) hiện sắp xếp POI theo GetDistanceMeters(...) rồi lấy phần tử đầu tiên.

Ý nghĩa: mỗi lần có location mới đều recompute nearest từ dữ liệu hiện có, không phụ thuộc trạng thái UI cũ.

### 3.3 Cập nhật highlight ở cả MainPage và MapPage

- Files:
  - FoodMarketNarrator.Maui/Views/MainPage.xaml.cs
  - FoodMarketNarrator.Maui/Views/MapPage.xaml.cs
- Luồng xử lý:
  - Tính nearest mỗi lần nhận LocationChanged.
  - Chỉ highlight nếu khoảng cách < MapHighlightDistanceMeters.
  - Nếu xa hơn ngưỡng thì bỏ highlight (truyền null).

### 3.4 Force refresh mạnh hơn trong MapHelper

- File: FoodMarketNarrator.Maui/Helpers/MapHelper.cs
- Thay đổi trong HighlightPOIs(...):
  - Reorder feature được highlight xuống cuối danh sách để vẽ sau cùng.
  - Gọi poiLayer.DataHasChanged().
  - Gọi mapControl.Map.RefreshData() và RefreshGraphics().
- Thay đổi trong UpdateUserLocation(...):
  - Sau khi cập nhật marker user cũng gọi RefreshData() + RefreshGraphics().

Ý nghĩa: đảm bảo renderer cập nhật ngay cả khi viewport ít thay đổi.

### 3.5 Theo dõi vị trí bằng polling loop 2 giây

- File: FoodMarketNarrator.Maui/Services/LocationService.cs
- Cơ chế hiện tại:
  - PollInterval = 2 giây.
  - GeolocationRequest(Best, timeout 10 giây).
  - Chỉ phát sự kiện khi người dùng di chuyển tối thiểu 6m.

Ý nghĩa: ổn định hơn trên emulator và giảm spam event nhỏ.

## 4) Kết quả sau thay đổi

- Cập nhật marker + highlight nhất quán hơn khi di chuyển trong cụm POI gần nhau.
- Không còn phụ thuộc thao tác reload trang để thấy highlight đúng trong phần lớn trường hợp.
- Marker được highlight rõ hơn do được ưu tiên thứ tự vẽ.

## 5) Lưu ý phạm vi

- Đợt này chưa tích hợp native Android FusedLocationProviderClient.
- Cơ chế hiện tại vẫn dựa trên MAUI Geolocation polling + foreground service Android.

## 6) Checklist kiểm thử đề xuất

1. Di chuyển qua lại giữa 2 POI rất gần nhau trên emulator.
2. Xác nhận marker user cập nhật liên tục, highlight đổi đúng nearest trong phạm vi 20m.
3. Kiểm tra lại khu vực POI xa nhau để đảm bảo không bị regression.
4. Kiểm tra floating button thuyết minh vẫn theo ngưỡng 30m.
