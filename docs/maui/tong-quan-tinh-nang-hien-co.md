# Tong quan tinh nang hien co - MAUI app

Tai lieu nay tom tat cac tinh nang dang ton tai trong phan ung dung MAUI (food-market-narrator-maui) dua tren code hien tai.

## 1) Man hinh va dieu huong

- Shell da cau hinh 2 man hinh chinh:
  - MainPage (Trang chu)
  - MapPage (Ban do)
- Da dang ky route den POIDetailPage, cho phep mo trang chi tiet qua restaurantId.
- Bottom navigation hien thi cac tab: Trang chu, Ban do, Yeu thich, Lich su, Cai dat.
- Hien tai chi co 2 tab da co dieu huong thuc te:
  - Trang chu -> MainPage
  - Ban do -> MapPage
- Cac tab Yeu thich/Lich su/Cai dat moi la khung UI, chua co handler chuc nang.

## 2) Tai du lieu va quan ly POI

- POIService goi API /api/restaurant de lay danh sach quan an/POI.
- Co co che cache danh sach POI trong bo nho de tai su dung.
- Ho tro:
  - Lay toan bo POI
  - Lay POI theo restaurantId
  - Tim POI gan nhat theo vi tri nguoi dung
- Co logic enter/exit radius trong service (30m vao, 40m ra) de xac dinh vao/ra khu vuc POI.

## 3) Dinh vi va theo doi vi tri

- LocationService xin quyen LocationWhenInUse.
- Ho tro lay vi tri hien tai (GetCurrentLocationAsync).
- Ho tro theo doi vi tri lien tuc foreground (StartTrackingAsync), phat su kien LocationChanged.
- MainPage va MapPage deu subscribe su kien thay doi vi tri de cap nhat ban do/POI gan nhat.

## 4) Ban do (Mapsui + OSM)

- Da tich hop Mapsui va tai tile OpenStreetMap.
- Co cache tile ban do trong thu muc cache cua app.
- Co helper MapHelper de:
  - Load ban do + layer OSM
  - Ve cac marker POI
  - Highlight POI gan nhat (doi mau/kich thuoc marker)
  - Zoom/center den vi tri can focus
  - Cap nhat marker vi tri nguoi dung (co ham, chua thay goi thuong xuyen trong flow hien tai)

## 5) Chuc nang tren MainPage

- Hien thi:
  - Header + nut chon ngon ngu
  - Search UI (dang la giao dien)
  - Ban do nhung
  - Danh muc mon an (dang la giao dien)
  - Danh sach quan ngon noi bat (CollectionView tu POIService)
- Khi chon item trong danh sach POI se dieu huong sang POIDetailPage theo restaurantId.
- Co floating button "Bat dau/Dung thuyet minh":
  - Chi hien khi nguoi dung nam trong ban kinh <= 30m tinh tu POI gan nhat
  - Dong bo trang thai theo NarrationFlowService.IsNarrating
- Lan dau vao app se tu dong mo popup chon ngon ngu.

## 6) Ngon ngu

- LanguageService luu ngon ngu da chon trong Preferences (key: AppLanguage).
- Ho tro cac ngon ngu UI/chon audio:
  - vi-VN, en-US, zh-CN, ko-KR, ja-JP
- Khi doi ngon ngu:
  - Cap nhat CurrentCulture/CurrentUICulture
  - Gan lai AppResources.Culture
  - Reload AppShell de cap nhat giao dien
- Sau khi doi ngon ngu o MainPage, flow se bat dau thuyet minh lai.

## 7) Thuyet minh tu dong theo vi tri

- NarrationFlowService quan ly che do thuyet minh.
- StartNarration:
  - Bat co narration
  - Dang ky LocationChanged
  - Kiem tra ngay vi tri hien tai de trigger audio neu du dieu kien
- CheckAndNarrateAsync:
  - Tim POI gan nhat
  - Kiem tra khoang cach trigger (<= 30m)
  - Chon audio theo ngon ngu hien tai
  - Co queue phat audio de tranh trung lap
  - Danh dau POI da phat de tranh auto trigger lap lai
- StopNarration:
  - Huy subscribe location
  - Dung audio
  - Xoa queue va reset danh sach POI da phat

## 8) Audio

- AudioService dung Plugin.Maui.Audio.
- Ho tro:
  - PlaySound(language, fileName)
  - Pause, Resume, StopSound
  - Theo doi IsPlaying, IsPaused, Duration, CurrentPosition
  - Event PlaybackEnded
- Co logic ResolveAudioPath de xu ly nhieu format duong dan audio (audio/, narration/, resources/narration/...).

## 9) Trang chi tiet POI (POIDetailPage)

- Nhan restaurantId tu route query.
- Tu load du lieu chi tiet POI theo id va bind len UI.
- Co module Audio Guide:
  - Nut play/stop
  - Ho tro pause/resume
  - Progress bar + current time/total time
  - Timer cap nhat tien trinh moi 200ms
- Co nut quay lai MainPage.
- Co cac nut UI "Duong di" va "Goi dien ngay" (hien tai chua thay code xu ly su kien click).

## 10) Trang thai tinh nang (da co vs placeholder)

Da co logic hoat dong:

- Lay du lieu POI tu API + cache
- Ban do OSM + marker POI + highlight POI gan nhat
- Theo doi vi tri lien tuc
- Trigger thuyet minh theo vi tri
- Chon ngon ngu va luu Preferences
- Trang chi tiet POI + phat audio + progress
- Dieu huong MainPage <-> MapPage <-> POIDetailPage

Dang o muc giao dien/chua noi day du logic:

- Search/filter tren MainPage/MapPage
- Tab Yeu thich, Lich su, Cai dat
- Nut Favorite/Share tren POIDetailPage
- Nut "Duong di" va "Goi dien ngay" tren POIDetailPage
- Mot so card/noi dung tren MapPage dang hard-code mau

## 11) Phu thuoc chinh dang su dung

- .NET MAUI
- Mapsui.UI.Maui
- BruTile (tile source/cache)
- Plugin.Maui.Audio
- SQLite attributes trong model POI
- HttpClient (goi API)

## 12) Ghi chu moi truong API

- MAUI app dang cau hinh base URL ve host local: http://10.0.2.2:5044/
- Day la setup phu hop cho Android Emulator truy cap localhost may host.
