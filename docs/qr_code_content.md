# QR Code Content

## Overview

QR code được sử dụng để mở ứng dụng **Food Market Narrator** thông qua một **custom URL scheme**.

## QR Code Data

Nội dung được mã hóa trong QR code:

```
foodmarketnarrator://open
```

## Required Application

Để quét mã QR, người dùng cần sử dụng ứng dụng **Trình quét QR và mã vạch** trên CH PLay.

## How It Works

Quy trình hoạt động:

1. Người dùng mở **ứng dụng QR scanner** trên điện thoại.
2. Hướng camera vào mã QR được đặt tại địa điểm.
3. Ứng dụng scanner đọc nội dung QR code.
4. Hệ thống nhận diện URL scheme `foodmarketnarrator://`.
5. Nếu ứng dụng **Food Market Narrator** đã được cài đặt trên thiết bị:
   - Hệ điều hành sẽ mở ứng dụng tự động.

6. Nếu ứng dụng chưa được cài đặt:

- Liên kết sẽ không thể được xử lý.

## Use Case

QR code được đặt tại các vị trí trong khu chợ để người dùng có thể:

- Quét mã bằng ứng dụng QR scanner
- Mở ứng dụng **Food Market Narrator**
- Truy cập nhanh vào hệ thống hướng dẫn hoặc thông tin địa điểm

## Example Scenario

1. Người dùng mở ứng dụng QR scanner.
2. Quét mã QR tại quầy hàng.
3. Điện thoại hiển thị liên kết:

```
foodmarketnarrator://open
```

4. Người dùng nhấn vào liên kết.
5. Ứng dụng **Food Market Narrator** được mở.
