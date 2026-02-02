THƯ MỤC: Shared/Constants/

MỤC ĐÍCH:
- Chứa các hằng số, cấu hình toàn hệ thống
- Tập trung quản lý magic numbers, strings
- Dễ thay đổi cấu hình mà không cần tìm kiếm

NỘI DUNG:
- AppConstants.cs - Hằng số chung
- ValidationConstants.cs - Hằng số validation (min/max length, etc.)
- ErrorMessages.cs - Pesan lỗi
- DatabaseConstants.cs - Hằng số database
- ApiConstants.cs - Hằng số API (endpoints, headers, etc.)

VÍ DỤ:
- const int PASSWORD_MIN_LENGTH = 8
- const int PRODUCT_NAME_MAX_LENGTH = 255
- const string DEFAULT_CURRENCY = "VND"
- const int ITEMS_PER_PAGE = 20

QUI TẮC:
- Sử dụng const hoặc readonly
- Tên hằng số phải UPPERCASE với underscores
- Nhóm liên quan vào cùng file
