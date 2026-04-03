// Tạo database
// use food_market_narrator

// Tạo collection và index

// ===================================================================
db.createCollection("UserSessions");
db.UserSessions.createIndex(
  { device_id: 1 },
  { name: "ux_user_sessions_device_id", unique: true },
);
db.UserSessions.createIndex({ created_at: -1 });
// ===================================================================
db.createCollection("LocationLogs");

// index cho session
db.LocationLogs.createIndex({ session_id: 1 });

// index cho thời gian
db.LocationLogs.createIndex({ timestamp: -1 });

// 🔥 GEO INDEX (bắt buộc)
db.LocationLogs.createIndex({ location: "2dsphere" });

// ===================================================================
db.createCollection("AudioLogs");

// index theo session
db.AudioLogs.createIndex({ session_id: 1 });

// index theo POI
db.AudioLogs.createIndex({ restaurant_id: 1 });

// index theo thời gian
db.AudioLogs.createIndex({ timestamp: -1 });

// compound index (rất hữu ích)
db.AudioLogs.createIndex({ restaurant_id: 1, timestamp: -1 });
