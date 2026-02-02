THƯ MỤC: Infrastructure/Repositories/

MỤC ĐÍCH:
- Chứa Repository Pattern implementation
- Lớp truy cập dữ liệu (Data Access Layer)
- Tách biệt logic truy vấn database với business logic

NỘI DUNG:
- ProductRepository.cs - CRUD cho Product
- StallRepository.cs - CRUD cho Stall
- OrderRepository.cs - CRUD cho Order
- GenericRepository.cs - Base class cho repositories

PHƯƠNG THỨC CHÍNH:
- GetAll() - Lấy tất cả
- GetById(id) - Lấy theo ID
- Add(entity) - Thêm mới
- Update(entity) - Cập nhật
- Delete(id) - Xóa
- SaveChanges() - Lưu thay đổi

QUI TẮC:
- Một repository cho một entity chính
- Sử dụng DbContext từ Infrastructure/SqlServer
- Không chứa business logic, chỉ truy vấn
- Trả về domain entities, không DTOs
