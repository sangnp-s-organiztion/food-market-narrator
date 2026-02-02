# Narration Automated Food Market

## 📌 Giới thiệu

**Narration Automated Food Market** là một dự án mô phỏng hệ thống chợ thực phẩm tự động, lấy cảm hứng từ phố ẩm thực Vĩnh Khánh. Dự án tập trung vào việc **tự động hóa quy trình mua bán, quản lý sản phẩm của các gian hàng ẩm thực, đồng thời tích hợp tính năng thuyết minh (narration) tự động nhằm giới thiệu món ăn, gian hàng và đặc trưng ẩm thực đến người dùng.**

Dự án hướng tới mục tiêu:
* Giảm sự can thiệp thủ công trong quản lý chợ thực phẩm
* Tăng trải nghiệm người dùng thông qua tự động hóa
* Làm nền tảng học tập & mở rộng cho các bài toán thực tế (web backend, database, system design)

---

## 🚀 Tính năng chính

* 🛒 Quản lý sản phẩm thực phẩm (thêm / sửa / xóa / xem)
* 📦 Quản lý danh mục (category)
* 💰 Hiển thị giá và thông tin sản phẩm
* 🤖 Tự động hóa quy trình xử lý dữ liệu
* 🔊 Text-to-Speech narration cho sản phẩm
* 🔐 Phân quyền người dùng (Visitor / Saler / Admin)

---

## 🛠️ Công nghệ sử dụng

* **Backend**: C# – ASP.NET Core 10.0 (Web API)
* **Database**: Microsoft SQL Server + MongoDB
* **ORM / Data Access**: Entity Framework Core
* **TTS**: Azure Speech Services / OpenAI TTS
* **Tools**: Git, GitHub, VS Code, SSMS

---

## 📂 Cấu trúc thư mục

```
├─ src/                          # Source code
│  ├─ Presentation/              # UI/API Controllers
│  │  ├─ Visitor/               # Customer endpoints
│  │  ├─ Saler/                 # Seller portal
│  │  └─ Admin/                 # Admin dashboard
│  │
│  ├─ Application/               # Business logic
│  │  ├─ Services/              # Application services
│  │  ├─ DTOs/                  # Data transfer objects
│  │  └─ Interfaces/            # Service contracts
│  │
│  ├─ Domain/                    # Domain models
│  │  ├─ Entities/              # Domain entities
│  │  ├─ Enums/                 # Enumerations
│  │  └─ ValueObjects/          # Value objects
│  │
│  ├─ Infrastructure/            # Data access & external services
│  │  ├─ SqlServer/             # SQL Server implementation
│  │  ├─ MongoDB/               # MongoDB implementation
│  │  ├─ Repositories/          # Repository implementations
│  │  └─ ExternalServices/      # TTS, API integration
│  │
│  └─ Shared/                    # Utilities & helpers
│     ├─ Utils/
│     ├─ Constants/
│     └─ Helpers/
│
├─ database/                     # Database scripts
│  ├─ sqlserver/                # SQL Server migrations
│  └─ mongodb/                  # MongoDB schemas
│
├─ narration/                    # Narration & TTS
│  ├─ scripts/                  # Narration scripts
│  ├─ audio/                    # Generated audio
│  └─ tts/                      # TTS service
│
├─ docs/                         # Documentation
│  ├─ report/
│  ├─ diagrams/
│  └─ api/
│
├─ tests/                        # Test projects
├─ .github/workflows/            # CI/CD pipelines
├─ README.md
└─ CONTRIBUTING.md
```

---

## ⚙️ Cài đặt & Chạy dự án

### 1️⃣ Clone project

```bash
git clone https://github.com/<username>/narration-automated-food-market.git
cd narration-automated-food-market
```

### 2️⃣ Cấu hình database

* Tạo database mới
* Import file SQL trong thư mục `database/sqlserver/`
* Cập nhật connection string

### 3️⃣ Chạy backend

```bash
dotnet build
dotnet run
```

### 4️⃣ Chạy tests

```bash
dotnet test
```

---

## 🤝 Contributing

Xem [CONTRIBUTING.md](CONTRIBUTING.md) để biết hướng dẫn đóng góp.

---

## 👤 Tác giả

* **Nguyễn Phước Sang** - [GitHub](https://github.com/NguyenPhuocSang1695)
* **Nguyễn Gia Thiệu**

---

## 📄 License

Dự án được phát triển cho mục đích học tập.

---

✨ *Feel free to fork, improve and contribute!*
