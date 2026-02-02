THƯ MỤC: Application/Interfaces/

MỤC ĐÍCH:
- Định nghĩa các interface/contract cho các service
- Cho phép dependency injection và loose coupling
- Dễ test bằng cách mock các interface

NỘI DUNG:
- IProductService.cs - Interface cho ProductService
- IOrderService.cs - Interface cho OrderService
- INarrationService.cs - Interface cho NarrationService
- IRepository.cs - Interface chung cho repositories

QUI TẮC:
- Mọi service phải có interface tương ứng
- Interface định nghĩa các method public của service
- Chỉ expose những method cần dùng bên ngoài
