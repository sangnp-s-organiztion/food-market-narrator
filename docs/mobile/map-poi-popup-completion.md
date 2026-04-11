# Hoàn thiện tính năng POI popup trên MapPage

Tài liệu này tổng hợp trạng thái hiện tại của tính năng chọn POI trên bản đồ và hiển thị popup chi tiết.

## 1) Phạm vi liên quan

- FoodMarketNarrator.Maui/Views/MapPage.xaml
- FoodMarketNarrator.Maui/Views/MapPage.xaml.cs
- FoodMarketNarrator.Maui/Helpers/MapHelper.cs

## 2) Thành phần UI đã có

### 2.1 Cụm điều khiển bên phải

- Zoom In: OnZoomInTapped
- Zoom Out: OnZoomOutTapped
- My Location: OnMyLocationTapped

Ghi chú:

- Zoom dùng thay đổi resolution của viewport, có clamp theo MinZoomLevel/MaxZoomLevel.
- My Location lấy vị trí hiện tại, center map và cập nhật marker user.

### 2.2 Popup card POI phía dưới

Card được bind theo POI đang chọn, gồm:

- Ảnh: SelectedPoiImage
- Tên: SelectedPoiName
- Địa chỉ: SelectedPoiAddress
- Nút Xem chi tiết: OnViewDetailClicked

Mặc định card ẩn (IsVisible = false) và chỉ hiện khi tap trúng POI hợp lệ.

## 3) Luồng tap map để chọn POI

Trong OnMapTapped:

1. Lấy điểm tap từ e.WorldPosition.
2. Chuyển world coordinate sang lat/lon bằng SphericalMercator.ToLonLat(...).
3. Tìm nearest POI từ danh sách \_pois.
4. Tính tap threshold theo zoom:
   - tapThresholdMeters = Clamp(viewportResolution \* 28, 12, 150)
5. Nếu distance tới nearest <= threshold thì:
   - Hiển thị popup card bằng ShowSelectedPoiCard(...)
   - Highlight POI bằng MapHelper.HighlightPOI(...)
6. Nếu không đạt ngưỡng thì ẩn card.

Ý nghĩa: thao tác tap ổn định hơn ở nhiều mức zoom khác nhau.

## 4) Tìm kiếm POI trên MapPage

Ngoài popup từ map tap, MapPage hiện có thêm luồng search:

- Search bar với debounce 220ms.
- Gợi ý tối đa 6 kết quả.
- Chuẩn hóa từ khóa có bỏ dấu (Normalize FormD + bỏ NonSpacingMark) để tìm gần đúng.
- Highlight nhiều POI theo kết quả tìm kiếm bằng MapHelper.HighlightPOIs(..., isSearchResult: true).
- Khi chọn suggestion, map tự focus về POI đầu tiên.

## 5) Điều hướng sang trang chi tiết

Nút Xem chi tiết sẽ:

1. Lấy restaurantId từ \_selectedPoi.
2. Encode bằng Uri.EscapeDataString(...).
3. Điều hướng route: POIDetailPage?restaurantId={encodedId}.

## 6) Tối ưu render marker/hightlight

MapHelper đã có các cải tiến để tránh hiện tượng “đổi trạng thái nhưng không vẽ lại ngay”:

- Reorder feature được highlight xuống cuối để vẽ trên cùng.
- Gọi poiLayer.DataHasChanged().
- Gọi mapControl.Map.RefreshData() + RefreshGraphics().

Các bước này áp dụng cho cả highlight POI và cập nhật marker vị trí người dùng.

## 7) Trạng thái hiện tại

- Đã hoạt động: tap POI, popup card, zoom, my location, view detail, search + suggestion + highlight.
- Đã có logic filter chip trên MapPage cho các nhóm: Tất cả, Gần bạn, Yêu thích, Đang mở.

## 8) Checklist kiểm thử

1. Mở MapPage, thử zoom in/out và xác nhận giới hạn zoom hợp lệ.
2. Bấm My Location để center về vị trí hiện tại.
3. Tap vào marker gần và xác nhận card hiển thị đúng tên/ảnh/địa chỉ.
4. Tap xa marker để xác nhận card tự ẩn.
5. Gõ từ khóa có dấu/không dấu, xác nhận suggestion và highlight hoạt động.
6. Bấm Xem chi tiết, xác nhận mở đúng POIDetailPage theo restaurantId.
