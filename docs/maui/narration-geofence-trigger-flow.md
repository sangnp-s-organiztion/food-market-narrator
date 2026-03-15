# Narration Trigger Theo Geofence

## 1) Muc tieu

Dong bo narration auto-trigger theo geofence transition da du debounce/cooldown.

Khac voi cach cu (chi dua vao distance <= TriggerDistanceMeters), cach moi trigger dua tren event vao vung hop le.

## 2) Thay doi trong NarrationFlowService

Trong CheckAndNarrateAsync(Location? currentLocation = null, bool force = false):

- Force mode:
  - Van lay nearest POI truc tiep.
  - Dung cho manual trigger (nguoi dung bam nut).

- Auto mode:
  - Goi \_poiService.UpdateNearestPOI(lat, lng).
  - Chi khi ham nay tra ve POI (nghia la pass geofence + debounce + cooldown) moi enqueue phat audio.

## 3) Dieu chinh co che chong lap narration

Trong huong trien khai moi, replay gating duoc chuyen ve geofence cooldown.

Vi vay co the bo HashSet played forever o NarrationFlowService de tranh xung dot voi cooldown theo thoi gian.

## 4) Luong xu ly sau khi doi

1. LocationService phat LocationChanged.
2. NarrationFlowService nhan update vi tri.
3. Auto mode goi UpdateNearestPOI.
4. Neu null -> khong phat (chua du dieu kien).
5. Neu co POI hop le -> day vao queue, phat audio theo ngon ngu hien tai.

## 5) Loi ich

- Narration auto trigger on dinh hon trong khu POI day.
- Tranh tinh trang trigger ao do dao dong nearest.
- Manual trigger van giu hanh vi mong muon cua user.

## 6) Checklist test nhanh

- Auto mode ON, dung sat ranh gioi: khong phat ngay.
- Auto mode ON, vao vung va giu on dinh: phat 1 lan.
- Auto mode ON, o lai trong cooldown: khong phat lap.
- Bam trigger manual: van phat du nearest co dang cooldown.
