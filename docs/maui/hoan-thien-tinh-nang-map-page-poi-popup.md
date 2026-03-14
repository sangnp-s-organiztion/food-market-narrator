# Hoan thien tinh nang POI popup tren MapPage

Tai lieu nay tong hop nhung gi da duoc implement cho tinh nang tren `MapPage`:

- Them nut `Zoom In`, `Zoom Out`, `My Location`
- Tap vao POI tren map se hien popup card duoi
- Popup card hien dung thong tin POI vua tap (ten, anh, dia chi)
- Nut `View Details` dieu huong sang `POIDetailPage` dung `restaurantId`
- Xoa nut `Share` trong popup card
- Fix cac loi compile do API Mapsui 5.0.2 khac voi gia dinh API truoc do

---

## 1) Pham vi file da cap nhat

- `food-market-narrator-maui/Views/MapPage.xaml`
- `food-market-narrator-maui/Views/MapPage.xaml.cs`

---

## 2) UI da cap nhat tren MapPage

### 2.1 Cum nut dieu khien ben phai map

Da them 3 nut:

- Zoom In (`OnZoomInTapped`)
- Zoom Out (`OnZoomOutTapped`)
- My Location (`OnMyLocationTapped`)

Y nghia:

- Zoom In/Out thay doi viewport resolution co clamp min/max zoom
- My Location lay vi tri hien tai, center map den vi tri user va cap nhat marker user

### 2.2 Popup card POI duoi man hinh

Card demo da duoc doi sang card du lieu dong:

- Them `x:Name="SelectedPoiCard"` va set `IsVisible="False"` mac dinh
- Anh: `SelectedPoiImage`
- Ten: `SelectedPoiName`
- Dia chi: `SelectedPoiAddress`
- Nut `View Details`: da gan su kien `OnViewDetailClicked`

Da bo hoan toan nut `Share` trong card.

---

## 3) Logic tap POI tren map

### 3.1 Dang ky su kien tap map

Trong `OnAppearing`:

- Dang ky `mapControl.MapTapped += OnMapTapped`
- Tranh dang ky trung bang cach unsubscribe truoc khi subscribe lai

Trong `OnDisappearing`:

- `mapControl.MapTapped -= OnMapTapped`

### 3.2 Xu ly tap vao map de tim POI

`OnMapTapped` duoc implement theo luong:

1. Kiem tra danh sach POI da co du lieu (`_pois`)
2. Lay diem tap tu `e.WorldPosition`
3. Chuyen doi world coordinate -> lat/lon qua `SphericalMercator.ToLonLat(...)`
4. Tim POI gan nhat bang `_poiService.GetNearestPOI(...)`
5. Tinh nguong tap theo zoom hien tai:
   - `tapThresholdMeters = viewportResolution * MarkerTapPixelRadius`
   - Co clamp nguong trong khoang [12m, 150m]
6. Neu khoang cach den POI gan nhat <= nguong thi:
   - Hien popup card voi dung du lieu POI
   - Goi `MapHelper.HighlightPOI(...)`
7. Neu khong hop le thi an card

### 3.3 State card POI duoc chon

Da them state:

- `_selectedPoi` de giu POI dang duoc chon

Da them helper:

- `ShowSelectedPoiCard(POI poi)`
- `HideSelectedPoiCard()`

---

## 4) Dieu huong View Details

`OnViewDetailClicked`:

- Lay `restaurantId` tu `_selectedPoi`
- Encode query bang `Uri.EscapeDataString(...)`
- Dieu huong:
  - `$"{nameof(POIDetailPage)}?restaurantId={encodedId}"`

Ket qua:

- Trang `POIDetailPage` mo dung POI ma user vua tap tren map

---

## 5) Cac fix API/compile lien quan Mapsui

Trong qua trinh implement da gap mot so mismatch API. Da fix nhu sau:

1. `MapEventArgs` khong co `Position`

- Dung `e.WorldPosition` thay vi `e.Position`

2. `Viewport` la non-null type

- Bo null-check `if (viewport == null)`
- Truy cap resolution theo `mapControl.Map?.Navigator?.Viewport.Resolution`

3. `SphericalMercator.ToLonLat` tra ve tuple `(lon, lat)`

- Dung `tapLonLat.lon` va `tapLonLat.lat`
- Khong dung `x/y`

4. Canh bao obsolete voi alert

- Doi `DisplayAlert(...)` -> `DisplayAlertAsync(...)`

---

## 6) Hanh vi sau khi hoan thien

- User co the zoom map bang nut + / -
- User co the bam nut My Location de quay lai vi tri hien tai
- User tap marker POI tren map se thay popup card duoi dung theo POI da tap
- Popup hien ten + anh + dia chi
- Bam `View Details` mo trang chi tiet dung `restaurantId`
- Da bo nut `Share` khoi card

---

## 7) Checklist test de xac nhan tinh nang

1. Mo `MapPage`
2. Bam `+` va `-`:
   - Xac nhan zoom thay doi va khong vuot gioi han
3. Bam `My Location`:
   - Xac nhan map center ve vi tri user
4. Tap vao marker POI:
   - Xac nhan card hien
   - Xac nhan ten/anh/dia chi dung theo marker da tap
5. Tap `View Details`:
   - Xac nhan mo dung `POIDetailPage` cua POI vua chon
6. Tap vao vung map khong gan marker:
   - Xac nhan card an

---

## 8) Ghi chu ky thuat

- Nguong hit-test theo pixel da duoc map sang meters dua tren viewport resolution, de thao tac tap marker on dinh hon o nhieu muc zoom.
- Logic nay hien uu tien tim POI gan nhat va so voi nguong; neu mat do marker qua day, co the nang cap tiep bang map hit-test theo feature trong layer de chon POI chinh xac hon.
