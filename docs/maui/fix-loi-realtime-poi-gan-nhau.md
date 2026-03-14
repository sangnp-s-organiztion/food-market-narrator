# Fix loi khong cap nhat POI gan nhau theo thoi gian thuc

## 1) Tom tat van de

Trong qua trinh test tren Android Emulator (khu vuc duong Vinh Khanh), app gap hien tuong:

- Khi di chuyen giua 2 POI xa nhau thi marker va highlight POI cap nhat binh thuong.
- Khi di chuyen giua 2 POI gan nhau (vi du Oc Loan va Oc Vu), marker/highlight co luc khong doi ngay.
- Man hinh map chi hien thi dung sau khi reload trang (vao lai page) hoac sau mot lan render khac.

Anh huong den UX:

- Nguoi dung thay map "tre" so voi vi tri hien tai.
- Poi gan nhat bi sai trong mot khoang thoi gian.
- Cac logic phu thuoc nearest POI (UI, narration trigger) khong on dinh trong khu vuc mat do cao.

---

## 2) Boi canh truoc khi fix (co che cu)

### 2.1 Luong cap nhat vi tri

- `LocationService` phat su kien `LocationChanged`.
- `MainPage` va `MapPage` subscribe su kien nay de cap nhat marker user va POI nearest.

Truoc do, service tung dung co che listening event cua geolocation. Sau do da duoc doi sang polling loop theo chu ky de on dinh hon trong emulator.

### 2.2 Cach tim POI gan nhat

Co ham `GetNearestPOI(...)` trong `POIService`, duoc goi moi khi co location moi.

### 2.3 Cach ve highlight tren Mapsui

- `MapHelper.HighlightPOI(...)` clear style va gan lai style cho tung feature.
- Sau do goi refresh map.

Van de o map renderer:

- Trong mot so case 2 POI rat gan nhau, style change co the khong render ra man hinh ngay neu layer/cache khong bi invalidation du manh.
- Marker highlight co the bi marker khac "de" len neu thu tu feature rendering khong uu tien marker duoc highlight.

---

## 3) Nguyen nhan goc (root cause)

Tong hop tu hanh vi thuc te va code path:

1. Tan suat update vi tri va do nhiu GPS:

- Khi 2 POI sat nhau, sai so vi tri hoac update truyen den cham lam nearest dao dong/khong doi kip.

2. Render layer chua du "force":

- Chi refresh thong thuong co the chua du de buoc Mapsui repaint trong kich ban feature gan nhau + viewport it thay doi.

3. Thu tu ve marker:

- Marker do (highlight) neu khong duoc ve sau cung co the bi marker khac che, gay cam giac "khong cap nhat".

4. Trigger khong tach ro cho map highlight:

- Highlight map can nguong rieng de tranh nhap nhay khi dung xa cum POI.

---

## 4) Muc tieu fix

- Luon recompute nearest POI theo vi tri moi.
- Chi highlight khi user du gan (nguong 20m) de giam nhieu.
- Force refresh map du manh de thay doi style hien thi ngay.
- Cap nhat vi tri theo loop lien tuc de on dinh tren emulator.

---

## 5) Cac thay doi da ap dung

## 5.1 Them nguong highlight rieng cho map

File:

- `food-market-narrator-maui/Settings/AppSettings.cs`

Thay doi:

- Them `MapHighlightDistanceMeters = 20`.

Y nghia:

- Tach biet nguong "map highlight" (20m) voi cac nguong khac nhu trigger narration/floating button (30m).

---

## 5.2 Luon recompute nearest bang sap xep khoang cach

File:

- `food-market-narrator-maui/Services/POIService.cs`

Thay doi:

- Trong `GetNearestPOI(Location currentLocation, IEnumerable<POI>? pois = null)`, doi ve:
  - sort theo `GetDistanceMeters(...)`
  - lay phan tu dau tien

Tu duy:

- Don gian hoa logic nearest.
- Bao dam moi location moi deu tinh nearest tu danh sach hien tai.

---

## 5.3 Update map highlight theo trigger 20m tren ca 2 man hinh

Files:

- `food-market-narrator-maui/Views/MapPage.xaml.cs`
- `food-market-narrator-maui/Views/MainPage.xaml.cs`

Thay doi:

- Tinh nearest moi lan co location event.
- Kiem tra distance voi nearest:
  - neu `< MapHighlightDistanceMeters` thi highlight nearest
  - nguoc lai truyen `null` de bo highlight

Loi ich:

- Giam flicker khi dung xa cum diem.
- Nearest trong vung quan tam duoc cap nhat ro rang.

---

## 5.4 Force refresh manh hon trong MapHelper

File:

- `food-market-narrator-maui/Helpers/MapHelper.cs`

Thay doi trong `HighlightPOI(...)`:

1. Reorder feature:

- Dua feature duoc highlight xuong cuoi danh sach de ve sau cung.

2. Force invalidation + redraw:

- `poiLayer.DataHasChanged();`
- `mapControl.Map.RefreshData();`
- `mapControl.Map.RefreshGraphics();`

Thay doi trong `UpdateUserLocation(...)`:

- Sau khi thay marker user, goi:
  - `mapControl.Map.RefreshData();`
  - `mapControl.Map.RefreshGraphics();`

Y nghia:

- Buoc renderer cap nhat du lieu va repaint ngay ca khi viewport khong doi nhieu.

---

## 5.5 Doi co che tracking sang polling loop 2 giay

File:

- `food-market-narrator-maui/Services/LocationService.cs`

Thay doi chinh:

- Them `PollInterval = TimeSpan.FromSeconds(2)`.
- Them `TrackingRequest = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10))`.
- Trong `StartTrackingAsync()`:
  - tao `CancellationTokenSource`
  - chay `RunTrackingLoopAsync(token)`
- Trong loop:
  - `GetLocationAsync(TrackingRequest)`
  - neu co location thi raise `LocationChanged`
  - `Task.Delay(2 giay)`
- Trong `StopTracking()`:
  - cancel token + dispose.

Y nghia:

- Dong bo voi yeu cau "tracking loop".
- Giam phu thuoc vao callback listening behavior cua platform trong moi truong emulator.

---

## 6) So sanh truoc va sau fix

## 6.1 Ve cap nhat vi tri

Truoc:

- Co the phu thuoc vao listening callback va cach platform day event.
- Case POI rat gan nhau de gap "tre" update.

Sau:

- Polling loop dinh ky 2s, moi nhat quan hon tren emulator.
- Moi chu ky deu co co hoi recompute nearest.

## 6.2 Ve nearest POI

Truoc:

- Logic nearest co the khong du ro rang trong mot so path.

Sau:

- Nearest duoc recompute ro rang bang sort distance moi lan goi.

## 6.3 Ve hien thi highlight

Truoc:

- Highlight co the khong thay doi ngay do cache/render behavior.
- Marker do co the bi marker khac de len khi gan nhau.

Sau:

- Co invalidation + refresh graphics sau moi update.
- Marker highlight duoc ve tren cung.
- Chi highlight trong vung 20m de giam nhieu/nhap nhay.

## 6.4 Ve UI cam nhan

Truoc:

- Nguoi dung co the phai reload page moi thay POI dung.

Sau:

- Chuyen dong giua POI gan nhau cap nhat ro rang hon.
- Khong can reload trang trong flow su dung thong thuong.

---

## 7) Pham vi fix va diem can luu y

Da fix:

- Recompute nearest, trigger 20m, force refresh, tracking loop.

Chua lam trong dot nay:

- Chua tich hop native Android `FusedLocationProviderClient` rieng.

Ly do:

- Can bo sung package Play Services + implement service theo Android platform + DI cho da nen tang.
- Dot nay uu tien fix nhanh, an toan, chay ngay tren kien truc MAUI hien tai.

Huong nang cap tiep theo (neu can):

- Tao `IAndroidFusedLocationProvider` tren Android.
- Fallback sang MAUI Geolocation tren iOS/Windows.
- Them bo loc accuracy/smoothing (vd bo diem co Accuracy > 30m) de nearest on dinh hon nua.

---

## 8) Kiem thu de xac nhan fix

Checklist de test:

1. Mo app, vao map o khu vuc co 2 POI gan nhau (Oc Loan, Oc Vu).
2. Gia lap di chuyen GPS qua lai giua 2 diem.
3. Xac nhan:

- Marker user di chuyen theo update moi.
- POI do (highlight) doi dung theo nearest trong vung <20m.
- Khong can reload page de thay doi highlight.

Regression test:

1. Di chuyen giua 2 POI xa nhau (vd Lau Met Nuong 79K va Oc Vu).
2. Dam bao behavior khong bi anh huong xau.
3. Dam bao floating button narration van obey nguong 30m nhu cu.

---

## 9) Ket luan

Ban fix nay giai quyet dung van de chinh: map khong cap nhat nearest/highlight o cum POI gan nhau.

Gia tri lon nhat den tu 4 diem ket hop:

- Recompute nearest lien tuc.
- Trigger highlight 20m.
- Force refresh renderer du manh.
- Tracking loop 2 giay on dinh tren emulator.

Khi can do chinh xac cao hon nua (muc Google Maps), buoc tiep theo nen la tich hop FusedLocationProviderClient tren Android kem bo loc nhieu toa do.
