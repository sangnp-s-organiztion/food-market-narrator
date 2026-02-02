THƯ MỤC: database/sqlserver/

MỤC ĐÍCH:
- Chứa các SQL script cho SQL Server
- Migration files, stored procedures
- Dữ liệu mẫu ban đầu

NỘI DUNG:
- Migrations/ - EF Core migrations
- Procedures/ - Stored procedures
- Functions/ - Functions
- Seeds/ - Dữ liệu ban đầu
- Schema/ - Định nghĩa schema

VÍ DỤ:
- 001_CreateProductTable.sql
- 002_CreateStallTable.sql
- CreateIndexes.sql
- InsertInitialData.sql

GHI CHÚ:
- Migrations được tạo tự động bởi EF Core
- Có thể viết stored procedures để tối ưu hiệu năng
- Giữ backup dữ liệu quan trọng
