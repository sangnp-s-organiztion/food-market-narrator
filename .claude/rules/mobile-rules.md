---
paths:
  - "FoodMarketNarrator.Maui/**/*.cs"
---

# Mobile App Rules (.NET MAUI / Android)

## Core Responsibilities

1. Track visitor GPS location (foreground service on Android).
2. Compute distance to all POIs (restaurants) using lat/lng.
3. Detect geofence entry/exit.
4. Auto-play audio narration when geofence conditions are met.
5. Cache POI data and audio for offline use.

## Location Tracking Config

| Setting | Value |
|---------|-------|
| PollInterval | 2 seconds |
| MinPublishDistanceMeters | 6 m |
| Accuracy | Best available |
| Timeout | 10 seconds |

Android permission flow: `WhenInUse` → `Always` (Android 10+) → `PostNotifications` (Android 13+).
Use a foreground service (`TrackingForegroundService`) to maintain tracking in background.

## Distance Calculation

Use Haversine formula (or equivalent) to compute distance from visitor position to each restaurant POI. Only update POI state when distance changes by ≥ `MinPublishDistanceMeters`.

## Geofence State Machine

Each restaurant is a POI with enter/exit radii:

| State | Condition |
|-------|-----------|
| **Enter** | Not in any POI → visitor within 30m of a POI |
| **Switch** | In POI A → visitor within 30m of POI B |
| **Exit** | In POI → visitor beyond 40m of current POI |

Radii: `PoiEnterRadiusMeters = 30`, `PoiExitRadiusMeters = 40` (hysteresis prevents edge-case flapping).

## Narration Trigger Rules

**Two anti-repeat mechanisms:**

1. **Session**: `HashSet<string> _playedPOIs` tracks restaurants played in current narration session. Reset on `StopNarration()`.
2. **Cooldown**: 60-second minimum between auto-plays for the same POI.

**Trigger logic:**
- Visitor enters 30m geofence → narration eligible.
- If already played in this session → skip (unless cooldown expired).
- If cooldown not expired → skip.
- Force/manual trigger bypasses all checks.
- Manual replay always available from POI detail screen.

**Audio selection:**
1. Get visitor's selected language.
2. Fetch audio for (restaurant_id, language_id).
3. If found → play. If not found → show soft notification, skip.

## API Calls (Mobile)

Use **public endpoints only** — no authentication required:

```
GET /Restaurant
GET /Restaurant/{id}
GET /Language
GET /Language/{languageCode}
GET /public/Restaurant/{restaurantId}/images
GET /public/Restaurant/{restaurantId}/dishes
GET /public/Restaurant/{restaurantId}/audios
```

> **Warning**: Do NOT use `/Restaurant/{id}/images|dishes|audios` — these require authentication.

## Offline Cache

| Data | Location | Format |
|------|----------|--------|
| POI list | `FileSystem.AppDataDirectory/offline_cache/pois.json` | JSON |
| Audio files | `audio_cache/` | SHA256 hash of `{language_code}\|{audio_url}` |

Cache policy:
- Max total: 200MB.
- Min free space: 50MB before downloading.
- LRU eviction when approaching limit.
- Priority: local cache → bundled package → network.

Offline behavior:
- POIs served from cache.
- Audio plays if cached; otherwise silent.
- No network + no cache = no narration (graceful degradation).

## Performance

- GPS updates throttled by `PollInterval` + `MinPublishDistanceMeters`.
- Distance calculations only when location is valid and moved enough.
- Narration triggered only when geofence conditions are met.
- No UI-blocking on the main thread.

## Error Handling

- Location permission denied → show explanation and deep-link to settings.
- Audio play failure → log, show soft toast, continue tracking.
- Network unavailable → switch to offline cache silently.
- Invalid POI data from API → skip POI, log error.

## Code Quality

- All location/geofence logic isolated in `Services/` (e.g., `LocationService`, `POIService`, `NarrationService`).
- App settings (radii, intervals, cache limits) in `AppSettings` — never hardcoded.
- Dispose of location listeners and audio players properly.
