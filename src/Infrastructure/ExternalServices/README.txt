THƯ MỤC: Infrastructure/ExternalServices/

MỤC ĐÍCH:
- Chứa các service gọi API bên ngoài
- Tích hợp với dịch vụ external như TTS, payment, email
- Cô lập các dependency bên ngoài

NỘI DUNG:
- TtsService.cs - Gọi Text-to-Speech API (Azure, Google, etc.)
- EmailService.cs - Gửi email
- SmsService.cs - Gửi SMS
- PaymentService.cs - Tích hợp thanh toán
- StorageService.cs - Upload file lên cloud

VÍ DỤ:
- AzureTtsService.cs - Implement TTS bằng Azure
- GmailSmtpService.cs - Gửi email qua Gmail
- CloudinaryStorageService.cs - Upload lên Cloudinary

GHI CHÚ:
- Mỗi service bên ngoài nên có interface riêng
- Xử lý exception từ API external
- Implement retry logic nếu cần
