THƯ MỤC: Domain/Entities/

MỤC ĐÍCH:
- Chứa các entity (lớp mô hình chính)
- Đại diện cho các khái niệm trong miền kinh doanh
- Chứa business logic và rules của miền

NỘI DUNG:
- Product.cs - Entity sản phẩm
- Stall.cs - Entity gian hàng
- Order.cs - Entity đơn hàng
- User.cs - Entity người dùng
- Category.cs - Entity danh mục

NHẬN XÉT:
- Các entity này tương ứng với bảng trong database
- Chứa các method xử lý logic nghiệp vụ của entity
- Không phụ thuộc vào framework hoặc database
- Có thể có các method như Validate(), GetPrice(), etc.
