# Admin Real API Integration — Design Spec

## Status

Approved for implementation.

## Mục tiêu

Chuyển toàn bộ admin frontend (`admin/`) từ dùng mock data sang dùng API thực từ backend. Tất cả 5 trang đều phải dùng API: Login, Dashboard, Nhà hàng, Người dùng, Nhật ký.

---

## 1. Backend: API mới

### 1.1 Bảng AuditLogs (EF Core migration)

Entity: `FoodMarketNarrator.Api/Models/AuditLog.cs`

```csharp
public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Action { get; set; }           // LOCK, UNLOCK, CREATE, UPDATE, DELETE, LOGIN, LOGOUT
    public string TargetType { get; set; }      // Restaurant, User, Audio, Dish, Image
    public string? TargetId { get; set; }
    public string? TargetName { get; set; }
    public string? Details { get; set; }         // JSON extra info
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 1.2 Middleware tự động ghi log

File: `FoodMarketNarrator.Api/Middleware/AuditLoggingMiddleware.cs`

- Áp dụng cho mọi request có authenticated user (principal not null)
- Skip: GET requests, static files, health checks
- Extract: HTTP method, route path, query params
- Map method + route → action label:
  - `POST` → `CREATE`
  - `PUT / PATCH` → `UPDATE`
  - `PATCH .../status` với body `isActive: false` → `LOCK`
  - `PATCH .../status` với body `isActive: true` → `UNLOCK`
  - `DELETE` → `DELETE`
- Extract target info từ route (e.g. `/Restaurant/5` → TargetType=Restaurant, TargetId=5)
- Ghi async vào DB — không block response
- Auth controller login/logout ghi riêng action=LOGIN/LOGOUT (không qua middleware)

### 1.3 Controller: AuditLogs

File: `FoodMarketNarrator.Api/Controllers/AuditLogsController.cs`

```
GET /api/audit-logs
  Query: page (default 1), pageSize (default 20), userId?, action?, targetType?, from?, to?
  Returns: { items: AuditLog[], totalCount: int, page: int, pageSize: int }
  Auth: [Authorize]
```

Response DTO: `AuditLogResponse { id, userId, username, action, targetType, targetId, targetName, details, ipAddress, createdAt }`

### 1.4 Controller: Analytics — entity counts

File: mở rộng `AnalyticsController.cs`

```
GET /api/analytics/entity-counts
  Returns: { totalRestaurants: int, totalAudios: int, totalUsers: int, totalDishes: int }
  Auth: [Authorize]
```

4 COUNT queries độc lập trong service. Không cần transaction vì counts không cần nhất quán hoàn toàn.

### 1.5 Controller: Analytics — listens timeseries

File: mở rộng `AnalyticsController.cs`

```
GET /api/analytics/listens-timeseries?days=14
  Query: days (default 14, max 90)
  Returns: { items: [{ date: "yyyy-MM-dd", listens: int }] }
  Auth: [Authorize]
```

Query từ bảng `AudioLogs` (nếu có trong DB). Nếu bảng không tồn tại hoặc empty, trả về empty array — frontend xử lý graceful.

---

## 2. Frontend Admin: Thay đổi từng trang

### 2.1 API client mới

File: `admin/src/lib/auditApi.ts` — gọi `GET /api/audit-logs`

### 2.2 Cập nhật analyticsApi.ts

Thêm:

- `getEntityCounts(): Promise<EntityCountsResponse>`
- `getListensTimeseries(days: number): Promise<ListensTimeseriesResponse>`

### 2.3 Dashboard — Index.tsx

**Entity stat cards (thay mock):**

```typescript
// Trước: dùng mockData.restaurants.length, hardcoded 8, 4, 10
// Sau: useQuery(['entity-counts'], () => analyticsApi.getEntityCounts())
```

**Daily listens chart (thay inline static):**

```typescript
// Trước: hardcoded 14-item array
// Sau: useQuery(['listens-timeseries', 14], () => analyticsApi.getListensTimeseries(14))
```

**Heatmap POI markers:**

```typescript
// Trước: fallback mockData khi restaurantPois empty
// Sau: dùng restaurant list từ useQuery(['restaurants']) để lấy lat/lng
```

### 2.4 Logs Page — LogsPage.tsx

- Thay `GET /api/analytics/recent-activity` bằng `GET /api/audit-logs`
- Map action → label tiếng Việt: LOCK→"Khóa", UNLOCK→"Mở khóa", CREATE→"Tạo mới", UPDATE→"Cập nhật", DELETE→"Xóa", LOGIN→"Đăng nhập", LOGOUT→"Đăng xuất"
- Pagination UI (page + pageSize controls)
- Filter controls: filter theo userId, action, targetType, date range
- Auto-refresh mỗi 30s giữ nguyên

### 2.5 Xóa mock data

- Xóa `admin/src/lib/mockData.ts`
- Xóa import `mockData` khỏi `HeatmapSection.tsx`, `StatusBadge.tsx` (nếu còn)
- Xóa `admin/src/lib/adminLogs.ts` (thay bằng `auditApi.ts`)

---

## 3. Error Handling

| Tình huống | Hành vi |
|-----------|---------|
| Entity counts API fail | Stat card hiển thị "—" |
| Timeseries API fail/empty | Chart hiển thị empty state |
| Audit logs API fail | Logs page hiển thị error message + retry button |
| Audit logs empty | Empty state message |

---

## 4. Test Plan

### Backend
- Middleware không ghi log cho GET request
- Middleware ghi đúng action với mỗi HTTP method
- AuditLogs endpoint pagination + filter đúng
- Entity counts trả về số đúng
- Listens timeseries trả về empty array nếu không có data

### Frontend
- Dashboard stat cards hiển thị số từ API
- Dashboard chart hiển thị data từ timeseries API
- Logs page hiển thị audit log với action label đúng
- Logs page pagination hoạt động
- Không còn import mockData ở bất kỳ đâu

---

## 5. Files to Modify

### Backend (FoodMarketNarrator.Api)
- `Models/AuditLog.cs` — **new**
- `Data/AppDbContext.cs` — thêm DbSet<AuditLog>
- `Migrations/` — **new migration**
- `Middleware/AuditLoggingMiddleware.cs` — **new**
- `Controllers/AuditLogsController.cs` — **new**
- `Controllers/AnalyticsController.cs` — thêm 2 endpoints
- `Services/IAnalyticsService.cs` + `AnalyticsService.cs` — thêm 2 methods

### Frontend (admin/)
- `src/lib/auditApi.ts` — **new**
- `src/lib/analyticsApi.ts` — thêm 2 methods
- `src/types/analytics.ts` — thêm DTO types
- `src/pages/Index.tsx` — thay mock bằng API
- `src/pages/LogsPage.tsx` — dùng audit-logs API
- `src/components/HeatmapSection.tsx` — xóa mock fallback
- `src/lib/mockData.ts` — **xóa**
- `src/lib/adminLogs.ts` — **xóa**

---

## 6. Out of Scope

- Không thay đổi API contract đã có (Restaurant, Users, Auth)
- Không sửa mobile app
- Không sửa saler frontend
- Không thêm unit test (sẽ verify bằng manual test)
