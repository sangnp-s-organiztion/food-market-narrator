THƯ MỤC: Application/Services/

MỤC ĐÍCH:
- Chứa các service xử lý logic nghiệp vụ chính
- Điểm kết nối giữa Presentation và Domain/Infrastructure
- Thực hiện các công việc business như tính giá, tìm kiếm, xử lý đơn hàng

NỘI DUNG:
- ProductService.cs - Logic xử lý sản phẩm
- OrderService.cs - Logic xử lý đơn hàng
- NarrationService.cs - Logic thuyết minh sản phẩm
- AuthService.cs - Logic xác thực người dùng

QUI TẮC:
- Một service chịu trách nhiệm cho một lĩnh vực (SRP)
- Sử dụng interface (Interfaces/) để định nghĩa contract
- Gọi Repository để truy cập dữ liệu
- Không trực tiếp truy cập database
