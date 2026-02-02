THƯ MỤC: Infrastructure/MongoDB/

MỤC ĐÍCH:
- Chứa cấu hình và context cho MongoDB
- MongoDB connection và configuration
- Collection definitions nếu sử dụng MongoDB

NỘI DUNG:
- MongoContext.cs - MongoDB connection context
- Collections/ - Định nghĩa collections
- Configurations/ - Cấu hình collection
- Seeds/ - Dữ liệu mẫu ban đầu

VÍ DỤ:
- NarrationCollection.cs - Collection thuyết minh
- LogCollection.cs - Collection log
- InitialDataSeed.cs - Dữ liệu ban đầu

GHI CHÚ:
- Sử dụng MongoDB driver hoặc MongoDB.Entities
- NoSQL schema - linh hoạt hơn SQL Server
- Thích hợp cho lưu trữ dữ liệu không có cấu trúc
