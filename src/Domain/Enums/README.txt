THƯ MỤC: Domain/Enums/

MỤC ĐÍCH:
- Chứa các enum định nghĩa các giá trị hằng
- Đảm bảo type-safety cho các trạng thái
- Tập trung quản lý các giá trị cố định

NỘI DUNG:
- OrderStatus.cs - enum trạng thái đơn hàng (Pending, Confirmed, Delivered)
- UserRole.cs - enum vai trò người dùng (Admin, Saler, Visitor)
- PaymentStatus.cs - enum trạng thái thanh toán
- ProductStatus.cs - enum trạng thái sản phẩm (Available, OutOfStock)

QUI TẮC:
- Tên enum phải rõ ràng, tính từ hoặc tính từ có hình dung
- Mỗi enum chịu trách nhiệm cho một khái niệm
- Sử dụng thay vì magic strings hoặc numbers
