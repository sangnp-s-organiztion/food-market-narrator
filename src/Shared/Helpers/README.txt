THƯ MỤC: Shared/Helpers/

MỤC ĐÍCH:
- Chứa các helper class, extension methods
- Làm giàu thêm chức năng cho các type
- Xử lý các tác vụ nhỏ lặp lại

NỘI DUNG:
- EnumHelper.cs - Công cụ làm việc với Enum
- ExceptionHelper.cs - Xử lý exception
- LoggerHelper.cs - Helper cho logging
- HttpHelper.cs - Helper cho HTTP requests
- MappingHelper.cs - Mapping giữa objects

VÍ DỤ:
- public static List<T> GetEnumValues<T>()
- public static string GetEnumDescription(Enum value)
- public static void LogException(Exception ex, ILogger logger)
- public static string GetErrorMessage(Exception ex)

QUI TẮC:
- Các static utility methods
- Có thể là extension methods
- Phục vụ một mục đích cụ thể
