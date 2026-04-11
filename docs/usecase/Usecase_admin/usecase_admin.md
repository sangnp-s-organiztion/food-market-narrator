# Đặc Tả Use Case Admin

Tài liệu này đặc tả các use case cho Admin dựa trên flow thực tế hiện có trong hệ thống.

Phạm vi gồm 24 use case (bao gồm các nhánh 10a, 14a, 18a, 20a).

## UC-ADM-01 - Kiểm tra phiên đăng nhập

Mục tiêu: Xác định phiên admin còn hợp lệ để cho phép vào route bảo vệ.

Tác nhân chính: Admin.

Tiền điều kiện: Admin đã truy cập ứng dụng admin.

Hậu điều kiện: Thiết lập trạng thái đăng nhập hoặc chuyển về trang đăng nhập.

Luồng chính:
1. Admin mở ứng dụng.
2. Frontend gọi GET /Auth/admin/me.
3. API trả về user hợp lệ, role admin.
4. Frontend cho phép truy cập route bảo vệ.

Luồng phụ:
1. API trả 401 hoặc role không hợp lệ.
2. Frontend xóa state local.
3. Frontend điều hướng về /login.

## UC-ADM-02 - Đăng nhập admin

Mục tiêu: Cho phép admin đăng nhập bằng username/password.

Tác nhân chính: Admin.

Tiền điều kiện: Admin ở trang đăng nhập.

Hậu điều kiện: Đăng nhập thành công vào dashboard hoặc hiển thị lỗi.

Luồng chính:
1. Admin nhập username/password.
2. Frontend validate dữ liệu bắt buộc.
3. Frontend gọi POST /Auth/admin/login.
4. API trả thành công và set cookie.
5. Frontend điều hướng vào dashboard.

Luồng phụ:
1. Validate không hợp lệ hoặc API trả lỗi.
2. Frontend hiển thị thông báo lỗi đăng nhập.

## UC-ADM-03 - Đăng xuất

Mục tiêu: Kết thúc phiên làm việc admin an toàn.

Tác nhân chính: Admin.

Tiền điều kiện: Admin đang đăng nhập.

Hậu điều kiện: State đăng nhập bị xóa và quay về /login.

Luồng chính:
1. Admin bấm Đăng xuất.
2. Frontend gọi POST /Auth/logout theo best-effort.
3. Frontend xóa user và auth state.
4. Frontend chuyển về /login.

## UC-ADM-04 - Điều hướng và bảo vệ route

Mục tiêu: Chỉ cho phép người dùng admin truy cập các trang quản trị.

Tác nhân chính: Admin.

Tiền điều kiện: Có yêu cầu điều hướng tới route trong dashboard.

Hậu điều kiện: Route hợp lệ được render hoặc bị chặn.

Luồng chính:
1. Admin chọn menu trên sidebar.
2. React Router điều hướng đến route mục tiêu.
3. ProtectedRoute kiểm tra isAuthenticated và role admin.
4. Trang tương ứng được hiển thị.

Luồng phụ:
1. Không đủ điều kiện truy cập.
2. Hệ thống điều hướng về /login.

## UC-ADM-05 - Xem dashboard tổng quan

Mục tiêu: Hiển thị KPI tổng quan hệ thống cho admin.

Tác nhân chính: Admin.

Tiền điều kiện: Admin đã đăng nhập và vào trang tổng quan.

Hậu điều kiện: Dashboard hiển thị KPI thực thể, analytics và dữ liệu bản đồ.

Luồng chính:
1. Frontend tải song song KPI thực thể, KPI analytics và dữ liệu map.
2. Frontend tổng hợp dữ liệu.
3. Frontend render card thống kê, chart top restaurants và heatmap.

Luồng phụ:
1. Một hoặc nhiều API lỗi.
2. Frontend hiển thị trạng thái lỗi/thiếu dữ liệu tương ứng.

## UC-ADM-06 - Đổi bộ lọc heatmap

Mục tiêu: Cập nhật heatmap theo mốc thời gian admin chọn.

Tác nhân chính: Admin.

Tiền điều kiện: Admin đang xem dashboard.

Hậu điều kiện: Bản đồ nhiệt phản ánh filter mới.

Luồng chính:
1. Admin chọn mốc 1h/6h/24h/all.
2. Frontend gọi API heatmap với tham số tương ứng.
3. Frontend cập nhật dữ liệu heatmap.

## UC-ADM-07 - Xem tuyến di chuyển người dùng

Mục tiêu: Theo dõi đường đi của người dùng trên bản đồ.

Tác nhân chính: Admin.

Tiền điều kiện: Admin vào trang /trajectory.

Hậu điều kiện: Bản đồ trajectory hiển thị theo sessionLimit hiện tại.

Luồng chính:
1. Frontend gọi GET /api/analytics/movement-paths?sessionLimit=100.
2. API trả danh sách session và tọa độ.
3. Frontend render tuyến di chuyển.

## UC-ADM-08 - Xem danh sách và tìm kiếm nhà hàng

Mục tiêu: Quản lý và tra cứu nhà hàng nhanh trên giao diện admin.

Tác nhân chính: Admin.

Tiền điều kiện: Admin ở trang nhà hàng.

Hậu điều kiện: Danh sách nhà hàng được hiển thị và lọc theo từ khóa.

Luồng chính:
1. Frontend tải danh sách nhà hàng và người dùng.
2. Frontend lọc seller active cho form tạo.
3. Admin nhập từ khóa tìm kiếm.
4. Frontend lọc client theo tên/địa chỉ và hiển thị kết quả.

## UC-ADM-09 - Xem chi tiết nhà hàng

Mục tiêu: Xem đầy đủ thông tin chi tiết của một nhà hàng.

Tác nhân chính: Admin.

Tiền điều kiện: Nhà hàng tồn tại trong danh sách.

Hậu điều kiện: Dialog chi tiết mở thành công hoặc hiển thị lỗi.

Luồng chính:
1. Admin bấm xem chi tiết.
2. Frontend gọi GET /restaurant/{id}.
3. Frontend chọn ảnh primary (fallback ảnh đầu tiên).
4. Frontend hiển thị dialog chi tiết.

Luồng phụ:
1. API lỗi.
2. Frontend hiển thị lỗi tải chi tiết.

## UC-ADM-10 - Tạo nhà hàng mới

Mục tiêu: Tạo mới nhà hàng và gán seller quản lý.

Tác nhân chính: Admin.

Tiền điều kiện: Admin ở trang nhà hàng; có seller active khả dụng.

Hậu điều kiện: Nhà hàng mới được tạo và danh sách được cập nhật.

Luồng chính:
1. Admin mở dialog và nhập thông tin.
2. Frontend validate tên nhà hàng và seller quản lý.
3. Frontend gọi POST /restaurant.
4. API trả thành công.
5. Frontend reload danh sách, đóng dialog và hiển thị thông báo thành công.

Luồng phụ:
1. Validate không hợp lệ hoặc API lỗi.
2. Frontend hiển thị thông báo thất bại.

## UC-ADM-10A - Tự điền tọa độ từ link Google Maps khi tạo nhà hàng

Mục tiêu: Hỗ trợ tự động điền vĩ độ/kinh độ từ link Google Maps.

Tác nhân chính: Admin.

Tiền điều kiện: Admin nhập URL Google Maps trong form tạo nhà hàng.

Hậu điều kiện: Tọa độ được tự điền hoặc báo lỗi để nhập tay.

Luồng chính:
1. Frontend parse tọa độ từ URL bằng regex.
2. Nếu parse được, frontend điền latitude/longitude.

Luồng phụ:
1. Parse client thất bại.
2. Frontend gọi API resolve-coordinates.
3. Nếu API thành công thì tự điền tọa độ.
4. Nếu API thất bại thì hiển thị lỗi và cho nhập thủ công.

## UC-ADM-11 - Khóa hoặc mở khóa nhà hàng

Mục tiêu: Quản lý trạng thái hoạt động của nhà hàng.

Tác nhân chính: Admin.

Tiền điều kiện: Nhà hàng tồn tại trong danh sách.

Hậu điều kiện: Trạng thái nhà hàng được cập nhật hoặc giữ nguyên nếu lỗi.

Luồng chính:
1. Admin bấm khóa/mở khóa nhà hàng.
2. Frontend hiển thị confirm dialog.
3. Admin xác nhận thao tác.
4. Frontend gọi PATCH /restaurant/{id}/status.
5. Frontend cập nhật danh sách và thông báo thành công.

Luồng phụ:
1. API lỗi.
2. Frontend thông báo thất bại.

## UC-ADM-12 - Xem danh sách người dùng

Mục tiêu: Hiển thị bảng người dùng để quản trị.

Tác nhân chính: Admin.

Tiền điều kiện: Admin vào trang người dùng.

Hậu điều kiện: Danh sách người dùng được hiển thị hoặc báo lỗi tải dữ liệu.

Luồng chính:
1. Frontend gọi GET /api/users.
2. API trả danh sách user.
3. Frontend render bảng người dùng.

Luồng phụ:
1. API lỗi.
2. Frontend hiển thị lỗi tải dữ liệu.

## UC-ADM-13 - Tạo người dùng mới

Mục tiêu: Tạo mới tài khoản người dùng trong hệ thống.

Tác nhân chính: Admin.

Tiền điều kiện: Admin ở trang người dùng.

Hậu điều kiện: User mới được tạo và danh sách được cập nhật.

Luồng chính:
1. Admin mở dialog tạo user và nhập thông tin.
2. Frontend validate username/password/confirm/phone/email/role.
3. Frontend gọi POST /api/users.
4. API trả thành công.
5. Frontend refresh bảng và hiển thị thông báo thành công.

Luồng phụ:
1. Validate không hợp lệ hoặc API lỗi.
2. Frontend hiển thị lỗi tương ứng.

## UC-ADM-14 - Khóa hoặc mở khóa người dùng

Mục tiêu: Quản lý trạng thái active của tài khoản người dùng.

Tác nhân chính: Admin.

Tiền điều kiện: User tồn tại trong bảng quản trị.

Hậu điều kiện: Trạng thái user được cập nhật hoặc giữ nguyên nếu lỗi.

Luồng chính:
1. Admin bấm khóa/mở khóa user.
2. Frontend hiển thị xác nhận.
3. Admin xác nhận thao tác.
4. Frontend gọi PATCH /api/users/{id}/status.
5. Frontend refresh bảng và hiển thị kết quả.

Luồng phụ:
1. API lỗi.
2. Frontend hiển thị thông báo lỗi.

## UC-ADM-14A - Xem chi tiết người dùng và nhà hàng quản lý

Mục tiêu: Xem hồ sơ user và các nhà hàng user đang quản lý.

Tác nhân chính: Admin.

Tiền điều kiện: Dữ liệu user và restaurant đã được tải.

Hậu điều kiện: Chi tiết user được hiển thị đầy đủ.

Luồng chính:
1. Admin mở chi tiết một user.
2. Frontend lấy dữ liệu user từ danh sách hiện có.
3. Frontend đối chiếu danh sách nhà hàng user quản lý.
4. Frontend hiển thị thông tin hồ sơ và danh sách nhà hàng liên quan.

## UC-ADM-15 - Xem nhật ký hệ thống và nhật ký nghe audio

Mục tiêu: Theo dõi hoạt động vận hành qua hai loại log.

Tác nhân chính: Admin.

Tiền điều kiện: Admin truy cập trang logs.

Hậu điều kiện: Hai bảng log được hiển thị đồng thời.

Luồng chính:
1. Frontend gọi API audit logs.
2. Frontend gọi API audio activity logs.
3. Frontend hiển thị hai bảng dữ liệu.

Luồng phụ:
1. Một trong hai API lỗi.
2. Frontend hiển thị lỗi ở bảng tương ứng.

## UC-ADM-16 - Tự động làm mới nhật ký

Mục tiêu: Giữ dữ liệu log luôn mới theo chu kỳ tự động.

Tác nhân chính: Admin.

Tiền điều kiện: Trang logs đang mở.

Hậu điều kiện: Dữ liệu log được refresh mỗi 30 giây.

Luồng chính:
1. Frontend khởi tạo timer 30 giây.
2. Mỗi chu kỳ, frontend gọi lại API cho hai bảng log.
3. Frontend cập nhật dữ liệu mới lên UI.

## UC-ADM-17 - Phân trang nhật ký

Mục tiêu: Duyệt dữ liệu log theo từng trang độc lập.

Tác nhân chính: Admin.

Tiền điều kiện: Trang logs có nhiều bản ghi.

Hậu điều kiện: Bảng audit và audio activity chuyển trang đúng trạng thái.

Luồng chính:
1. Admin đổi trang ở bảng audit hoặc audio activity.
2. Frontend cập nhật page/pageSize theo bảng tương ứng.
3. Frontend gọi lại API theo tham số mới.
4. Frontend render dữ liệu trang mới.

## UC-ADM-18 - Xem danh sách tour và chi tiết tour

Mục tiêu: Theo dõi danh sách tour và thông tin chi tiết của từng tour.

Tác nhân chính: Admin.

Tiền điều kiện: Admin vào trang tour.

Hậu điều kiện: Danh sách tour và chi tiết tour được hiển thị.

Luồng chính:
1. Frontend tải danh sách tour.
2. Frontend tải dữ liệu nhà hàng liên quan.
3. Admin chọn một tour.
4. Frontend hiển thị chi tiết tour để chỉnh sửa.

## UC-ADM-18A - Tạo tour mới

Mục tiêu: Tạo mới tour, có thể kèm ảnh đại diện tour.

Tác nhân chính: Admin.

Tiền điều kiện: Admin ở trang tour và có dữ liệu hợp lệ.

Hậu điều kiện: Tour mới được tạo thành công và hiển thị trong danh sách.

Luồng chính:
1. Admin nhập thông tin tour.
2. Frontend validate dữ liệu đầu vào.
3. Nếu có ảnh, frontend upload ảnh tour.
4. Frontend gọi API tạo tour.
5. Frontend refresh danh sách tour.

Luồng phụ:
1. Validate lỗi hoặc API lỗi.
2. Frontend hiển thị thông báo thất bại.

## UC-ADM-19 - Lưu cập nhật tour (stop order và metadata)

Mục tiêu: Lưu các thay đổi cấu hình tour.

Tác nhân chính: Admin.

Tiền điều kiện: Tour đang ở chế độ chỉnh sửa.

Hậu điều kiện: Thứ tự điểm dừng và metadata được lưu.

Luồng chính:
1. Admin chỉnh sửa stop order và thông tin tour.
2. Frontend validate dữ liệu trước khi lưu.
3. Frontend gọi API cập nhật stop order.
4. Frontend gọi API cập nhật metadata.
5. Frontend hiển thị kết quả lưu thành công.

Luồng phụ:
1. Một trong các API lỗi.
2. Frontend hiển thị lỗi và giữ trạng thái chưa lưu.

## UC-ADM-20 - Thêm nhà hàng vào tour

Mục tiêu: Bổ sung điểm dừng nhà hàng vào tour.

Tác nhân chính: Admin.

Tiền điều kiện: Tour hợp lệ và không có thay đổi chưa lưu.

Hậu điều kiện: Nhà hàng được thêm vào danh sách điểm dừng của tour.

Luồng chính:
1. Admin chọn nhà hàng cần thêm.
2. Frontend kiểm tra dữ liệu hợp lệ.
3. Frontend gọi API thêm nhà hàng vào tour.
4. Frontend cập nhật chi tiết tour và hiển thị thông báo thành công.

Luồng phụ:
1. Có unsaved changes hoặc dữ liệu không hợp lệ.
2. Frontend chặn thao tác và hiển thị cảnh báo.

## UC-ADM-20A - Khóa hoặc mở khóa tour

Mục tiêu: Kích hoạt hoặc tạm ngưng tour theo nhu cầu vận hành.

Tác nhân chính: Admin.

Tiền điều kiện: Tour tồn tại trong danh sách.

Hậu điều kiện: Trạng thái tour được cập nhật.

Luồng chính:
1. Admin bấm khóa/mở khóa tour.
2. Frontend hiển thị xác nhận thao tác.
3. Admin xác nhận.
4. Frontend gọi PATCH /Tour/{id}.
5. Frontend cập nhật danh sách và hiển thị kết quả.

Luồng phụ:
1. API lỗi.
2. Frontend hiển thị thông báo thất bại.

## UC-ADM-21 - Xem và cập nhật thông tin tài khoản

Mục tiêu: Quản lý hồ sơ admin hiện tại.

Tác nhân chính: Admin.

Tiền điều kiện: Admin đã đăng nhập và mở trang tài khoản.

Hậu điều kiện: Thông tin profile được cập nhật và state đăng nhập được refresh.

Luồng chính:
1. Frontend tải dữ liệu profile hiện tại.
2. Admin chỉnh sửa thông tin cá nhân.
3. Frontend validate dữ liệu.
4. Frontend gọi API cập nhật profile.
5. Frontend gọi refresh me và hiển thị thành công.

Luồng phụ:
1. Validate hoặc API lỗi.
2. Frontend hiển thị thông báo lỗi.

## UC-ADM-22 - Đổi mật khẩu tài khoản

Mục tiêu: Cho phép admin đổi mật khẩu an toàn.

Tác nhân chính: Admin.

Tiền điều kiện: Admin ở trang tài khoản.

Hậu điều kiện: Mật khẩu mới được cập nhật và form được reset.

Luồng chính:
1. Admin nhập mật khẩu cũ, mật khẩu mới, xác nhận.
2. Frontend validate bắt buộc, độ dài và xác nhận khớp.
3. Frontend gọi API đổi mật khẩu.
4. Frontend reset form và hiển thị thành công.

Luồng phụ:
1. Validate không đạt hoặc API lỗi.
2. Frontend hiển thị lỗi tương ứng.

## UC-ADM-23 - Xem chi phí dịch token

Mục tiêu: Theo dõi chi phí dịch token ở mức tổng hợp và chi tiết.

Tác nhân chính: Admin.

Tiền điều kiện: Admin mở trang translation-billing.

Hậu điều kiện: KPI cùng dữ liệu monthly/usage được hiển thị.

Luồng chính:
1. Frontend tải dữ liệu KPI.
2. Frontend tải bảng monthly summary.
3. Frontend tải bảng usage chi tiết.
4. Frontend hiển thị dashboard chi phí dịch token.

Luồng phụ:
1. Một phần dữ liệu lỗi.
2. Frontend hiển thị thông báo lỗi theo khối dữ liệu.

## UC-ADM-24 - Lọc và phân trang chi phí dịch token

Mục tiêu: Truy vấn và duyệt dữ liệu billing theo bộ lọc linh hoạt.

Tác nhân chính: Admin.

Tiền điều kiện: Admin đang ở trang translation-billing.

Hậu điều kiện: Dữ liệu monthly và usage phản ánh bộ lọc và trang hiện tại.

Luồng chính:
1. Admin chọn tháng hoặc seller.
2. Frontend reset page phù hợp và gọi lại API theo filter mới.
3. Admin thao tác phân trang ở bảng monthly/usage.
4. Frontend tải dữ liệu trang mới cho từng bảng độc lập.

