// Tạo database
// use food_market_narrator

// Tạo collection và index

// =======================================================================
db.createCollection("UserSessions");
db.UserSessions.createIndex(
  { device_id: 1 },
  { name: "ux_user_sessions_device_id", unique: true },
);
db.UserSessions.createIndex({ created_at: -1 });

// =======================================================================
db.createCollection("LocationLogs");

// index cho session
db.LocationLogs.createIndex({ session_id: 1 });

// index cho thời gian
db.LocationLogs.createIndex({ timestamp: -1 });

// 🔥 GEO INDEX (bắt buộc)
db.LocationLogs.createIndex({ location: "2dsphere" });

// =======================================================================
db.createCollection("AudioLogs");

// index theo session
db.AudioLogs.createIndex({ session_id: 1 });

// index theo POI
db.AudioLogs.createIndex({ restaurant_id: 1 });

// index theo thời gian
db.AudioLogs.createIndex({ timestamp: -1 });

// compound index (rất hữu ích)
db.AudioLogs.createIndex({ restaurant_id: 1, timestamp: -1 });

// use food_market_narrator

//  =======================================================================
db.createCollection("AudioTranslationVersions");
db.AudioTranslationVersions.createIndex(
  { audio_id: 1, target_language_code: 1, version_no: -1 },
  { name: "ix_atv_audio_lang_version" },
);
db.AudioTranslationVersions.createIndex(
  { audio_id: 1, target_language_code: 1, is_active: 1 },
  {
    name: "ux_atv_audio_lang_active",
    unique: true,
    partialFilterExpression: { is_active: true },
  },
);
db.AudioTranslationVersions.createIndex(
  { seller_user_id: 1, created_at: -1 },
  { name: "ix_atv_seller_created" },
);

// =======================================================================
db.createCollection("TranslationJobs");
db.TranslationJobs.createIndex(
  { request_id: 1 },
  { name: "ux_tj_request_id", unique: true },
);
db.TranslationJobs.createIndex(
  { seller_user_id: 1, created_at: -1 },
  { name: "ix_tj_seller_created" },
);
db.TranslationJobs.createIndex(
  { status: 1, created_at: -1 },
  { name: "ix_tj_status_created" },
);
db.TranslationJobs.createIndex(
  { audio_id: 1, target_language_code: 1, created_at: -1 },
  { name: "ix_tj_audio_lang_created" },
);

// =======================================================================
db.createCollection("TranslationUsageLedger");
db.TranslationUsageLedger.createIndex(
  { usage_event_id: 1 },
  { name: "ux_tul_usage_event_id", unique: true },
);
db.TranslationUsageLedger.createIndex(
  { request_id: 1 },
  { name: "ix_tul_request_id" },
);
db.TranslationUsageLedger.createIndex(
  { seller_user_id: 1, created_at: -1 },
  { name: "ix_tul_seller_created" },
);
db.TranslationUsageLedger.createIndex(
  { seller_user_id: 1, billing_month: 1 },
  { name: "ix_tul_seller_billing_month" },
);
db.TranslationUsageLedger.createIndex(
  { status: 1, created_at: -1 },
  { name: "ix_tul_status_created" },
);

// =======================================================================
db.createCollection("AudioUsageLedger");
db.AudioUsageLedger.createIndex(
  { usage_event_id: 1 },
  { name: "ux_aul_usage_event_id", unique: true },
);
db.AudioUsageLedger.createIndex(
  { request_id: 1 },
  { name: "ix_aul_request_id" },
);
db.AudioUsageLedger.createIndex(
  { seller_user_id: 1, created_at: -1 },
  { name: "ix_aul_seller_created" },
);
db.AudioUsageLedger.createIndex(
  { seller_user_id: 1, billing_month: 1 },
  { name: "ix_aul_seller_billing_month" },
);

// =======================================================================
db.createCollection("TranslationBillingMonthly");
db.TranslationBillingMonthly.createIndex(
  { seller_user_id: 1, billing_month: 1 },
  { name: "ux_tbm_seller_month", unique: true },
);
db.TranslationBillingMonthly.createIndex(
  { billing_month: 1, total_amount: -1 },
  { name: "ix_tbm_month_amount" },
);
