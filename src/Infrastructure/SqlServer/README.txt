THƯ MỤC: Infrastructure/SqlServer/

MỤC ĐÍCH:
- Chứa cấu hình và context cho SQL Server
- Entity Framework Core DbContext
- Migration files cho database schema

NỘI DUNG:
- FoodMarketDbContext.cs - DbContext chính
- Migrations/ - Folder chứa migration files
- Configurations/ - Entity type configurations
- Seeds/ - Dữ liệu mẫu ban đầu

VÍ DỤ:
- ProductConfiguration.cs - Cấu hình Entity Product
- StallConfiguration.cs - Cấu hình Entity Stall
- InitialDataSeed.cs - Dữ liệu ban đầu

GHI CHÚ:
- Sử dụng Entity Framework Core
- Migrations được tạo bằng: dotnet ef migrations add {MigrationName}
- Cập nhật database: dotnet ef database update
