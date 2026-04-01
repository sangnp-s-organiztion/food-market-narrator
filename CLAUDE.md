# Food Market Narrator — AI Development Guide

## Project Overview

Location-based audio narration app for Vinh Khanh Food Street. Visitors walk near restaurants and hear audio narration automatically. Sellers and admins manage content via dashboards.

**Components:**
- `FoodMarketNarrator.Maui/` — Android visitor app (.NET MAUI)
- `FoodMarketNarrator.Api/` — ASP.NET Core Web API + EF Core + SQL Server
- `saler/` — Seller dashboard (React + TypeScript + Vite)
- `admin/` — Admin dashboard (React + TypeScript + Vite)

---

## System Flow

```
Visitor (MAUI) ──public API──► FoodMarketNarrator.Api ──► SQL Server
                                                  ▲
Seller (saler/) ──cookie auth──┘
Admin  (admin/)  ──cookie auth──┘
```

---

## Roles

| Role    | Access |
|---------|--------|
| Visitor | Public endpoints only (no login) |
| Seller  | Own restaurants only (user_id filter on every query) |
| Admin   | All resources |

---

## Tech Stack

- **Mobile**: .NET MAUI (Android), GPS + geofencing + audio playback
- **Backend**: ASP.NET Core Web API, EF Core, SQL Server, Cookie Auth
- **Frontend**: React + TypeScript + Vite + Tailwind CSS
- **Languages**: vi-VN, en-US, zh-CN, ko-KR, ja-JP

---

## Behavior Rules

1. **Do not start coding immediately** — always present a plan first.
2. **Read existing code before editing** — match existing patterns.
3. **Keep changes small and targeted** — avoid rewriting files unless required.
4. **Verify before claiming done** — build/lint/test where possible; report what couldn't be verified.
5. **Cross-layer changes** — check API contract impact before declaring complete.

---

## Standard Response Format

All API responses use this envelope:

```json
{ "success": true, "data": ..., "message": "" }
```

Use HTTP status codes: 200 OK, 201 Created, 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 500 Internal Server Error.

---

## Key Conventions

- **No `any`** in TypeScript unless unavoidable.
- **No entity models returned from API** — always use DTOs.
- **No magic strings** — use constants, enums, `nameof()`.
- **No hardcoded API URLs in components** — use central config.
- **No default credentials or secrets in code** — use env vars.
- **Cache audio** (MAUI) with SHA256 filename hash; limit 200MB, min 50MB free, LRU eviction.

---

## Detailed Rules (path-scoped)

| File | Scope |
|------|-------|
| `.claude/rules/backend-rules.md` | FoodMarketNarrator.Api/**/*.cs |
| `.claude/rules/frontend-rules.md` | admin/**, saler/** |
| `.claude/rules/mobile-rules.md` | FoodMarketNarrator.Maui/**/*.cs |
| `.claude/rules/security-rules.md` | All layers |
| `.claude/rules/dependencies-rules.md` | All .csproj, package.json |
| `.claude/architecture/backend-architecture.md` | FoodMarketNarrator.Api/**/*.cs |
| `.claude/architecture/api-architecture.md` | All layers |
| `.claude/domain/business-domain.md` | All layers |
