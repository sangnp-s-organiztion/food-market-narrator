THƯ MỤC: Domain/ValueObjects/

MỤC ĐÍCH:
- Chứa các Value Objects - các object mô tả giá trị
- Không có Identity, chỉ quan trọng giá trị
- Immutable và reusable

NỘI DUNG:
- Money.cs - Đối tượng tiền tệ (amount, currency)
- Address.cs - Địa chỉ (street, city, zipcode)
- Email.cs - Email với validation
- PhoneNumber.cs - Số điện thoại
- Rating.cs - Đánh giá (score, count)

ĐẶC ĐIỂM:
- Không có ID
- Không thay đổi được (Immutable)
- Hai value objects bằng nhau nếu giá trị bằng nhau
- Có thể nhúng trong Entity hoặc DTO
