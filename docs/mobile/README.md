# MAUI Docs

Tai lieu tong quan cho FoodMarketNarrator.Maui.

## Muc tieu app

Ung dung mobile visitor voi tinh nang:

- Theo doi vi tri GPS
- Geofence theo POI
- Tu dong phat audio thuyet minh
- Ho tro da ngon ngu
- Cache offline co ban cho POI va audio

## Stack

- .NET MAUI (net10.0-android)
- Mapsui
- Plugin.Maui.Audio

## Luong narration

- Poll vi tri theo chu ky
- Xac dinh POI gan nhat
- Trigger enter/switch theo geofence
- Chon audio theo ngon ngu hien tai
- Chong lap bang session state va cooldown

## Cau hinh API

Cau hinh host va endpoint trong:

- FoodMarketNarrator.Maui/Settings/AppSettings.cs

Luu y thiet bi that:

- Dien thoai va may chay API phai cung mang
- API local thong thuong: <http://localhost:5044>

## Chay local

```bash
cd FoodMarketNarrator.Maui
dotnet restore
dotnet build
dotnet run -f net10.0-android
```

## Chay test

```bash
dotnet test test/maui-testing/FoodMarketNarrator.Maui.UnitTests/unit-test.csproj
```

## Tai lieu lien quan

- overview-current-features.md
- narration-geofence-trigger-flow.md
- qr-access-session-flow.md
- audio-cache-storage.md
- ../testing/unit/maui-unit-test-cases.md
