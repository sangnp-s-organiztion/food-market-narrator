# Troubleshooting

## API không truy cập được từ Android thật

- Kiểm tra `LocalApiHost` trong `FoodMarketNarrator.Maui/Settings/AppSettings.cs`.
- Kiểm tra phone và máy backend cùng LAN.
- Mở firewall port 5044/7041.

## Mongo test-connect fail

- Kiểm tra container Mongo đang chạy port 27017.
- Kiểm tra username/password và `authSource=admin`.
- Gọi lại `GET /Mongo/test-connect` sau khi restart API.

## MAUI không build

- Kiểm tra tên file trong `Resources/Images` chỉ dùng chữ thường theo quy tắc MAUI resizetizer.
- Chạy lại `dotnet restore` và `dotnet build`.
