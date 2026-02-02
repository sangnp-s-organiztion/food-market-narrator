THƯ MỤC: Shared/Utils/

MỤC ĐÍCH:
- Chứa các hàm utility, helper function dùng chung
- Tối đa hóa tái sử dụng code
- Các công cụ không phụ thuộc vào logic nghiệp vụ

NỘI DUNG:
- DateTimeHelper.cs - Xử lý ngày giờ
- StringHelper.cs - Xử lý string (slug, truncate, etc.)
- PaginationHelper.cs - Xử lý pagination
- EncryptionHelper.cs - Encrypt/Decrypt
- ValidationHelper.cs - Validate input
- FileHelper.cs - Xử lý file

VÍ DỤ:
- public static string ToSlug(string text)
- public static string Truncate(string text, int length)
- public static PagedResult<T> Paginate<T>(List<T> items, int page, int pageSize)

QUI TẮC:
- Phải là static methods
- Không phụ thuộc vào state
- Tái sử dụng được ở nhiều nơi
