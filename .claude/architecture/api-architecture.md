---
paths:
  - "FoodMarketNarrator.Api/**/*.cs"
  - "FoodMarketNarrator.Maui/**/*.cs"
  - "admin/**"
  - "saler/**"
---

# API Architecture

## Base URL

No `/api` prefix. Endpoints are served directly from root.

```
https://{host}/Restaurant
https://{host}/public/Restaurant/{id}/audios
```

## Standard Response Envelope

```json
{
  "success": true,
  "data": { ... },
  "message": ""
}
```

- `success: true` → 2xx HTTP status.
- `success: false` → 4xx or 5xx HTTP status with `message` explaining the error.

## Authentication

All saler/admin endpoints use **Cookie Authentication**.
Mobile visitor endpoints are **public** (no login required).

## Public Endpoints (Visitor / MAUI App)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/Auth/login` | Login (returns cookie) |
| GET | `/Language` | All supported languages |
| GET | `/Language/{languageCode}` | Single language |
| GET | `/Restaurant` | All active restaurants (POI list for MAUI) |
| GET | `/Restaurant/{id}` | Single restaurant detail |
| GET | `/public/Restaurant/{restaurantId}/images` | Restaurant images |
| GET | `/public/Restaurant/{restaurantId}/dishes` | Restaurant menu |
| GET | `/public/Restaurant/{restaurantId}/audios` | Available narration audios |

> **MAUI NOTE**: Always use `/public/` prefixed endpoints. Endpoints like `/Restaurant/{id}/audios` require authentication.

## Seller Endpoints (Cookie Auth Required)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/Restaurant/{id}` | Own restaurant |
| PATCH | `/Restaurant/{id}` | Update own restaurant |
| PATCH | `/Restaurant/{id}/status` | Toggle active status |
| GET | `/Users/{userId:int}/restaurants` | Seller's restaurants |
| POST | `/Restaurant/{restaurantId}/audios` | Upload narration audio |
| PATCH | `/Audios/{audioId:int}/active` | Toggle audio active |
| DELETE | `/Audios/{audioId:int}` | Delete audio |
| POST | `/Restaurant/{restaurantId}/dishes` | Add dish |
| PUT | `/Dishes/{dishId:int}` | Update dish |
| DELETE | `/Dishes/{dishId:int}` | Delete dish |
| POST | `/Restaurant/{restaurantId}/images` | Upload image |
| DELETE | `/Images/{imageId:int}` | Delete image |
| PATCH | `/Images/{imageId:int}/primary` | Set primary image |
| PATCH | `/Restaurant/{restaurantId}/images/reorder` | Reorder images |

## Admin Endpoints (Cookie Auth + Admin Role)

| Method | Path | Description |
|--------|------|-------------|
| * | All seller endpoints | Full access to all restaurants |
| GET | `/Users` | List all users |
| POST | `/Users` | Create user (Seller or Admin) |

## Language Codes

| Code | Language |
|------|----------|
| vi-VN | Vietnamese |
| en-US | English |
| zh-CN | Chinese (Simplified) |
| ko-KR | Korean |
| ja-JP | Japanese |

## Static Media Paths

| Path | Content |
|------|---------|
| `/maui-images/{fileName}` | App images |
| `/maui-audios/{fileName}` | Packaged audio |
| `/uploads/audios/{fileName}` | Runtime uploads |

## HTTP Status Code Usage

| Code | When to use |
|------|-------------|
| 200 | Successful GET, PATCH |
| 201 | Successful POST (resource created) |
| 400 | Validation error, bad request |
| 401 | Not authenticated |
| 403 | Authenticated but not authorized |
| 404 | Resource not found |
| 500 | Server error |

## Versioning

No versioning prefix currently. If v2 is needed in the future: use `/v2/` prefix and keep `/` for v1.

## When Adding New Endpoints

1. Choose: public (visitor) or authenticated (saler/admin)?
2. Add to the correct table above.
3. If public → add to `PublicEndpoints` list in backend code.
4. Update this document.
5. Update `business-domain.md` if new entity involved.
