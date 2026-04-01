# Admin Features (React + TypeScript + Vite)

Last updated: 2026-04-01

## 1. Scope

This document describes the **current implemented state** of the new Admin frontend in `admin/`.

It focuses on:

- routes and pages
- authentication flow
- data source status (real API vs mock)
- analytics integration status
- known gaps and safe next steps

## 2. Tech Stack (Admin)

- React 18 + TypeScript + Vite
- React Router v6
- TanStack Query
- shadcn/ui + Tailwind
- Leaflet map for heatmap and movement paths

## 3. Routing and Navigation

Protected routes are defined in `admin/src/App.tsx` and guarded by `AuthProvider` + `ProtectedRoute`.

- `/login`: login page
- `/`: dashboard (overview + analytics widgets)
- `/restaurants`: restaurant management
- `/users`: user management
- `/logs`: recent listening activity

Sidebar menu is defined in `admin/src/components/AdminSidebar.tsx`.

## 4. Authentication (Cookie-based)

Current flow uses backend auth APIs and cookie session:

- `POST /Auth/login`
- `GET /Auth/me`
- `POST /Auth/logout`

Implementation files:

- `admin/src/lib/authApi.ts`
- `admin/src/contexts/AuthContext.tsx`
- `admin/src/pages/LoginPage.tsx`

Behavior:

- On app bootstrap, frontend calls `/Auth/me` to restore auth state from cookie.
- Protected routes wait for bootstrap completion (`isLoading`) before redirecting.
- Logout is best-effort API call, then local auth state is cleared.

## 5. Data Source Status (Important)

### 5.1 Features already using real APIs

1. Users management page (`/users`)

- GET `/api/users`
- POST `/api/users`
- PATCH `/api/users/{id}/role`
- PATCH `/api/users/{id}/status`

2. Restaurants management page (`/restaurants`)

- GET `/api/restaurant`
- GET `/api/restaurant/{id}`
- PATCH `/api/restaurant/{id}`
- PATCH `/api/restaurant/{id}/status`

3. Analytics widgets on dashboard (`/`)

- GET `/api/analytics/kpis`
- GET `/api/analytics/top-restaurants`
- GET `/api/analytics/heatmap`
- GET `/api/analytics/movement-paths`

4. Activity logs page (`/logs`)

- GET `/api/analytics/recent-activity`

### 5.2 Parts still using static/mock data

1. Dashboard entity KPI cards (total restaurants/audios/users/dishes)

- still uses local constants and `mockData` import

2. Dashboard chart "Lượt nghe theo ngày"

- currently hard-coded chart dataset in page component

3. Heatmap POI markers fallback

- if API POI data is empty, map falls back to mock restaurant list

4. `admin/src/lib/mockData.ts`

- still present and partly referenced for UI fallback/placeholder behavior

## 6. Analytics API Contract Used by Admin

Client definition is in `admin/src/lib/analyticsApi.ts` and expected response types are in `admin/src/types/analytics.ts`.

### 6.1 Endpoints used

- `GET /api/analytics/kpis`
- `GET /api/analytics/heatmap?hours={number}`
- `GET /api/analytics/top-audios?limit={number}`
- `GET /api/analytics/top-restaurants?limit={number}`
- `GET /api/analytics/movement-paths?sessionLimit={number}`
- `GET /api/analytics/recent-activity?limit={number}`
- `GET /api/analytics/audio-stats`

### 6.2 UI usage by page

- Dashboard currently consumes: kpis, heatmap, top-restaurants, movement-paths
- Logs page consumes: recent-activity (auto-refresh every 30s)
- top-audios/audio-stats APIs are available in client but not yet rendered on a dedicated page/widget

## 7. Page-by-Page Feature Snapshot

### 7.1 Dashboard (`/`)

Implemented:

- system overview layout
- analytics KPI cards (total valid plays, average listening time)
- top restaurants bar chart (API)
- heatmap section (API points)
- anonymous movement paths map (API sessions)

Partially implemented / placeholder:

- entity KPI cards still static
- daily listens area chart still static dataset

### 7.2 Restaurants (`/restaurants`)

Implemented:

- fetch restaurant list from API
- search by name/address (client-side)
- lock/unlock restaurant via status API
- loading/empty/error states

### 7.3 Users (`/users`)

Implemented:

- fetch user list from API
- create user
- lock/unlock user
- change role (admin/editor mapping)
- loading/empty/error states

### 7.4 Logs (`/logs`)

Implemented:

- read recent activity from analytics API
- display inferred action label by duration
- auto refresh every 30 seconds
- loading/empty/error states

## 8. Environment Configuration

All API clients use:

- `VITE_API_BASE_URL` (if provided)
- fallback: `http://localhost:5044`

This applies to:

- `authApi`
- `adminApi`
- `analyticsApi`

All calls send `credentials: include` to support cookie auth.

## 9. Known Integration Risks

1. If backend route naming differs from `/api/...`, frontend will fail until route/base-path is aligned.
2. If CORS cookie policy is not configured correctly, authenticated requests will fail even after login.
3. Dashboard still mixes real analytics and static cards, which may confuse operators if values do not match.

## 10. Recommended Next Steps (Additive)

1. Replace static entity KPI cards with real API counts.
2. Replace static daily listens chart with an analytics endpoint (timeseries).
3. Remove mock fallback from heatmap once production data is stable.
4. Add a dedicated widget/page for top audios using existing `getTopAudios` or `getAudioStats` client methods.

## 11. Acceptance Checklist

- [x] Login uses backend cookie auth APIs
- [x] Users page reads/writes real API
- [x] Restaurants page reads/writes real API
- [x] Logs page reads real analytics API
- [x] Dashboard reads core analytics APIs
- [ ] All dashboard metrics fully real-time (still has static parts)
- [ ] Mock data fully removed from runtime path
