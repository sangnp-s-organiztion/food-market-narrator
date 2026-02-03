FEATURES – WEB & APP THUYẾT MINH PHỐ ẨM THỰC VĨNH KHÁNH
====================================================

I. CHỨC NĂNG CHUNG
-----------------
- Hệ thống gồm 2 nền tảng:
  + Web: dành cho Admin, Seller.
  + Mobile App: dành cho Visitor để trải nghiệm thuyết minh tự động theo vị trí GPS và truy cập thông tin.

- Dữ liệu thuyết minh hỗ trợ nhiều ngôn ngữ (Việt, Anh, …).
- Mỗi gian hàng có nội dung thuyết minh riêng (audio/TTS).

----------------------------------------------------

II. CHỨC NĂNG TRANG WEB (WEB APPLICATION)
----------------------------------------

1. Hiển thị mã QR
- Khi người dùng truy cập website lần đầu, hệ thống hiển thị mã QR.
- Mã QR dùng để:
  + Tải ứng dụng mobile cho Visitor
  + Hoặc mở ứng dụng nếu đã cài đặt

2. Xem thông tin phố ẩm thực
- Hiển thị thông tin tổng quan về Phố Ẩm Thực Vĩnh Khánh:
  + Giới thiệu
  + Bản đồ khu vực
  + Danh sách các gian hàng

3. Xem gian hàng & món ăn (Chế độ thông thường)
- Cho phép người dùng:
  + Xem danh sách gian hàng
  + Xem chi tiết gian hàng (tên, mô tả, hình ảnh, món ăn)
- Áp dụng cho trường hợp:
  + Visitor chưa ở Phố Ẩm Thực Vĩnh Khánh
  + Hoặc chỉ muốn tham khảo thông tin

4. Quản trị hệ thống (Admin)
- Quản lý người dùng (Seller, Admin)
- Quản lý gian hàng
- Duyệt nội dung thuyết minh (audio/TTS)
- Quản lý ngôn ngữ thuyết minh
- Xem thống kê lượt nghe thuyết minh

5. Quản lý gian hàng (Seller)
- Cập nhật thông tin gian hàng
- Thêm / chỉnh sửa món ăn
- Thêm nội dung thuyết minh theo từng ngôn ngữ
- Gửi nội dung để Admin duyệt

----------------------------------------------------

III. CHỨC NĂNG APP VISITOR (MOBILE APPLICATION)
----------------------------------------------

1. Quét QR & khởi tạo trải nghiệm
- Visitor quét mã QR từ website để truy cập hoặc cài đặt app.
- Sau khi mở app, visitor chọn ngôn ngữ thuyết minh.

2. Xem thông tin gian hàng (Ngoài khu vực phố ẩm thực)
- Khi Visitor KHÔNG ở khu vực Phố Ẩm Thực Vĩnh Khánh:
  + App hoạt động như một ứng dụng tra cứu thông tin
  + Có thể xem danh sách gian hàng, món ăn, nội dung mô tả
  + Không tự động phát audio

3. Định vị GPS & Geofencing
- App sử dụng GPS để theo dõi vị trí Visitor (cập nhật định kỳ).
- Mỗi gian hàng được thiết lập một vùng bán kính (geofence).
- Hệ thống xác định gian hàng gần nhất và ưu tiên cao nhất.

4. Thuyết minh tự động theo vị trí
- Khi Visitor ở trong khu vực Phố Ẩm Thực Vĩnh Khánh:
  + App tự động kích hoạt chế độ thuyết minh
  + Khi Visitor đi vào vùng bán kính của gian hàng:
      * Tự động phát audio thuyết minh của gian hàng đó
      * Audio phát theo ngôn ngữ Visitor đã chọn
  + Khi Visitor di chuyển sang gian hàng khác:
      * Dừng audio cũ
      * Phát audio của gian hàng mới

5. Chống phát lặp & quản lý trạng thái
- Hệ thống ghi nhận gian hàng đã phát audio.
- Không phát lại audio liên tục khi Visitor đứng yên.
- Chỉ phát lại khi:
  + Visitor rời khỏi gian hàng một khoảng thời gian nhất định
  + Hoặc quay lại gian hàng sau khi đã đi sang vị trí khác

6. Hỗ trợ Online / Offline
- App lưu dữ liệu POI (gian hàng, audio) ở chế độ offline.
- Khi có mạng:
  + Đồng bộ dữ liệu mới từ server
  + Cập nhật nội dung thuyết minh

----------------------------------------------------

IV. TÍNH NĂNG NÂNG CAO
---------------------
- Hỗ trợ Text-To-Speech (TTS) hoặc file audio có sẵn.
- Cho phép điều chỉnh:
  + Độ nhạy GPS
  + Bán kính kích hoạt thuyết minh
- Ghi log lượt nghe để phục vụ thống kê và cải tiến nội dung.

====================================================