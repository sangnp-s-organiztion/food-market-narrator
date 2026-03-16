## AI Rules

Tài liệu này định nghĩa nguyên tắc làm việc của AI trong dự án Food Market Narrator.

## 1) Nguyên tắc chung

- Ưu tiên code chạy được ngay trong môi trường hiện tại, không để pseudo code.
- Luôn đọc code hiện có trước khi sửa để bám đúng pattern sẵn có.
- Ưu tiên sửa file hiện hữu thay vì tạo mới trừ khi thật sự cần.
- Không thay đổi kiến trúc hoặc API contract nếu chưa có yêu cầu rõ ràng.
- Không thêm thư viện mới nếu có thể giải quyết bằng stack đang dùng.

## 2) Cách thực hiện thay đổi

- Mỗi thay đổi cần nêu rõ:
  - File bị ảnh hưởng.
  - Nội dung thay đổi chính.
  - Lý do thay đổi.
- Giữ phạm vi sửa nhỏ nhất có thể để giảm rủi ro regression.
- Tránh reformat hoặc đổi tên không liên quan tới yêu cầu.

## 3) Quy tắc theo ngữ cảnh dự án

- Backend: ASP.NET Web API + Cookie Auth + SQL Server.
- Mobile: .NET MAUI, ưu tiên ổn định tracking/location và narration flow.
- Frontend saler: React + TypeScript + Vite.

Khi sửa khác layer, cần kiểm tra tác động qua API contract trước khi kết luận hoàn tất.

## 4) Chất lượng và xác minh

- Nếu có thể, chạy kiểm tra nhanh sau khi sửa (build/lint/test tương ứng).
- Nếu không chạy được, phải nói rõ lý do và rủi ro còn lại.
- Tài liệu cập nhật phải phản ánh đúng trạng thái code hiện tại.
