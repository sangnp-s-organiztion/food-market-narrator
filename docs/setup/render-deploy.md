# Deploy API Len Render (Cho Android That)

Tai lieu nay huong dan deploy `FoodMarketNarrator.Api` len Render thay vi dung ngrok.

## 1) Kien truc can co

API hien tai phu thuoc:

- SQL Server (du lieu nghiep vu)
- MongoDB (UserSessions, logs, analytics)

Luu y quan trong:

- Render khong cung cap managed SQL Server/Mongo free san co.
- De app chay that tren Android, ban can dung DB cloud ben ngoai:
  - SQL Server: Azure SQL / SQL Server cloud khac
  - MongoDB: MongoDB Atlas (M0 free tier)

## 2) Files da co san cho Render

- Dockerfile API: `FoodMarketNarrator.Api/Dockerfile`
- Blueprint: `render.yaml`

## 3) Tao service tren Render

1. Push code len GitHub.
2. Vao Render -> New -> Blueprint.
3. Chon repo co file `render.yaml`.
4. Render se tao service `food-market-narrator-api`.

Neu ban khong dung Blueprint, co the tao Web Service thu cong:

- Environment: Docker
- Dockerfile Path: `./FoodMarketNarrator.Api/Dockerfile`
- Docker Context: `.`
- Health Check Path: `/Mongo/test-connect`

## 4) Env vars bat buoc tren Render

Can dien toi thieu:

- `ConnectionStrings__DefaultConnection`
- `MongoDb__ConnectionString`
- `MongoDb__DatabaseName`

Vi du format:

- SQL Server:
  - `Server=tcp:<host>,1433;Database=food_market_narrator;User Id=<user>;Password=<pass>;TrustServerCertificate=True;Encrypt=True;`
- MongoDB:
  - `mongodb+srv://<user>:<pass>@<cluster>/<db>?retryWrites=true&w=majority`

Env vars khuyen nghi them:

- `LibreTranslate__BaseUrl`
- `EdgeTts__BaseUrl`
- `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromEmail`, `Smtp__FromName`

## 5) Sau khi deploy thanh cong

Gia su domain Render la:

- `https://food-market-narrator-api.onrender.com`

Kiem tra nhanh:

- `GET https://food-market-narrator-api.onrender.com/Mongo/test-connect`
- `GET https://food-market-narrator-api.onrender.com/Restaurant`

## 6) Cau hinh MAUI cho Android that

Mo file `FoodMarketNarrator.Maui/Settings/AppSettings.cs`:

- Gan `CloudApiBaseUrl` thanh domain Render (co https)

Vi du:

```csharp
private const string CloudApiBaseUrl = "https://food-market-narrator-api.onrender.com/";
```

Sau do build lai APK va cai tren may that.

## 7) Cau hinh QR cho flow mo app/tai APK

Noi dung QR nen la:

- `https://food-market-narrator-api.onrender.com/qr/open.html`

Trang nay se:

1. Ghi session vao `UserSessions`.
2. Thu mo app qua deep link.
3. Neu chua cai app, chuyen den `/qr/download.html`.

## 8) Luu y van hanh Render Free

- Free web service co the sleep khi khong co traffic -> lan mo dau co do tre.
- He thong file trong container la ephemeral:
  - file upload runtime (vd QR PNG upload tu admin) co the mat sau redeploy/restart.
  - nen dung object storage/CDN neu can luu ben vung.
