THƯ MỤC: Application/DTOs/

MỤC ĐÍCH:
- Chứa Data Transfer Objects (DTOs) - dùng để truyền dữ liệu
- Tách biệt dữ liệu hiển thị với domain entities
- Định hình dữ liệu gửi giữa client và server

NỘI DUNG:
- ProductDTO.cs - Dữ liệu sản phẩm cho client
- OrderDTO.cs - Dữ liệu đơn hàng
- CreateProductRequest.cs - Request tạo sản phẩm
- UpdateProductRequest.cs - Request cập nhật sản phẩm

LỢI ÍCH:
- Không lộ các trường nội bộ của entity
- Có thể customize dữ liệu trả về cho từng endpoint
- Dễ validate dữ liệu đầu vào
- Tách biệt giữa API contract và internal domain
