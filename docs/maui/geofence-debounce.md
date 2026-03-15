# Geofence Debounce

## 1) Muc tieu

Giam trigger sai khi GPS dao dong o ranh gioi POI.

Truoc khi trigger vao POI, app can thay POI gan nhat on dinh trong mot khoang thoi gian lien tuc (debounce window).

## 2) Tham so cau hinh

Them 1 tham so trong AppSettings:

- GeofenceDebounceSeconds = 4 (co the doi theo slide)

Y nghia:

- Khi user vua vao EnterRadius, app chua trigger ngay.
- App cho den khi du 4 giay on dinh moi cho phep trigger.

## 3) Thay doi trong POIService

Trong POIService, bo sung state cho debounce:

- \_pendingEnterPoi: POI dang theo doi de vao
- \_pendingEnterStartedAtUtc: moc thoi gian bat dau debounce

Them helper:

- TryStartDebounce(POI nearest, DateTimeOffset nowUtc)

Hanh vi:

1. Neu nearest POI thay doi, reset pending sang POI moi va bat dau dem lai.
2. Neu nearest khong doi, tinh elapsed time.
3. Chi khi elapsed >= GeofenceDebounceSeconds moi tra ve true.

## 4) Tich hop vao UpdateNearestPOI

Trong UpdateNearestPOI:

1. Van tinh nearest va khoang cach nhu cu.
2. Neu ngoai EnterRadius thi xoa pending debounce.
3. Neu trong EnterRadius thi buoc qua TryStartDebounce.
4. Neu chua du debounce thi return null (khong trigger).
5. Neu du debounce thi moi tiep tuc check cooldown/transition.

## 5) Loi ich

- Giam nhap nhay trigger khi user dung sat mep radius.
- Giam truong hop vao/ra ao do sai so GPS.
- Tranh spam event khi khu vuc co POI gan nhau.

## 6) Checklist test nhanh

- Dung sat mep EnterRadius: khong trigger ngay lap tuc.
- Giu vi tri on dinh > debounce window: trigger 1 lan.
- Nhay qua lai 2 POI gan nhau: pending duoc reset dung khi nearest thay doi.
