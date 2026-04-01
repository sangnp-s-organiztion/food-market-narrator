---
paths:
  - "FoodMarketNarrator.Api/**/*.cs"
  - "FoodMarketNarrator.Maui/**/*.cs"
  - "admin/**"
  - "saler/**"
  - ".claude/**"
---

# Business Domain

## Core Concept: POI (Point of Interest)

Every **Restaurant** is a POI. The mobile app tracks the visitor's GPS location, computes distance to each POI, and triggers audio narration when the visitor enters the geofence.

```
Visitor GPS ──► Distance calc ──► Geofence check ──► Narration trigger
                 (lat/lng)        (30m enter/40m exit)
```

## Domain Entities

| Entity | Key Fields | Notes |
|--------|------------|-------|
| **Language** | language_id, language_code, language_name | Codes: vi-VN, en-US, zh-CN, ko-KR, ja-JP |
| **User** | user_id, username, password_hash, role, is_active | Roles: Admin, Seller |
| **Restaurant** | restaurant_id, name, lat/lng, open/close_time, user_id, is_active | **POI**: lat/lng drives geofencing |
| **RestaurantImage** | image_id, restaurant_id, image_url, is_primary, sort_order | Multiple per restaurant; one primary |
| **Dish** | dish_id, restaurant_id, name, price, description, image_id, is_active | |
| **Audio** | audio_id, restaurant_id, language_id, audio_url, version, is_active | One audio per (restaurant, language) pair |

## Entity Relationships

```
User (1) ──owns──► (N) Restaurant
Restaurant (1) ──has──► (N) RestaurantImage
Restaurant (1) ──has──► (N) Dish
Restaurant (1) ──has──► (N) Audio
Audio (N) ──belongs to──► (1) Language
```

## Role & Access Rules

| Role    | Access |
|---------|--------|
| **Visitor** | Reads public endpoints only. No authentication needed. |
| **Seller**  | Can only manage restaurants where `restaurant.user_id == current_user.id`. All queries and mutations must filter by ownership. |
| **Admin**   | Full access to all resources. |

> **CRITICAL**: Every Seller-scoped query and mutation MUST include `user_id` filter. Backend enforces ownership at the repository/service layer — not just the UI.

## Multi-language Audio

- Audio is scoped to (restaurant_id, language_id).
- The MAUI app selects audio based on the visitor's current language setting.
- If no audio exists for the selected language, no narration plays.

## Ownership Enforcement Pattern

```csharp
// In Seller service/repository — every method enforces ownership
var restaurant = await _repo.GetByIdAsync(id);
if (restaurant.UserId != _currentUserId) throw new ForbiddenException();
```

## Data Integrity Rules

- `restaurant_id` is the primary identifier used across all layers (mobile, API, dashboards).
- `is_active` on Restaurant and Audio controls visibility/playability — inactive = hidden.
- `is_primary` + `sort_order` on RestaurantImage control display order.
- `language_code` (not id) is used in API responses for mobile compatibility.
