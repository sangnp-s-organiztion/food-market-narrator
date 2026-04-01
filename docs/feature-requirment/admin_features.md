# Admin Panel — Feature Specification

## Overview

The Admin Panel (`/admin`) is a React + TypeScript + Vite web application used by system administrators to manage restaurants, users, and monitor system activity.

**Tech Stack:**
- React 18 + TypeScript
- Vite (build tool)
- Tailwind CSS + shadcn/ui components
- React Router v6 (routing)
- TanStack Query (data fetching, prepared)
- Leaflet + React-Leaflet (maps)
- Recharts (charts)
- Lucide React (icons)

**Location:** `admin/` in the project root.

---

## 1. Authentication

### Login
- **Route:** `/login`
- **Component:** `LoginPage`
- **Fields:** Username, Password
- **Behavior:** Validates both fields are non-empty. On submit, stores `sonicmap_auth=true` in `localStorage` and redirects to `/`. Credentials are **not validated** against a backend — the form accepts any non-empty input.
- **Guard:** `ProtectedRoute` wrapper redirects unauthenticated users to `/login`.

### Logout
- Triggered via the LogOut button in the sidebar footer.
- Removes `sonicmap_auth` from `localStorage` and navigates to `/login`.

---

## 2. Dashboard

**Route:** `/` | **Component:** `Index`

Displays system overview with charts, metrics, and maps.

### Stats Cards
Four cards showing aggregate counts (data sourced from mock data, not live API):

| Stat | Icon | Mock Data Source |
|------|------|-----------------|
| Tổng nhà hàng | `Store` | `restaurants` array (count) |
| Tổng âm thanh | `Headphones` | `audios` array (count) |
| Người dùng | `Users` | `users` array (count) |
| Tổng món ăn | `UtensilsCrossed` | `dishes` array (count) |

Each card shows a positive/neutral delta indicator (hardcoded).

### Charts
1. **Lượt nghe theo ngày** — Area chart (Recharts `AreaChart`) backed by `dailyListens` mock data (15 days of listen counts).
2. **Nhà hàng được nghe nhiều nhất** — Horizontal bar chart (Recharts `BarChart`) backed by `topRestaurants` mock data (top 5 restaurants by listen count).

### Key Metrics Panel
- **Tổng lượt nghe:** Hardcoded value `15,254` with `+12.5%` delta.
- **Thời gian trung bình nghe 1 POI:** Hardcoded value `3:45` minutes.

### Heatmap Section
- **Component:** `HeatmapSection`
- **Library:** Leaflet with CARTO tile layer
- **Data:** `heatmapData` mock array of `[lat, lng, intensity]` tuples
- **Markers:**
  - Blue circle markers — user activity heatmap points, radius scaled by intensity
  - Cyan circle markers — restaurant POI locations, with popup showing name and address

### User Route Section
- **Component:** `UserRouteSection`
- **Library:** Leaflet
- **Data:** `userPaths` mock array (2 visitor paths, each with multiple waypoints)
- **Visualization:** Polylines connecting waypoints per user, circle markers at each POI (size indicates if `duration > 60s`), popup showing restaurant, username, and dwell time.
- **Legend:** Shows username badges with point count.

---

## 3. Restaurant Management

**Route:** `/restaurants` | **Component:** `RestaurantsPage`

### Table
Columns: Nhà hàng, Địa chỉ, Điện thoại, Giờ mở cửa, Trạng thái, Ngày tạo, *(action)*

- Data sourced from `restaurants` mock array (8 records).
- All columns display data directly; no pagination.
- **Sort:** Not implemented.
- **Filter:** Client-side text search by `name` and `address`.

### Status
Displayed via `StatusBadge` component:
- `active` → "Hoạt động" (green pill)
- `inactive` → "Ngừng hoạt động" (gray pill)

### Actions
- **Lock/Unlock:** Toggle button per row. Opens `ConfirmDialog`.
  - Lock → sets `status: "inactive"`
  - Unlock → sets `status: "active"`
  - On confirm → calls `addLog(action, "Restaurant", name)` and shows success toast.

---

## 4. User Management

**Route:** `/users` | **Component:** `UsersPage`

### Table
Columns: Tên đăng nhập, Vai trò, Trạng thái, Ngày tạo, Hành động

- Data sourced from `users` mock array (4 records).
- No search or pagination.

### Role Display
- `admin` → "Quản trị viên" badge (blue)
- `editor` → "Biên tập viên" badge (gray)
- Both roles displayed with a `Shield` icon.

### Status Display
- `is_active: true` → "Hoạt động" (green)
- `is_active: false` → "Ngừng hoạt động" (gray)

### Actions
1. **Create User** — "Tạo người dùng" button opens `Dialog`.
   - Fields: Username (text input), Vai trò (dropdown: Quản trị viên / Biên tập viên)
   - On submit: adds new `User` object to local state with `is_active: true`, `created_at: today`, `password_hash: "***"`. Does **not** call a backend API.
   - Shows success toast.

2. **Lock/Unlock** — Icon button per row. Opens `ConfirmDialog`.
   - Toggles `is_active` flag.
   - On confirm → calls `addLog(action, "User", username)` and shows success toast.

3. **Change Role** — Per-row dropdown (`Select` component) with "Quản trị viên" / "Biên tập viên" options.
   - Immediately updates role on selection change.
   - No confirmation dialog.
   - No log entry (unlike lock/unlock).

---

## 5. Activity Logs

**Route:** `/logs` | **Component:** `LogsPage`

### Table
Columns: Quản trị viên, Hành động, Đối tượng, Tên, Thời gian

### Log Sources
1. **Session logs** — returned by `getSessionLogs()` from `adminLogs.ts` (in-memory array populated at runtime by calls to `addLog()`).
2. **Historical logs** — `activityLogs` mock array (8 pre-seeded records).

Both sources are merged and displayed together in the table.

### Action Badge Colors
| Action | Badge Color |
|--------|------------|
| `LOCK` | Amber |
| `UNLOCK` | Emerald |
| `BAN` | Red |
| `UPDATE` | Blue |
| `DISABLE` | Red |
| `ENABLE` | Emerald |

### Real-time Refresh
`LogsPage` uses a `setInterval` that ticks every 2 seconds to re-render the component, picking up any new session log entries added during the current admin session.

---

## 6. Shared Infrastructure

### Admin Layout (`AdminLayout`)
- Fixed left sidebar (width: 240px) + scrollable main content area (`ml-60` offset).
- Sticky page header with `backdrop-filter: blur`.

### Sidebar (`AdminSidebar`)
- Brand header: "SonicMap" + "Admin" badge.
- Navigation items: Tổng quan, Nhà hàng, Người dùng, Nhật ký.
- Active state highlighted with lighter background.
- Footer: admin avatar (initials "A"), username, email (`admin@sonicmap.vn`), logout button.

### Confirm Dialog (`ConfirmDialog`)
- Wraps `AlertDialog` from shadcn/ui.
- Props: `title`, `description`, `onConfirm`, `confirmLabel`, `variant` (`"default"` | `"destructive"`).
- Destructive variant renders a warning icon and styled confirm button.
- Used by both Restaurants and Users pages for lock/unlock confirmations.

### Toast Notifications
- Powered by `sonner` (`Toaster` + `useToast` from sonner).
- Success: `"Thao tác thành công"` after all mutating actions.
- Error: `"Có lỗi xảy ra"` on validation failure in user creation.

### Logging Library (`adminLogs.ts`)
- `addLog(action, target, target_name)` — prepends a new `ActivityLog` entry to an in-memory array with timestamp and hardcoded user `"admin"`.
- `getSessionLogs()` — returns the in-memory array.
- Logs are **not persisted** to a backend.

---

## 7. Mock Data Schema

All data is client-side mock data defined in `src/lib/mockData.ts`. No backend API is called.

| Entity | Fields |
|--------|--------|
| `Restaurant` | `restaurant_id`, `name`, `description`, `latitude`, `longitude`, `phone`, `address`, `status`, `created_at`, `user_id`, `open_time`, `close_time` |
| `Dish` | `dish_id`, `name`, `price`, `description`, `created_at`, `restaurant_id`, `image_id`, `status` |
| `Audio` | `audio_id`, `restaurant_id`, `language_id`, `audio_url`, `version`, `status`, `date_generation` |
| `Language` | `language_id`, `language_code`, `language_name` |
| `User` | `user_id`, `username`, `password_hash`, `role`, `is_active`, `created_at` |
| `ActivityLog` | `id`, `user`, `action`, `target`, `target_name`, `timestamp` |

---

## 8. Routing Summary

| Path | Page | Auth Required |
|------|------|--------------|
| `/login` | LoginPage | No (redirects to `/` if already authed) |
| `/` | Index (Dashboard) | Yes |
| `/restaurants` | RestaurantsPage | Yes |
| `/users` | UsersPage | Yes |
| `/logs` | LogsPage | Yes |
| `/*` | NotFound | — |

---

## 9. Not Implemented (Out of Scope)

The following features described in the previous version of this document **do not exist** in the current implementation:

- Geofence configuration UI (no settings page or form for radius/cooldown/debounce)
- Content moderation UI (no review/approval workflow for POI descriptions or media)
- System monitoring UI (no live metrics dashboard for active users, API errors, location tracking performance)
- Seller account management (sellers are not modeled as a separate entity; only internal admin/editor user accounts exist)
- Role-based permissions enforcement (UI shows roles but role checks are not enforced on any page)
- Any backend API integration (all data is mock; no real CRUD operations against the ASP.NET API)
