# ĐẶC TẢ USE CASE (TỔNG HỢP TỪ ẢNH TRONG docs/usecase)

Tài liệu này tổng hợp đặc tả use case theo từng ảnh trong thư mục docs/usecase.

## 1. Usecase App - Tổng quát

Ảnh use case: ![Usecase App Tổng quát](TongQuat.png)

- Tên use case: Usecase tổng quát App
- Tác nhân: Người dùng
- Mô tả: Người dùng sử dụng các chức năng cốt lõi của app mobile để theo dõi vị trí, khám phá POI/tour, nghe thuyết minh và quản lý danh sách/lịch sử cá nhân.
- Tiền điều kiện: App đã cài đặt và mở thành công.
- Hậu điều kiện: Người dùng hoàn thành một hoặc nhiều thao tác chính trong hệ thống.
- Luồng chính:

1. Người dùng vào app.
2. Người dùng theo dõi vị trí.
3. Người dùng khám phá POI.
4. Người dùng nghe thuyết minh theo ngôn ngữ.
5. Người dùng khám phá tour.
6. Người dùng quản lý lịch sử nghe và danh sách quán yêu thích.

- Luồng thay thế:

1. Người dùng bỏ qua một số chức năng và chỉ dùng các chức năng cần thiết.
2. Nếu thiếu quyền vị trí hoặc không có dữ liệu, một số chức năng liên quan sẽ bị hạn chế.

## 2. Usecase App - Khám phá POI

Ảnh use case: ![Khám phá POI](KhamPhaPOI.png)

- Tên use case: Khám phá POI
- Tác nhân: Người dùng
- Mô tả: Người dùng duyệt danh sách POI, tìm kiếm/lọc, xem chi tiết, thao tác với POI như yêu thích, chia sẻ, xem bản đồ, nghe thuyết minh.
- Tiền điều kiện: Đã mở app và tải được danh sách POI.
- Hậu điều kiện: Người dùng tìm thấy POI mong muốn và có thể thực hiện thao tác tiếp theo.
- Luồng chính:

1. Người dùng mở danh sách POI.
2. Người dùng lọc POI hoặc tìm kiếm POI.
3. Người dùng chọn POI để xem chi tiết.
4. Từ màn hình chi tiết, người dùng có thể thêm vào yêu thích, chia sẻ, xem trên bản đồ, nghe thuyết minh.

- Luồng thay thế:

1. Không có kết quả tìm kiếm/lọc thì hệ thống hiển thị trạng thái rỗng.
2. Nếu không tải được chi tiết POI, hệ thống thông báo lỗi và cho thử lại.

## 3. Usecase App - Khám phá Tour

Ảnh use case: ![Khám phá Tour](KhamPhaTour.png)

- Tên use case: Khám phá Tour
- Tác nhân: Người dùng
- Mô tả: Người dùng xem danh sách tour, vào chi tiết tour và tiếp tục xem POI thuộc tour trên bản đồ.
- Tiền điều kiện: Đã có dữ liệu tour.
- Hậu điều kiện: Người dùng xác định được tour và POI cần khám phá.
- Luồng chính:

1. Người dùng xem danh sách tour hiện có.
2. Người dùng mở chi tiết tour.
3. Người dùng xem chi tiết POI trong tour.
4. Người dùng xem POI thuộc tour trên bản đồ.

- Luồng thay thế:

1. Nếu không có tour phù hợp, hệ thống hiển thị thông báo không có dữ liệu.
2. Nếu lỗi tải dữ liệu bản đồ, hệ thống cho phép quay lại màn hình trước.

## 4. Usecase App - Nghe thuyết minh theo ngôn ngữ

Ảnh use case: ![Nghe thuyết minh theo ngôn ngữ](NgheThuyetMinhTheoNgonNgu.png)

- Tên use case: Nghe thuyết minh theo ngôn ngữ
- Tác nhân: Người dùng
- Mô tả: Người dùng nghe nội dung thuyết minh theo ngôn ngữ đã chọn và có thể bật thuyết minh tự động.
- Tiền điều kiện: Đã có audio cho POI theo ngôn ngữ tương ứng.
- Hậu điều kiện: Audio được phát đúng ngôn ngữ hoặc hệ thống báo không có audio phù hợp.
- Luồng chính:

1. Người dùng bật tính năng nghe thuyết minh theo ngôn ngữ.
2. Người dùng chọn ngôn ngữ thuyết minh.
3. Hệ thống tìm audio phù hợp với ngôn ngữ.
4. Hệ thống phát audio.
5. Người dùng có thể bật thuyết minh tự động.

- Luồng thay thế:

1. Không tìm thấy audio theo ngôn ngữ thì thông báo và bỏ qua.
2. Nếu chế độ tự động tắt, người dùng vẫn có thể phát thủ công.

## 5. Usecase App - Quản lí danh sách yêu thích

Ảnh use case: ![Quản lí danh sách yêu thích](QuanLiDanhSachYeuThich.png)

- Tên use case: Quản lí danh sách quán yêu thích
- Tác nhân: Người dùng
- Mô tả: Người dùng theo dõi danh sách quán yêu thích, xem chi tiết và xóa khỏi danh sách.
- Tiền điều kiện: Người dùng đã có ít nhất một quán trong danh sách yêu thích.
- Hậu điều kiện: Danh sách yêu thích được cập nhật theo thao tác của người dùng.
- Luồng chính:

1. Người dùng mở danh sách các quán yêu thích.
2. Người dùng xem chi tiết quán.
3. Người dùng có thể xóa quán khỏi danh sách yêu thích.

- Luồng thay thế:

1. Danh sách rỗng thì hiển thị thông báo chưa có dữ liệu.
2. Xóa thất bại thì giữ nguyên danh sách và thông báo lỗi.

## 6. Usecase App - Quản lí lịch sử

Ảnh use case: ![Quản lí lịch sử POI](QuanLiLichSu.png)

- Tên use case: Quản lí lịch sử POI
- Tác nhân: Người dùng
- Mô tả: Người dùng xem lại danh sách lịch sử đã nghe và mở lại chi tiết POI từ lịch sử.
- Tiền điều kiện: Đã phát sinh lịch sử nghe trước đó.
- Hậu điều kiện: Người dùng xem được thông tin lịch sử và POI liên quan.
- Luồng chính:

1. Người dùng mở danh sách lịch sử đã nghe.
2. Người dùng chọn một mục lịch sử.
3. Hệ thống mở chi tiết POI tương ứng.

- Luồng thay thế:

1. Không có lịch sử thì hiển thị trạng thái rỗng.
2. Chi tiết POI không còn tồn tại thì thông báo và loại bỏ mục lịch sử lỗi nếu cần.

## 7. Usecase App - Theo dõi vị trí

Ảnh use case: ![Theo dõi vị trí](TheoDoiViTri.png)

- Tên use case: Theo dõi vị trí
- Tác nhân: Người dùng
- Mô tả: App theo dõi vị trí người dùng để phục vụ geofence và thuyết minh tự động.
- Tiền điều kiện: Thiết bị hỗ trợ GPS, người dùng đồng ý cấp quyền vị trí.
- Hậu điều kiện: Vị trí được cập nhật theo chu kỳ hoặc bị tạm dừng nếu không đủ điều kiện.
- Luồng chính:

1. Người dùng bật tính năng theo dõi vị trí.
2. Hệ thống yêu cầu cấp quyền vị trí (include).
3. Người dùng chấp nhận quyền.
4. Hệ thống bắt đầu lấy vị trí và cập nhật theo chu kỳ.

- Luồng thay thế:

1. Người dùng từ chối quyền vị trí thì hệ thống dừng theo dõi và thông báo.
2. Mất tín hiệu GPS tạm thời thì hệ thống thử lại ở chu kỳ tiếp theo.
