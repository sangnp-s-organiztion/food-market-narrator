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

[
  { "_id": {"$oid": "65432101abcdef0123456781"}, "device_info": "iPhone 14, iOS 17", "created_at": {"$date": "2026-03-12T08:00:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456782"}, "device_info": "Samsung S23, Android 14", "created_at": {"$date": "2026-03-12T08:05:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456783"}, "device_info": "Xiaomi 13, Android 13", "created_at": {"$date": "2026-03-12T08:10:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456784"}, "device_info": "iPhone 15 Pro", "created_at": {"$date": "2026-03-12T08:15:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456785"}, "device_info": "Oppo Reno 10", "created_at": {"$date": "2026-03-12T08:20:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456786"}, "device_info": "iPhone 13", "created_at": {"$date": "2026-03-12T09:00:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456787"}, "device_info": "Google Pixel 7", "created_at": {"$date": "2026-03-12T09:10:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456788"}, "device_info": "iPad Air 5", "created_at": {"$date": "2026-03-12T09:15:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456789"}, "device_info": "iPhone 12", "created_at": {"$date": "2026-03-12T10:00:00Z"} },
  { "_id": {"$oid": "65432101abcdef0123456790"}, "device_info": "Samsung Fold 5", "created_at": {"$date": "2026-03-12T10:30:00Z"} }
]

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

  [
  { "session_id": {"$oid": "65432101abcdef0123456781"}, "location": { "type": "Point", "coordinates": [106.701279, 10.764057] }, "timestamp": {"$date": "2026-03-12T08:01:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456781"}, "location": { "type": "Point", "coordinates": [106.702051, 10.763402] }, "timestamp": {"$date": "2026-03-12T08:03:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456781"}, "location": { "type": "Point", "coordinates": [106.702372, 10.761733] }, "timestamp": {"$date": "2026-03-12T08:05:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456781"}, "location": { "type": "Point", "coordinates": [106.702700, 10.761408] }, "timestamp": {"$date": "2026-03-12T08:07:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456782"}, "location": { "type": "Point", "coordinates": [106.703631, 10.760703] }, "timestamp": {"$date": "2026-03-12T08:06:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456782"}, "location": { "type": "Point", "coordinates": [106.704322, 10.760798] }, "timestamp": {"$date": "2026-03-12T08:08:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456783"}, "location": { "type": "Point", "coordinates": [106.705417, 10.761126] }, "timestamp": {"$date": "2026-03-12T08:12:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456784"}, "location": { "type": "Point", "coordinates": [106.705702, 10.761183] }, "timestamp": {"$date": "2026-03-12T08:16:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456785"}, "location": { "type": "Point", "coordinates": [106.703051, 10.761168] }, "timestamp": {"$date": "2026-03-12T08:22:00Z"} },
  { "session_id": {"$oid": "65432101abcdef0123456786"}, "location": { "type": "Point", "coordinates": [106.702620, 10.761242] }, "timestamp": {"$date": "2026-03-12T09:05:00Z"} }
]

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

  [
  { "session_id": {"$oid": "65432101abcdef0123456781"}, "restaurant_id": "oc-loan", "audio_id": 8, "start_time": {"$date": "2026-03-12T08:05:00Z"}, "end_time": {"$date": "2026-03-12T08:07:30Z"}, "duration": 150 },
  { "session_id": {"$oid": "65432101abcdef0123456782"}, "restaurant_id": "oc-oanh", "audio_id": 9, "start_time": {"$date": "2026-03-12T08:10:00Z"}, "end_time": {"$date": "2026-03-12T08:12:00Z"}, "duration": 120 },
  { "session_id": {"$oid": "65432101abcdef0123456783"}, "restaurant_id": "oc-phat", "audio_id": 10, "start_time": {"$date": "2026-03-12T08:15:00Z"}, "end_time": {"$date": "2026-03-12T08:19:00Z"}, "duration": 240 },
  { "session_id": {"$oid": "65432101abcdef0123456784"}, "restaurant_id": "oc-loan", "audio_id": 8, "start_time": {"$date": "2026-03-12T08:20:00Z"}, "end_time": {"$date": "2026-03-12T08:22:30Z"}, "duration": 150 },
  { "session_id": {"$oid": "65432101abcdef0123456785"}, "restaurant_id": "chilli-bbq-hotpot-restaurant", "audio_id": 1, "start_time": {"$date": "2026-03-12T08:25:00Z"}, "end_time": {"$date": "2026-03-12T08:26:30Z"}, "duration": 90 },
  { "session_id": {"$oid": "65432101abcdef0123456786"}, "restaurant_id": "quan-oc-vu", "audio_id": 13, "start_time": {"$date": "2026-03-12T09:10:00Z"}, "end_time": {"$date": "2026-03-12T09:14:00Z"}, "duration": 240 },
  { "session_id": {"$oid": "65432101abcdef0123456787"}, "restaurant_id": "the-gioi-bo", "audio_id": 15, "start_time": {"$date": "2026-03-12T09:20:00Z"}, "end_time": {"$date": "2026-03-12T09:23:00Z"}, "duration": 180 },
  { "session_id": {"$oid": "65432101abcdef0123456788"}, "restaurant_id": "oc-loan", "audio_id": 8, "start_time": {"$date": "2026-03-12T09:30:00Z"}, "end_time": {"$date": "2026-03-12T09:33:00Z"}, "duration": 180 },
  { "session_id": {"$oid": "65432101abcdef0123456789"}, "restaurant_id": "lau-met-nuong-79k", "audio_id": 3, "start_time": {"$date": "2026-03-12T10:10:00Z"}, "end_time": {"$date": "2026-03-12T10:12:00Z"}, "duration": 120 },
  { "session_id": {"$oid": "65432101abcdef0123456790"}, "restaurant_id": "oc-oanh", "audio_id": 9, "start_time": {"$date": "2026-03-12T10:40:00Z"}, "end_time": {"$date": "2026-03-12T10:43:00Z"}, "duration": 180 }
]

*/
