/*
1. Collection UserSessions (Quản lý phiên người dùng)
{
  "_id": ObjectId("..."),
  "device_id": "uuid-hoac-fingerprint", 
  "browser_info": {
    "os": "iOS",
    "version": "16.5"
  },
  "created_at": ISODate("2023-10-27T10:00:00Z")
}

2. Collection LocationLogs (Lưu tuyến di chuyển & Heatmap)
{
  "_id": ObjectId("..."),
  "session_id": ObjectId("..."), // Liên kết với UserSessions
  "location": {
    "type": "Point",
    "coordinates": [106.701, 10.762] // [Kinh độ (Long), Vĩ độ (Lat)] - Lưu ý: MongoDB yêu cầu Long đứng trước
  },
  "timestamp": ISODate("2023-10-27T10:05:00Z")
}

3. Collection AudioLogs (Top địa điểm & Thời gian nghe)
{
  "_id": ObjectId("..."),
  "session_id": ObjectId("..."),
  "restaurant_id": "R001", // ID từ SQL của bạn
  "audio_id": 10,
  "action": "play", // start, pause, end
  "start_time": ISODate("2023-10-27T10:10:00Z"),
  "end_time": ISODate("2023-10-27T10:12:30Z"),
  "duration": 150, // thời gian nghe tính bằng giây
  "timestamp": ISODate("2023-10-27T10:10:00Z")
}

*/
