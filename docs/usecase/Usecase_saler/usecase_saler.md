# Đặc Tả Use Case Saler

Tài liệu này đặc tả các use case cho Saler dựa trên flow thực tế hiện có trong hệ thống.

Phạm vi bao gồm 19 use case từ xác thực, quản lý nhà hàng, món ăn, hình ảnh, âm thanh, thống kê đến quản lý tài khoản.

## UC-SAL-01 - Kiểm tra phiên đăng nhập

Mục tiêu: Xác định người dùng đã đăng nhập hợp lệ với vai trò saler hay chưa khi mở ứng dụng.

Tác nhân chính: Saler.

Tiền điều kiện: Người dùng đã truy cập ứng dụng saler.

Hậu điều kiện: Hệ thống thiết lập trạng thái đăng nhập hoặc điều hướng về trang đăng nhập.

Luồng chính:
1. Saler mở ứng dụng.
2. Frontend gọi GET /Auth/me.
3. Hệ thống trả về thông tin user hợp lệ và role saler.
4. Frontend thiết lập isAuthenticated=true.

Luồng phụ:
1. API trả về lỗi hoặc role không phải saler.
2. Frontend gọi POST /Auth/logout theo best-effort.
3. Frontend điều hướng về /login.

## UC-SAL-02 - Đăng nhập saler

Mục tiêu: Cho phép saler đăng nhập vào hệ thống bằng username/password.

Tác nhân chính: Saler.

Tiền điều kiện: Saler ở trang /login.

Hậu điều kiện: Đăng nhập thành công và chuyển tới /select-restaurant, hoặc hiển thị lỗi.

Luồng chính:
1. Saler nhập username/password.
2. Frontend gọi POST /Auth/login.
3. API trả về thành công và set cookie, role là saler.
4. Frontend điều hướng đến /select-restaurant.

Luồng phụ:
1. Sai thông tin hoặc role không hợp lệ.
2. Frontend gọi logout best-effort.
3. Frontend hiển thị lỗi đăng nhập.

## UC-SAL-03 - Quên mật khẩu bằng OTP

Mục tiêu: Khôi phục mật khẩu thông qua OTP email.

Tác nhân chính: Saler.

Tiền điều kiện: Saler ở trang đăng nhập và nhớ username/email.

Hậu điều kiện: Mật khẩu mới được cập nhật hoặc quá trình thất bại có thông báo lỗi.

Luồng chính:
1. Saler mở dialog quên mật khẩu.
2. Saler nhập username + email.
3. Frontend gọi POST /Auth/forgot-password/send-otp.
4. Frontend hiển thị countdown OTP.
5. Saler nhập OTP.
6. Frontend gọi POST /Auth/forgot-password/verify-otp.
7. Saler nhập mật khẩu mới và xác nhận.
8. Frontend gọi POST /Auth/forgot-password/reset.
9. Frontend đóng dialog và hiển thị toast thành công.

Luồng phụ:
1. OTP không hợp lệ hoặc hết hạn.
2. Frontend hiển thị lỗi và yêu cầu gửi OTP lại.

## UC-SAL-04 - Đăng xuất

Mục tiêu: Kết thúc phiên làm việc của saler.

Tác nhân chính: Saler.

Tiền điều kiện: Saler đang đăng nhập.

Hậu điều kiện: Trạng thái đăng nhập bị xóa và quay về /login.

Luồng chính:
1. Saler bấm Đăng xuất.
2. Frontend gọi POST /Auth/logout theo best-effort.
3. Frontend xóa auth state.
4. Frontend điều hướng về /login.

## UC-SAL-05 - Điều hướng và bảo vệ route

Mục tiêu: Đảm bảo chỉ người dùng hợp lệ được vào đúng route.

Tác nhân chính: Saler.

Tiền điều kiện: Có yêu cầu truy cập route dashboard.

Hậu điều kiện: Người dùng được render trang hợp lệ hoặc bị chuyển hướng.

Luồng chính:
1. Saler truy cập /dashboard/*.
2. ProtectedRoute kiểm tra isAuthenticated.
3. Hệ thống kiểm tra selectedRestaurant nếu route yêu cầu.
4. Render trang dashboard tương ứng.

Luồng phụ:
1. Chưa đăng nhập thì redirect /login.
2. Đã đăng nhập nhưng chưa chọn nhà hàng, và route khác /dashboard/account thì redirect /select-restaurant.

## UC-SAL-06 - Chọn nhà hàng để quản lý

Mục tiêu: Chọn nhà hàng mục tiêu cho phiên quản trị.

Tác nhân chính: Saler.

Tiền điều kiện: Saler đã đăng nhập và ở /select-restaurant.

Hậu điều kiện: selectedRestaurant được lưu trong context và chuyển sang dashboard.

Luồng chính:
1. Frontend gọi GET /Restaurant.
2. Frontend lọc theo user_id của saler hiện tại.
3. Saler chọn một nhà hàng để quản lý.
4. Frontend gọi selectRestaurant(restaurantId).
5. Frontend điều hướng /dashboard/restaurant.

Luồng phụ:
1. Chỉ có một nhà hàng hợp lệ.
2. Hệ thống tự chọn và tự chuyển trang.

## UC-SAL-07 - Chuyển nhà hàng từ header dashboard

Mục tiêu: Đổi nhanh nhà hàng đang quản lý trong dashboard.

Tác nhân chính: Saler.

Tiền điều kiện: Saler đang ở dashboard và có danh sách nhà hàng có thể chọn.

Hậu điều kiện: selectedRestaurant mới được áp dụng cho các trang con.

Luồng chính:
1. Saler chọn nhà hàng mới trong dropdown header.
2. Frontend gọi selectRestaurant(restaurantId).
3. Các trang con tải dữ liệu theo nhà hàng mới.

## UC-SAL-08 - Cập nhật thông tin nhà hàng

Mục tiêu: Cập nhật thông tin và trạng thái hoạt động nhà hàng.

Tác nhân chính: Saler.

Tiền điều kiện: Đã chọn nhà hàng và ở trang nhà hàng.

Hậu điều kiện: Dữ liệu nhà hàng được cập nhật hoặc báo lỗi.

Luồng chính:
1. Saler chỉnh sửa thông tin nhà hàng.
2. Saler bấm Lưu thay đổi.
3. Frontend gọi PATCH /Restaurant/{id}.
4. Frontend gọi PATCH /Restaurant/{id}/status.
5. Frontend refresh danh sách nhà hàng.
6. Frontend hiển thị toast thành công.

Luồng phụ:
1. API trả lỗi.
2. Frontend hiển thị toast lỗi.

## UC-SAL-09 - Tự động hoặc thủ công trạng thái mở cửa

Mục tiêu: Quản lý trạng thái mở cửa theo lịch hoặc thao tác tay.

Tác nhân chính: Saler.

Tiền điều kiện: Saler ở trang nhà hàng.

Hậu điều kiện: Trạng thái mở cửa được xác định theo mode đã chọn.

Luồng chính:
1. Saler bật auto mode.
2. Hệ thống định kỳ tính isWithinSchedule.
3. Hệ thống cập nhật trạng thái is_active trên form.

Luồng phụ:
1. Saler tắt auto mode.
2. Saler gạt trạng thái thủ công mở hoặc đóng.

## UC-SAL-10 - Tự điền tọa độ từ link Google Maps

Mục tiêu: Tự động lấy latitude/longitude từ URL Google Maps.

Tác nhân chính: Saler.

Tiền điều kiện: Saler nhập link Google Maps tại form nhà hàng.

Hậu điều kiện: Tọa độ được điền tự động hoặc hiển thị lỗi.

Luồng chính:
1. Saler dán link Google Maps.
2. Frontend parse tọa độ bằng regex.
3. Nếu parse thành công, frontend tự điền latitude/longitude.

Luồng phụ:
1. Parse client thất bại.
2. Frontend gọi GET /api/maps/resolve-coordinates?url=....
3. Nếu API thành công thì điền tọa độ.
4. Nếu API thất bại thì hiển thị toast lỗi.

## UC-SAL-11 - Xem danh sách món ăn

Mục tiêu: Hiển thị danh sách món ăn kèm ảnh tương ứng.

Tác nhân chính: Saler.

Tiền điều kiện: Đã chọn nhà hàng và truy cập /dashboard/dishes.

Hậu điều kiện: Danh sách món + ảnh được hiển thị.

Luồng chính:
1. Frontend tải song song dishes và images.
2. Frontend map image_id của dish với ảnh phù hợp.
3. Frontend render danh sách món.

## UC-SAL-12 - Thêm món ăn mới

Mục tiêu: Tạo món ăn mới, có thể kèm ảnh.

Tác nhân chính: Saler.

Tiền điều kiện: Đã chọn nhà hàng và ở trang món ăn.

Hậu điều kiện: Món mới được tạo hoặc rollback ảnh nếu tạo dish thất bại.

Luồng chính:
1. Saler mở dialog thêm món.
2. Saler nhập tên, giá, chọn ảnh tùy chọn.
3. Frontend validate dữ liệu.
4. Nếu có ảnh, frontend upload ảnh.
5. Frontend gọi POST /Restaurant/{id}/dishes.
6. Frontend đóng dialog và hiển thị thành công.

Luồng phụ:
1. Dish tạo thất bại sau khi upload ảnh.
2. Frontend gọi DELETE ảnh vừa upload theo best-effort.
3. Frontend hiển thị lỗi.

## UC-SAL-13 - Cập nhật món ăn

Mục tiêu: Chỉnh sửa thông tin món ăn và/hoặc thay ảnh.

Tác nhân chính: Saler.

Tiền điều kiện: Món ăn đã tồn tại.

Hậu điều kiện: Món ăn được cập nhật; ảnh cũ hoặc ảnh mới được xử lý an toàn.

Luồng chính:
1. Saler mở dialog sửa món.
2. Saler chỉnh tên, giá, ảnh.
3. Frontend gọi PUT /Dishes/{dishId}.
4. Frontend thông báo kết quả.

Luồng phụ:
1. Có chọn ảnh mới.
2. Frontend upload ảnh mới trước khi gọi PUT.
3. Nếu PUT thành công thì xóa ảnh cũ best-effort.
4. Nếu PUT thất bại thì xóa ảnh mới upload best-effort.

## UC-SAL-14 - Xóa món ăn

Mục tiêu: Xóa món ăn khỏi danh sách quản lý.

Tác nhân chính: Saler.

Tiền điều kiện: Món ăn tồn tại trong danh sách.

Hậu điều kiện: Món bị xóa hoặc giữ nguyên nếu API lỗi.

Luồng chính:
1. Saler bấm xóa món.
2. Frontend gọi DELETE /Dishes/{dishId}.
3. Frontend cập nhật lại danh sách và hiển thị toast thành công.

Luồng phụ:
1. API lỗi.
2. Frontend hiển thị toast lỗi.

Ghi chú: Hiện chưa có confirm dialog trước khi xóa.

## UC-SAL-15 - Thay ảnh đại diện nhà hàng

Mục tiêu: Cập nhật ảnh primary của nhà hàng.

Tác nhân chính: Saler.

Tiền điều kiện: Đã chọn nhà hàng và truy cập /dashboard/images.

Hậu điều kiện: Ảnh đại diện mới được cập nhật và dữ liệu liên quan nhất quán.

Luồng chính:
1. Saler chọn ảnh và tải lên với is_primary=true.
2. Frontend xử lý các món đang dùng ảnh primary cũ.
3. Frontend xóa ảnh primary cũ.
4. Frontend hiển thị avatar mới và toast thành công.

## UC-SAL-16 - Tải lên và quản lý phiên bản âm thanh

Mục tiêu: Quản lý đầy đủ vòng đời audio mô tả theo ngôn ngữ.

Tác nhân chính: Saler.

Tiền điều kiện: Đã chọn nhà hàng và ở /dashboard/audio/description.

Hậu điều kiện: Audio được tải lên, bật/tắt active, hoặc xóa theo ràng buộc nghiệp vụ.

Luồng chính:
1. Frontend tải danh sách ngôn ngữ và audio.
2. Saler tải file audio mới.
3. Frontend gọi POST /Restaurant/{id}/audios và refresh danh sách.
4. Saler bật hoặc tắt active audio.
5. Frontend gọi PATCH /Audios/{audioId}/active.
6. Saler xóa audio.
7. Frontend gọi DELETE /Audios/{audioId}.

Luồng phụ:
1. Tắt active bị chặn nếu ngôn ngữ chỉ còn 1 active.
2. Xóa bị chặn nếu ngôn ngữ chỉ còn 1 bản ghi.

## UC-SAL-17 - Dịch văn bản và tạo âm thanh từ text

Mục tiêu: Dịch nội dung và sinh audio trực tiếp từ văn bản.

Tác nhân chính: Saler.

Tiền điều kiện: Đã chọn nhà hàng và có nội dung văn bản nguồn.

Hậu điều kiện: Có bản dịch và/hoặc audio mới để preview.

Luồng chính:
1. Saler nhập source text, chọn ngôn ngữ nguồn và đích.
2. Frontend gọi POST /Restaurant/{id}/translate.
3. Frontend hiển thị translatedText và estimatedCost.
4. Saler bấm tạo âm thanh.
5. Frontend gọi POST /Restaurant/{id}/audios/from-text.
6. Frontend refresh danh sách audio và cho preview.

## UC-SAL-18 - Xem thống kê nghe và lịch sử chi phí token

Mục tiêu: Theo dõi KPI nghe audio và chi phí dịch token.

Tác nhân chính: Saler.

Tiền điều kiện: Đã chọn nhà hàng và truy cập /dashboard/audio/history.

Hậu điều kiện: KPI và dữ liệu usage được hiển thị theo bộ lọc hiện tại.

Luồng chính:
1. Frontend gọi API KPI nhà hàng.
2. Frontend gọi API usage theo billingMonth/page/pageSize.
3. Frontend hiển thị KPI và bảng usage.
4. Saler đổi tháng hoặc chuyển trang.
5. Frontend tải lại usage và cập nhật bảng.

## UC-SAL-19 - Xem và cập nhật tài khoản

Mục tiêu: Quản lý hồ sơ cá nhân và đổi mật khẩu.

Tác nhân chính: Saler.

Tiền điều kiện: Saler đã đăng nhập, truy cập /dashboard/account.

Hậu điều kiện: Thông tin profile được cập nhật và/hoặc mật khẩu mới được lưu.

Luồng chính:
1. Frontend tải thông tin user theo userId.
2. Saler chỉnh sửa profile và bấm lưu.
3. Frontend validate dữ liệu.
4. Frontend gọi PATCH /Auth/profile.
5. Frontend gọi refreshMe và hiển thị thành công.
6. Saler nhập thông tin đổi mật khẩu.
7. Frontend validate old/new/confirm.
8. Frontend gọi PATCH /Auth/password.
9. Frontend reset form và hiển thị thành công.

Luồng phụ:
1. Validate thất bại hoặc API lỗi.
2. Frontend hiển thị thông báo lỗi tương ứng.
