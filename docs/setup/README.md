# ⚠️ IMPORTANT NOTE

## 🔐 Credentials Usage Notice

Tất cả **username**, **password**, và **connection string** trong project này:

👉 **CHỈ dùng cho môi trường LOCAL (development)**

---

## 🚫 KHÔNG sử dụng trong production

Các thông tin như:

- MongoDB username/password
- MSSQL username/password
- API keys (nếu có)

**KHÔNG được sử dụng khi deploy thật**

---

## ⚙️ Khi deploy production cần:

- Thay đổi toàn bộ credentials
- Sử dụng biến môi trường (environment variables)
- Không hardcode thông tin nhạy cảm trong code
- Sử dụng secret manager (nếu có)

---

## 🧪 Ví dụ (LOCAL ONLY)

```text
MongoDB:
mongodb://admin:root%401133@localhost:27017/?authSource=admin

MSSQL:
Server=localhost,1433;Database=food_market_narrator;User Id=sa;Password=YourStrong!Pass123;
```

---

## ✅ Best Practices

- Dùng `.env` hoặc `appsettings.Development.json` cho local
- Dùng `appsettings.Production.json` hoặc environment variables cho production
- Không commit credentials thật lên Git

---

## 📌 Kết luận

👉 Toàn bộ credentials hiện tại = **chỉ để test local**
👉 Deploy thật = **bắt buộc thay đổi**

---

END OF DOCUMENT
