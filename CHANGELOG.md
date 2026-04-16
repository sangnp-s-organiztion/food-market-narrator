# Changelog

Tổng hợp từ toàn bộ lịch sử commit (all refs).
Mỗi ngày có phần tóm tắt công việc và phần commit chi tiết theo contributor.

## 2026-04-17

### Tóm tắt trong ngày

- Đã thay đổi:
  - Mở rộng hỗ trợ translation động cho `tour` ở API public translations để mobile có thể lấy tên/mô tả tour theo ngôn ngữ.
  - Cập nhật MAUI `TourService` để áp dụng translation theo ngôn ngữ hiện tại cho:
    - Tên + mô tả tour.
    - Tên quán + địa chỉ trong danh sách điểm dừng (tour stops).
  - Bổ sung cache translation offline theo `language + entityType` cho Tour flow, có fallback khi mất mạng.
  - Localize toàn bộ metric và fallback text ở Tour Detail (ví dụ: số phút, số điểm dừng, mô tả/địa chỉ mặc định) qua RESX.
  - Sửa popup chọn ngôn ngữ ở Settings:
    - Tap vào overlay để đóng popup như nút X.
    - Giảm độ tối overlay để UX nhẹ hơn.
    - Chặn mở chồng nhiều popup.
  - Cập nhật thống kê dung lượng offline trong Settings để tính luôn thư mục cache translation.
  - Localize trạng thái mở/đóng quán ở Main/Favorite theo ngôn ngữ (`StatusOpenNow`, `StatusClosedNow`), bỏ hardcode tiếng Việt.
  - Localize tiêu đề và empty-state của trang Favorite (bỏ hardcode "Yêu thích").
- Xác minh:
  - Build MAUI `food-market-narrator.csproj` thành công sau các thay đổi.
  - API có cập nhật code hỗ trợ `tour` translation; build API có lúc bị lock file do process đang chạy, không phải lỗi compile logic.

### Local Workspace (chưa ghi nhận commit)

- FoodMarketNarrator.Api
  - `Services/UiTranslationService.cs`
- FoodMarketNarrator.Maui
  - `Models/POI.cs`
  - `Models/TourModel.cs`
  - `Services/TourService.cs`
  - `Views/TourDetailPage.xaml.cs`
  - `Views/SettingsPage.xaml.cs`
  - `Views/FavoritePage.xaml`
  - `Views/FavoritePage.xaml.cs`
  - `Resources/Localization/AppResources.resx`
  - `Resources/Localization/AppResources.en-US.resx`
  - `Resources/Localization/AppResources.ja-JP.resx`
  - `Resources/Localization/AppResources.ko-KR.resx`
  - `Resources/Localization/AppResources.zh-CN.resx`

## 2026-04-12

### Tóm tắt trong ngày

- Đã thay đổi:
  - Bổ sung hiển thị lịch sử tạo audio cho saler và cập nhật giao diện saler.
  - Điều chỉnh logic thêm audio ở backend.
  - Thêm/chuẩn hóa comment mô tả tác dụng của hàm.
  - Cập nhật tài liệu use case, activity và testing.
- Merged:
  - Merge pull request #208 từ nhánh `fix/bug-giathieu`.
  - Merge pull request #206 từ nhánh `main`.
  - Merge pull request #205 từ nhánh `release`.
  - Merge pull request #204 từ nhánh `develop`.
  - Merge pull request #203 từ nhánh `fix/bug-giathieu`.
  - Merge pull request #202 từ nhánh `fix/bug-before-submit`.
  - Merge branch `fix/bug-before-submit` vào `fix/bug-before-submit`.

### Nguyen Phuoc Sang (6 commits)

- [94c640b] Merge pull request #208 from sangnp-s-organiztion/fix/bug-giathieu
- [9a1c582] Merge pull request #206 from sangnp-s-organiztion/main
- [eb51166] Merge pull request #205 from sangnp-s-organiztion/release
- [7bbcf34] Merge pull request #204 from sangnp-s-organiztion/develop
- [4a4e2b3] Merge pull request #203 from sangnp-s-organiztion/fix/bug-giathieu
- [985ebc4] Merge pull request #202 from sangnp-s-organiztion/fix/bug-before-submit

### sangnpdev (7 commits)

- [636c6e0] cập nhật giao diện saler
- [d4a833a] Merge branch 'fix/bug-before-submit' of https://github.com/sangnp-s-organiztion/food-market-narrator into fix/bug-before-submit
- [7cd0ccc] thêm comment tác dụng tác dụng của hàm
- [eb315e8] thêm hiển thị lịch sử tạo audio của saler
- [d3173c7] thêm useccase cho app và đặc tả
- [dfa6407] thêm usecase cho app
- [d52bd03] cập nhật tài liệu testing

### giathieu0311 (3 commits)

- [1c6fe7c] chỉnh sửa logic thêm audio
- [815cdac] Sua UseCase
- [8bbbc1d] cập nhật activity

## 2026-04-11

### Tóm tắt trong ngày

- Đã thay đổi:
  - Cập nhật giao diện admin và saler, đồng thời vá một số lỗi UI/logic giữa hai cổng web.
  - Điều chỉnh flow QR ở admin theo hướng upload file QR thay vì tạo mới trực tiếp trong UI.
  - Cập nhật logic MAUI để tính thời gian nghe audio chính xác hơn.
  - Bổ sung schema Mongo và cập nhật file setup collection liên quan.
  - Bổ sung `.gitignore` để chặn file build tạm.
  - Cập nhật tài liệu sequence và dọn dẹp file draw.io không còn dùng.
- Merged:
  - Merge pull request #197 từ nhánh `fix/bug-giathieu`.
  - Merge pull request #196 từ nhánh `fix/bug-before-submit`.

### Nguyen Phuoc Sang (2 commits)

- [1735940] Merge pull request #197 from sangnp-s-organiztion/fix/bug-giathieu
- [db57a6a] Merge pull request #196 from sangnp-s-organiztion/fix/bug-before-submit

### sangnpdev (10 commits)

- [14bc53e] xóa draw.io
- [96a0f70] sửa saler cho phép sửa full name và ko cho sửa username
- [6d8b2cd] đổi giao diện admin, sửa qr từ tạo mới thành upload
- [c3a31e5] thêm mongo schema và cập nhật file set up collection mongo
- [c79e1a3] thêm gitignore chặn build temp
- [7fb2d61] cập nhật lại giao diện
- [1d64fec] cập nhật lại giao diện
- [2c665c2] sửa giao diện admin
- [5dd286a] sửa lại logic tính thời gian nghe audio ở maui
- [06c2e7b] sửa lỗi admin và saler

### giathieu0311 (1 commit)

- [f938611] cap nhat sequence

## 2026-04-10

### Tóm tắt trong ngày

- Đã thay đổi:
  - Cập nhật QR code theo hướng chỉ mở app qua deep link, không còn mục tiêu giới hạn thời gian khi quét.
  - Cập nhật bộ API integration tests để đồng bộ với DTO/request hiện tại.
  - Bổ sung thêm các test còn thiếu cho MAUI, API, admin và saler.
  - Cải thiện tốc độ tải chi tiết POI và cập nhật nội dung hiển thị tiếng Việt.
  - Cập nhật giao diện quên mật khẩu, điều chỉnh dấu câu cho email và bỏ chức năng xóa avatar.
- Merged:
  - Merge pull request #186 từ nhánh `admin/billing`.
  - Merge pull request #184 từ nhánh `visitor/tour-detail`.

### Nguyen Phuoc Sang (2 commits)

- [69aaa27] Merge pull request #186 from sangnp-s-organiztion/admin/billing
- [aff560b] Merge pull request #184 from sangnp-s-organiztion/visitor/tour-detail

### sangnpdev (4 commits)

- [36e5f29] cập nhật thêm các test còn thiếu
- [7c6532f] cập nhật lại test api integretion
- [541a822] cập nhật lại QR code, chỉ quét để mở app
- [ce84b07] cải thiện tốc độ load chi tiết poi

### giathieu0311 (4 commits)

- [e9b541f] Cập nhật dấu câu cho gmail
- [2f50002] cập nhật lại giao diện mật khẩu ver2
- [ee0efef] quên mật khẩu
- [3fb059c] bỏ chức năng xóa avt

## 2026-04-09

### Tóm tắt trong ngày

- Đã thay đổi:
  - Triển khai và hoàn thiện tour detail page, bổ sung nút xem chi tiết từ Tour page.
  - Cải thiện hiệu năng và tốc độ load chi tiết POI.
  - Bổ sung/điều chỉnh logic xác thực `is_active` cho tour model.
  - Cập nhật dữ liệu tour, dish và chỉnh sửa các luồng ảnh khi tạo/cập nhật tour.
  - Dịch text giao diện sang tiếng Việt.
- Merged:
  - Merge pull request #182 từ nhánh `visitor/tour-detail`.
  - Merge pull request #181 từ nhánh `admin/billing`.
  - Merge pull request #180 từ nhánh `admin/billing`.
  - Merge pull request #178 từ nhánh `visitor/setting-page-ui`.

### sangnpdev (7 commits)

- [4c771f8] dịch text thành tiếng Việt
- [cb0a885] cải thiện hiệu năng
- [be9c282] thêm nút xem chi tiết ở tour page, thêm xác thực is_active = true ở tour model
- [1357e5e] thêm trang chi tiết tour và các logic trong trang chi tiết tour
- [4fb10bc] Merge branch 'admin/billing' of https://github.com/sangnp-s-organiztion/food-market-narrator into visitor/tour-detail
- [9d860b9] fix lỗi không nhận ảnh khi tạo và cập nhật tour
- [74693d2] hiện số (stop_order) của poi khi đang xem các poi thuộc tour

### Nguyen Phuoc Sang (5 commits)

- [4c89ce4] Merge pull request #182 from sangnp-s-organiztion/visitor/tour-detail
- [db5bf79] Merge branch 'develop' into visitor/tour-detail
- [fb0ca59] Merge pull request #181 from sangnp-s-organiztion/admin/billing
- [e3e56a1] Merge pull request #180 from sangnp-s-organiztion/admin/billing
- [4d32719] Merge pull request #178 from sangnp-s-organiztion/visitor/setting-page-ui

### giathieu0311 (3 commits)

- [52ba28e] cập nhật lại tour và người dùng
- [27e57bb] tour sửa thêm
- [eb8d0ab] cập nhật lại dish

## 2026-04-08

### Tóm tắt trong ngày

- Đã thay đổi:
  - Bổ sung/cập nhật luồng tour và route map ở cả admin, saler và mobile.
  - Cập nhật giao diện map/main page: lọc POI, hiển thị danh sách POI theo tour, điều hướng từ setting sang tour.
  - Hoàn thiện dữ liệu bản đồ: link Google Maps, tọa độ, route/logo tour.
  - Mở rộng tính năng saler: thống kê, lịch sử token, dịch lại nội dung UI.
  - Triển khai offline tour và xử lý phụ thuộc TourImageWarmupService để tránh lỗi không load TourPage.
- Merged:
  - Merge pull request #176 từ nhánh `admin/billing`.
  - Merge pull request #172 từ nhánh `admin/route-map`.
  - Merge pull request #171 từ nhánh `visitor/offline-tour-v2`.
  - Merge pull request #168 từ nhánh `visitor/offline-tour-v2`.
  - Merge pull request #167 từ nhánh `saler/manage-translate`.

### giathieu0311 (12 commits)

- [6953d93] image-admin
- [de267fa] thêm limk bản đồ saler
- [bcee318] dán link share gg map
- [6b30f9c] upgrade thêm gg link map
- [56ebe68] cập nhật tọa độ theo ggmap
- [05a565a] thống kê bên saler
- [b49be48] lịch sử xem token ở saler
- [a4f6451] dịch lại các trang bên saler và admin
- [6d00baa] thêm tour và thêm trạng tháo
- [256563f] audio
- [c6a2ddb] cập nhật route và logo tour
- [d2a41bf] cập nhật flow-admin

### sangnpdev (8 commits)

- [e17c07e] thêm chức năng lọc cho main page và map page
- [1076b25] thiết kế lại giao diên của nút hiện tất cả ở map page khi hiển thị list poi của tour
- [d6c4e9a] xóa phân loại bị thừa bên main page
- [aa9be68] xóa Tự tùy chỉnh hành trình và đánh giá ở tour
- [2e84691] thêm tính năng khi bấm vào nút khám phá ngay ở settingPage thì sẽ nhảy qua tour
- [9e13fbe] sửa banner bên setting, xóa nút đăng nhập sửa thành banner
- [d0c2074] thêm logic khi nhấn vào xem chi tiết tour, khi xem chi tiết tour thì đi tới các poi kh thuộc tour cũng ko phát audio của poi đó
- [67fa6fa] triển khai offline cho tour, đồng thời xử lí phụ thuộc của tourPage vào TourImageWarmupService qua contructor - bug tourPage ko load được

### Nguyen Phuoc Sang (5 commits)

- [5751c15] Merge pull request #176 from sangnp-s-organiztion/admin/billing
- [0c180e0] Merge pull request #172 from sangnp-s-organiztion/admin/route-map
- [85d9222] Merge pull request #171 from sangnp-s-organiztion/visitor/offline-tour-v2
- [55d6cc1] Merge pull request #168 from sangnp-s-organiztion/visitor/offline-tour-v2
- [14518f8] Merge pull request #167 from sangnp-s-organiztion/saler/manage-translate

## 2026-04-07

### Tóm tắt trong ngày

- Đã thay đổi:
  - Bổ sung/cập nhật tính năng tour: stop_order, trạng thái nổi bật/ưu tiên, và dữ liệu tour cho MSSQL.
  - Cập nhật UI/UX mobile: làm nổi bật POI trong tour, highlight POI gần nhất, điều chỉnh tổng thể giao diện app và điều hướng lịch sử.
  - Cập nhật tài khoản admin/saler: chỉnh sửa thông tin tài khoản, bổ sung email/sđt, cập nhật luồng đổi mật khẩu.
  - Điều chỉnh luồng hiển thị tour để đảm bảo vẫn trả kết quả rỗng khi dữ liệu đầu vào lỗi.
- Merged:
  - Merge pull request #165 từ nhánh `saler/manage-translate`.

### giathieu0311 (12 commits)

- [453550a] chỉnh sửa thời gian dự kiến, ưu tiên, nổi bật
- [bab7d44] cập nhật dấu câu và tiếng việt cho admin
- [8af76eb] Cập nhật thứ tự stop_order
- [b76fdb4] thêm chức năng tour
- [a4cffb7] thêm chỉnh sửa cho tài khoản ở admin và saler
- [ffe280d] Merge branch 'visitor/add-tour' of https://github.com/sangnp-s-organiztion/food-market-narrator into saler/manage-translate
- [63823df] cập nhật mật tài khoản ở admin và saler
- [d18def8] thêm email và sđt
- [2fe181b] sửa chỉnh sửa người dùng, nhập lại mật khẩu
- [3b0eee0] Merge branch 'visitor/add-tour' of https://github.com/sangnp-s-organiztion/food-market-narrator into saler/manage-translate
- [61ce324] lọc theo tháng
- [509dc69] Chỉnh admin trang tuyến đường đi theo ngày giờ

### sangnpdev (7 commits)

- [3c0c93e] fix lỗi không hiện tour nếu sai bất cư sthonog tin nào, -> nếu sai thì vẫn trả về nhưng rỗng
- [405c4e1] ẩn luôn tên của mấy quán gần mình mà ko có trong tour
- [8c8add4] hightlight poi gần nhất bên mappage
- [9b6755e] hiển thị nổi bật các poi của tour
- [e2c782a] xóa image urlr ở tour và sửa thành image id
- [a94fee3] sửa lại giao diện tổng của app, bỏ nút lịch sử ở bottom navigation. đưa lịch sử vào cài đặt
- [fe8be23] setup tour cho database mssql

### Nguyen Phuoc Sang (1 commit)

- [b288ffe] Merge pull request #165 from sangnp-s-organiztion/saler/manage-translate

## 2026-04-06

### Tóm tắt trong ngày

- Đã thay đổi:
  - MAUI MainPage: tối ưu khởi tạo UI khi quay lại trang chính, cache trạng thái hiển thị nút thuyết minh, chỉ delay tracking 1 lần mỗi phiên và preload POI/location nền để nút phản hồi nhanh hơn.
  - MAUI BottomNavigation: điều hướng về Home ưu tiên pop về MainPage trong navigation stack hiện tại, tránh reset route không cần thiết.
  - MAUI LanguageService: đổi ngôn ngữ không còn reset AppShell, giữ nguyên navigation stack và trang đang mở.
  - MAUI LocationLogSyncService: thêm persist buffer log vị trí ra file local, tự restore khi app khởi động và tiếp tục retry sync khi online trở lại.
  - MAUI AppSettings: cập nhật LocalApiHost cho môi trường chạy API hiện tại.
  - Docs: cập nhật architecture/release notes để phản ánh thay đổi ở LanguageService, LocationLogSyncService và luồng UI MainPage.

### sangnpdev (3 commits)

- [6f95960] Tối ưu UI/điều hướng MAUI (MainPage + BottomNavigation), cải thiện phản hồi nút thuyết minh khi quay lại trang chính và cập nhật tài liệu kiến trúc/release notes.
- [517d2de] Chuẩn hóa comment tiếng Việt trong LanguageService để mô tả rõ hành vi mặc định và đổi ngôn ngữ.
- [8dd173f] Bỏ reset AppShell khi đổi ngôn ngữ, bổ sung persist/restore buffer cho LocationLogSyncService và cập nhật LocalApiHost.

## 2026-04-05

### Tóm tắt trong ngày

- Added:
  - them chi tiet nha hang o admin
  - feat: update download
  - feat(settings): add offline data usage display for cache sizes
  - feat(maui): add 3-minute POI TTL refresh with startup logs and update caching docs
  - feat: add mocks for location and QR access services in narration flow tests
  - feat: add QR time-limited access enforcement with app auto-close on expiry
- Đã thay đổi:
  - cập nhật docs prd
  - cập nhật prd
  - cập nhật docs
  - cập nhật ảnh các nhà hàng
  - ok ok ok ok
  - update logic time
  - update lung tung
  - update admin ui
  - update giao diện trang nhật ký bên admin
  - update heatmap
  - update: optimal performence
  - thử tính năng
- Fixed:
  - sửa docs
  - fix audio version
  - sửa ui admin
- Docs:
  - docs: update docs
  - docs(mobile): bổ sung tài liệu flow QR access session
- Merged:
  - Merge pull request #157 from sangnp-s-organiztion/release
  - Merge pull request #156 from sangnp-s-organiztion/develop
  - Merge pull request #155 from sangnp-s-organiztion/fixbug/aa
  - Merge pull request #154 from sangnp-s-organiztion/release
  - Merge pull request #153 from sangnp-s-organiztion/develop
  - Merge pull request #152 from sangnp-s-organiztion/update/admin-saler-ui
  - Merge pull request #151 from sangnp-s-organiztion/develop
  - Merge pull request #150 from sangnp-s-organiztion/experiment-feature
  - Merge pull request #149 from sangnp-s-organiztion/visitor/ttl-3m
  - Merge pull request #148 from sangnp-s-organiztion/develop
  - Merge pull request #147 from sangnp-s-organiztion/visitor/add-mini-player
  - Merge pull request #146 from sangnp-s-organiztion/visitor/add-mini-player

### Nguyen Phuoc Sang (12 commits)

- [924dc82] Merge pull request #157 from sangnp-s-organiztion/release
- [f66bccd] Merge pull request #156 from sangnp-s-organiztion/develop
- [8735ac8] Merge pull request #155 from sangnp-s-organiztion/fixbug/aa
- [8372210] Merge pull request #154 from sangnp-s-organiztion/release
- [15209e4] Merge pull request #153 from sangnp-s-organiztion/develop
- [138e7a5] Merge pull request #152 from sangnp-s-organiztion/update/admin-saler-ui
- [841a425] Merge pull request #151 from sangnp-s-organiztion/develop
- [fbedca2] Merge pull request #150 from sangnp-s-organiztion/experiment-feature
- [81a28d7] Merge pull request #149 from sangnp-s-organiztion/visitor/ttl-3m
- [c4e09aa] Merge pull request #148 from sangnp-s-organiztion/develop
- [ce4a197] Merge pull request #147 from sangnp-s-organiztion/visitor/add-mini-player
- [fdc2cf2] Merge pull request #146 from sangnp-s-organiztion/visitor/add-mini-player

### sangnpdev (25 commits)

- [f394fb7] sửa docs
- [2cb8c2d] cập nhật docs prd
- [22b9f51] cập nhật prd
- [bbe2946] cập nhật docs
- [27b0a59] cập nhật ảnh các nhà hàng
- [777009c] ok ok ok ok
- [8c27b4f] update logic time
- [f89561c] fix audio version
- [a16b5aa] sửa ui admin
- [8434310] them chi tiet nha hang o admin
- [214556b] update lung tung
- [de83373] update admin ui
- [3337b4c] update giao diện trang nhật ký bên admin
- [aaeb1b5] update heatmap
- [6c552ac] docs: update docs
- [cd83b70] update: optimal performence
- [ec0ed38] feat: update download
- [417e791] feat(settings): add offline data usage display for cache sizes
- [dfc0cf8] thử tính năng
- [24f75b4] feat(maui): add 3-minute POI TTL refresh with startup logs and update caching docs
- [8e4fc92] docs(mobile): bổ sung tài liệu flow QR access session
- [75a1c57] docs(mobile): bổ sung tài liệu flow QR access session
- [8355b70] feat: add mocks for location and QR access services in narration flow tests
- [650af89] feat: add QR time-limited access enforcement with app auto-close on expiry
- [25f2441] feat: add QR time-limited access enforcement with app auto-close on expiry

## 2026-04-04

### Tóm tắt trong ngày

- Added:
  - feat: update changelog with new features, improvements, and fixes for MAUI
  - feat: improve audio playback handling in narration flow
  - feat: add POI label layer and update user location functionality on map
  - feat: add zoom controls and user location functionality to map
  - feat: filter inactive POIs when loading from API and cache
  - ... và 4 thay đổi khác
- Đã thay đổi:
  - chore: update dependencies and refactor components
- Fixed:
  - fix ci admin saler
- Merged:
  - Merge pull request #145 from sangnp-s-organiztion/visitor/add-mini-player
  - Merge pull request #144 from sangnp-s-organiztion/release
  - Merge pull request #143 from sangnp-s-organiztion/develop
  - Merge pull request #142 from sangnp-s-organiztion/chore/upgrade-react-19
  - Merge pull request #141 from sangnp-s-organiztion/develop
  - ... và 1 thay đổi khác

### Nguyen Phuoc Sang (6 commits)

- [0838c68] Merge pull request #145 from sangnp-s-organiztion/visitor/add-mini-player
- [6d6d929] Merge pull request #144 from sangnp-s-organiztion/release
- [229a2be] Merge pull request #143 from sangnp-s-organiztion/develop
- [60afcaf] Merge pull request #142 from sangnp-s-organiztion/chore/upgrade-react-19
- [d8eeb2e] Merge pull request #141 from sangnp-s-organiztion/develop
- [783f4a6] Merge pull request #140 from sangnp-s-organiztion/test-v1.2

### sangnpdev (11 commits)

- [dbf5efd] feat: update changelog with new features, improvements, and fixes for MAUI
- [c5ea19f] feat: improve audio playback handling in narration flow
- [40fd466] feat: add POI label layer and update user location functionality on map
- [38f9dde] feat: add zoom controls and user location functionality to map
- [127529d] feat: filter inactive POIs when loading from API and cache
- [de819a2] feat: add pagination controls and functionality for POI list
- [47462ec] feat: add MAUI architecture documentation and runtime flows
- [d3e857b] feat: enhance location services and UI interactions
- [2e7a74c] Add last_seen_at and session_id fields to UserSessions JSON data
- [324dc61] fix ci admin saler
- [b5ef0d4] chore: update dependencies and refactor components

## 2026-04-03

### Tóm tắt trong ngày

- Added:
  - feat: Force React 19 in CI for admin tests
  - feat: Enhance CI workflow and add tests for admin and saler
  - feat: update movement paths API to support 'all' session limit; adjust related components and services
  - feat: enhance movement path retrieval logic; improve session grouping and add null handling for coordinates
  - feat: update login error handling and improve error messages for user feedback
  - ... và 4 thay đổi khác
- Đã thay đổi:
  - Cập nhật tài liệu và cấu trúc cho dự án Food Market Narrator
  - Tái cấu trúc mã để tăng khả năng đọc và bảo trì
  - tái cấu trúc: cập nhật các endpoint API to remove dư thừa /public prefix and adjust related tài liệu
  - Loại bỏ tài liệu và script setup lỗi thời cho MongoDB và kiểm thử API; bổ sung hướng dẫn setup MSSQL và MongoDB mới với chỉ dẫn chi tiết và best practices.
- Fixed:
  - fixbug: audio cannot download
- Merged:
  - Merge pull request #137 from sangnp-s-organiztion/develop
  - Merge pull request #136 from sangnp-s-organiztion/admin/create-saler-account
  - Merge pull request #135 from sangnp-s-organiztion/develop
  - Merge pull request #134 from sangnp-s-organiztion/optimal-api
  - Merge pull request #133 from sangnp-s-organiztion/fixbug/audio-download

### sangnpdev (14 commits)

- [1af5ef8] feat: Force React 19 in CI for admin tests
- [36e41d4] Cập nhật tài liệu và cấu trúc cho dự án Food Market Narrator
- [59dbe95] feat: Enhance CI workflow and add tests for admin and saler
- [63442e3] feat: update movement paths API to support 'all' session limit; adjust related components and services
- [381fad9] feat: enhance movement path retrieval logic; improve session grouping and add null handling for coordinates
- [85e6e0d] feat: update login error handling and improve error messages for user feedback
- [42c7d9f] feat: implement role-based authentication for saler accounts; update user and API types
- [6a7b990] Add screenshot image for FoodMarketNarrator on April 3, 2026
- [dc21c31] Add generated image for Gemini feature
- [145fe3e] feat: enhance user creation and status update logic; set default password and improve validation messages
- [3fe7a6d] Tái cấu trúc mã để tăng khả năng đọc và bảo trì
- [288bde9] refactor: update API endpoints to remove redundant /public prefix and adjust related documentation
- [c26b33d] fixbug: audio cannot download
- [e3eb3d1] Loại bỏ tài liệu và script setup lỗi thời cho MongoDB và kiểm thử API; bổ sung hướng dẫn setup MSSQL và MongoDB mới với chỉ dẫn chi tiết và best practices.

### Nguyen Phuoc Sang (5 commits)

- [54710b2] Merge pull request #137 from sangnp-s-organiztion/develop
- [b065269] Merge pull request #136 from sangnp-s-organiztion/admin/create-saler-account
- [4997968] Merge pull request #135 from sangnp-s-organiztion/develop
- [81fc7fc] Merge pull request #134 from sangnp-s-organiztion/optimal-api
- [22318fb] Merge pull request #133 from sangnp-s-organiztion/fixbug/audio-download

## 2026-04-02

### Tóm tắt trong ngày

- Added:
  - feat: comment out logout button in SettingsPage.xaml
  - feat: implement logging for database connection and location sync operations
  - feat: enhance session handling by implementing retry logic for missing sessions
  - Add initial database schema and seed data for food market application
  - feat: add audio logging functionality with API endpoints and services
  - ... và 9 thay đổi khác
- Fixed:
  - fix: enhance layout and scrolling behavior in TrajectorySection component
  - fix: update HeatmapSection labels and improve CSS variable formatting
  - fix: update heatmap color palettes and add base layer selection with opacity control
- Merged:
  - Merge pull request #132 from sangnp-s-organiztion/visitor/logging-mongo
  - Merge pull request #131 from sangnp-s-organiztion/release
  - Merge pull request #130 from sangnp-s-organiztion/develop
  - Merge pull request #129 from sangnp-s-organiztion/fixbug/require-role-admin
  - Merge pull request #128 from sangnp-s-organiztion/develop
  - ... và 1 thay đổi khác

### Nguyen Phuoc Sang (6 commits)

- [fa57c85] Merge pull request #132 from sangnp-s-organiztion/visitor/logging-mongo
- [ae29a30] Merge pull request #131 from sangnp-s-organiztion/release
- [7c7ab5d] Merge pull request #130 from sangnp-s-organiztion/develop
- [337acc6] Merge pull request #129 from sangnp-s-organiztion/fixbug/require-role-admin
- [98162e7] Merge pull request #128 from sangnp-s-organiztion/develop
- [d029d7f] Merge pull request #127 from sangnp-s-organiztion/fixbug/require-role-admin

### sangnpdev (17 commits)

- [679ca54] feat: comment out logout button in SettingsPage.xaml
- [4d43ad7] feat: implement logging for database connection and location sync operations
- [9b470c8] feat: enhance session handling by implementing retry logic for missing sessions
- [4cd6d4f] Add initial database schema and seed data for food market application
- [d498cab] feat: add audio logging functionality with API endpoints and services
- [f96a811] feat: implement user session management with start and activity tracking
- [dee2e1f] fix: enhance layout and scrolling behavior in TrajectorySection component
- [a51fe2b] feat: add TrajectoryPage and TrajectorySection components with movement path visualization
- [40a5145] fix: update HeatmapSection labels and improve CSS variable formatting
- [10d2b74] feat: integrate movement paths into HeatmapSection and add view mode toggle
- [6395b50] feat: add recenter map functionality and POI marker handling in HeatmapSection
- [92885a4] fix: update heatmap color palettes and add base layer selection with opacity control
- [c9eab8c] feat: add heatmap functionality with customizable lookback hours and gradient options
- [0cdae99] feat: enhance heatmap functionality with color interpolation and density clustering
- [84848fe] feat: add logging for location points synchronization to server
- [5b23807] feat: implement location logging functionality with batch ingestion and synchronization
- [fbb7955] feat: add audit log functionality with API integration and UI updates

## 2026-04-01

### Tóm tắt trong ngày

- Added:
  - feat: add create restaurant functionality with validation and UI integration
  - feat: implement pagination for recent activity in Analytics module
  - feat: add admin login functionality and role-based access control
  - feat: implement admin stats API and integrate with frontend
  - feat(admin): replace mock data with real APIs - dashboard, logs, types, auditApi
  - ... và 12 thay đổi khác
- Đã thay đổi:
  - Tái cấu trúc(audit): migrate audit logging to MongoDB and update related services
  - tác vụ: bổ sung .worktrees to .gitignore
  - Tái cấu trúc xử lý ngôn ngữ ở SettingsPage, loại bỏ case dư thừa; bổ sung tài liệu setup MongoDB và script khởi tạo collection/seed data.
- Fixed:
  - fix: update activity data handling in LogsPage component
  - fix(api): remove unused TargetName field from AuditLog entity and migration
  - fix(api): read request body before \_next to capture body content
  - sửa design và .claude
  - fix: enhance audio playback functionality and background location tracking
- Docs:
  - docs: fix plan issues from review - BSON syntax, ACTION_LABELS, AuthController
  - docs: add admin real API implementation plan
  - docs: add admin real API integration design spec
- Merged:
  - Merge pull request #126 from sangnp-s-organiztion/admin-real-api
  - Merge branch 'develop' into admin-real-api
  - Merge pull request #125 from sangnp-s-organiztion/config-mongodb
  - Merge pull request #124 from sangnp-s-organiztion/develop
  - Merge branch 'develop' of https://github.com/sangnp-s-organiztion/food-market-narrator into config-mongodb
  - ... và 2 thay đổi khác

### sangnpdev (29 commits)

- [dad1f26] feat: add create restaurant functionality with validation and UI integration
- [50343c8] feat: implement pagination for recent activity in Analytics module
- [b324d89] fix: update activity data handling in LogsPage component
- [21dd7c2] feat: add admin login functionality and role-based access control
- [187d3af] refactor(audit): migrate audit logging to MongoDB and update related services
- [39cc802] feat: implement admin stats API and integrate with frontend
- [215f680] fix(api): remove unused TargetName field from AuditLog entity and migration
- [4a588c7] feat(admin): replace mock data with real APIs - dashboard, logs, types, auditApi
- [2a3b85b] feat(api): log LOGIN/LOGOUT audit events in AuthController
- [9f765c4] feat(api): add entity-counts and listens-timeseries analytics endpoints
- [c42a364] feat(api): add AuditLogsController and AuditLogService
- [1a94e13] fix(api): read request body before \_next to capture body content
- [0af6bc2] feat(api): add AuditLoggingMiddleware for automatic admin action tracking
- [d29d9c9] feat(api): add AuditLoggingMiddleware for automatic admin action tracking
- [3beab3b] feat(api): add indexes on AuditLogs(user_id, created_at)
- [e7786f1] feat(api): add AuditLog entity and EF migration
- [9566922] chore: add .worktrees to .gitignore
- [4a9ecd6] docs: fix plan issues from review - BSON syntax, ACTION_LABELS, AuthController
- [f9ec6b1] docs: add admin real API implementation plan
- [c08de2c] docs: add admin real API integration design spec
- [1a7eef9] Merge branch 'develop' of https://github.com/sangnp-s-organiztion/food-market-narrator into config-mongodb
- [12f88b3] Add analytics functionality with KPI dashboard, heatmap, and recent activity endpoints
- [55ece82] Add MongoDB setup, seed data, and update collection configurations
- [1bbd6d0] Add seed data for user sessions, location logs, and audio logs
- [e331fb4] Add comprehensive documentation for MAUI app features, troubleshooting, environment variables, local development setup, API integration tests, test strategy, and unit tests
- [a36671e] Tái cấu trúc xử lý ngôn ngữ ở SettingsPage, loại bỏ case dư thừa; bổ sung tài liệu setup MongoDB và script khởi tạo collection/seed data.
- [a7110c9] Implement MongoDB connection and health check functionality
- [b98593a] Add background location permission handling in SettingsPage and ILocationService
- [a585fd1] fix: enhance audio playback functionality and background location tracking

### Nguyen Phuoc Sang (6 commits)

- [8e8cecf] Merge pull request #126 from sangnp-s-organiztion/admin-real-api
- [90f4628] Merge branch 'develop' into admin-real-api
- [bf12649] Merge pull request #125 from sangnp-s-organiztion/config-mongodb
- [dfd3a61] Merge pull request #124 from sangnp-s-organiztion/develop
- [ed45036] Merge pull request #123 from sangnp-s-organiztion/admin/add-api
- [0103578] Merge pull request #122 from sangnp-s-organiztion/visitor/setting-layout

### giathieu0311 (1 commits)

- [b83f8d9] sửa design và .claude

## 2026-03-31

### Tóm tắt trong ngày

- Added:
  - add design admin
- Fixed:
  - fix dont play audio

### sangnpdev (1 commits)

- [37c5a83] fix dont play audio

### giathieu0311 (1 commits)

- [4defc5a] add design admin

## 2026-03-24

### Tóm tắt trong ngày

- Fixed:
  - fix-url-images
- Merged:
  - Merge pull request #121 from sangnp-s-organiztion/saler/fix-logic-dish

### Nguyen Phuoc Sang (1 commits)

- [f9a68af] Merge pull request #121 from sangnp-s-organiztion/saler/fix-logic-dish

### giathieu0311 (1 commits)

- [a5271df] fix-url-images

## 2026-03-22

### Tóm tắt trong ngày

- Fixed:
  - fix-img-name
  - fix-logic-saler
- Merged:
  - Merge pull request #120 from sangnp-s-organiztion/saler/fix-logic-dish
  - Merge pull request #119 from sangnp-s-organiztion/saler/fix-logic-dish
  - Merge pull request #118 from sangnp-s-organiztion/main

### Nguyen Phuoc Sang (3 commits)

- [afe0fb6] Merge pull request #120 from sangnp-s-organiztion/saler/fix-logic-dish
- [89c2e44] Merge pull request #119 from sangnp-s-organiztion/saler/fix-logic-dish
- [16a819e] Merge pull request #118 from sangnp-s-organiztion/main

### giathieu0311 (2 commits)

- [5063793] fix-img-name
- [cfa440b] fix-logic-saler

## 2026-03-21

### Tóm tắt trong ngày

- Added:
  - Add permissions for contents and pull-requests in CI workflow
  - Add StatusToColorConverter and update POI status handling
- Đã thay đổi:
  - Loại bỏ bước tạo PR tự động khỏi quy trình CI
  - Cập nhật GitHub Actions để dùng personal access token khi tạo PR
  - Tái cấu trúc mã để tăng khả năng đọc và bảo trì
  - Tái cấu trúc test CheckAndNarrateAsync theo mẫu async/await
  - Cập nhật test POI model để bao gồm OpeningHours trong các kịch bản status text
  - ... và 8 thay đổi khác
- Fixed:
  - Fix workload restore command in CI workflow to specify project path
- Merged:
  - Merge pull request #117 from sangnp-s-organiztion/release
  - Merge pull request #116 from sangnp-s-organiztion/develop
  - Merge pull request #115 from sangnp-s-organiztion/visitor/update-favorite
  - Merge pull request #114 from sangnp-s-organiztion/develop
  - Merge pull request #113 from sangnp-s-organiztion/visitor/update-favorite

### Nguyen Phuoc Sang (5 commits)

- [5244ae4] Merge pull request #117 from sangnp-s-organiztion/release
- [a6cbf20] Merge pull request #116 from sangnp-s-organiztion/develop
- [c0ddea2] Merge pull request #115 from sangnp-s-organiztion/visitor/update-favorite
- [65bee52] Merge pull request #114 from sangnp-s-organiztion/develop
- [ab805b7] Merge pull request #113 from sangnp-s-organiztion/visitor/update-favorite

### sangnpdev (16 commits)

- [24a1703] Loại bỏ bước tạo PR tự động khỏi quy trình CI
- [cd1c9b9] Cập nhật GitHub Actions để dùng personal access token khi tạo PR
- [8dae5b1] Add permissions for contents and pull-requests in CI workflow
- [ecbe724] Tái cấu trúc mã để tăng khả năng đọc và bảo trì
- [3c3da41] Tái cấu trúc test CheckAndNarrateAsync theo mẫu async/await
- [f6a8618] Cập nhật test POI model để bao gồm OpeningHours trong các kịch bản status text
- [64e10df] Remove unsupported target frameworks from project file
- [d9cdb1d] Update MAUI build step to target Android framework
- [c6580e9] Remove Windows target framework from project file
- [e256ac6] Remove RuntimeIdentifiers for MacCatalyst and Windows from project file
- [33bcd40] Update MAUI solution restore step to include RuntimeIdentifiers for all platforms
- [47dc2c5] Update MAUI solution restore step to include platform-specific runtime identifiers
- [f1d18dd] Refactor API integration tests to use updated response DTOs for login, user info, restaurant, and dish requests
- [0b7ffc0] Fix workload restore command in CI workflow to specify project path
- [fdda9b0] Add StatusToColorConverter and update POI status handling
- [7115b42] Refactor dish-related endpoints and models to improve image handling and visibility logic

## 2026-03-20

### Tóm tắt trong ngày

- Added:
  - Add CI workflow for MAUI and API testing with automatic PR creation
- Đã thay đổi:
  - Cập nhật CI để chạy test trên Windows và bổ sung README cho test Admin/Seller
  - Tái cấu trúc DTO và cập nhật method controller để thống nhất quy ước đặt tên
  - Cập nhật IP LocalApiHost, chỉnh layout FavoritePage, và bổ sung README cho kiểm thử
- Fixed:
  - Fix formatting inconsistencies in CI workflow configuration

### sangnpdev (5 commits)

- [060e9e9] Fix formatting inconsistencies in CI workflow configuration
- [3c5b6f7] Cập nhật CI để chạy test trên Windows và bổ sung README cho test Admin/Seller
- [92ff365] Tái cấu trúc DTO và cập nhật method controller để thống nhất quy ước đặt tên
- [698a3b6] Add CI workflow for MAUI and API testing with automatic PR creation
- [4c5bc16] Cập nhật IP LocalApiHost, chỉnh layout FavoritePage, và bổ sung README cho kiểm thử

## 2026-03-18

### Tóm tắt trong ngày

- Đã thay đổi:
  - Cập nhật claude-start.ps1
  - Đổi tên CLAUDE.MD thành CLAUDE.md
  - Cập nhật CLAUDE.MD
- Merged:
  - Merge pull request #112 from sangnp-s-organiztion/main
  - Merge pull request #111 from sangnp-s-organiztion/release
  - Merge pull request #110 from sangnp-s-organiztion/develop
  - Merge pull request #109 from sangnp-s-organiztion/develop
  - Merge pull request #108 from sangnp-s-organiztion/sangnpdev-patch-1
  - ... và 1 thay đổi khác

### Nguyen Phuoc Sang (10 commits)

- [33018ba] Merge pull request #112 from sangnp-s-organiztion/main
- [dd79abc] Merge pull request #111 from sangnp-s-organiztion/release
- [5fd6f65] Merge pull request #110 from sangnp-s-organiztion/develop
- [ba6f56f] Merge pull request #109 from sangnp-s-organiztion/develop
- [051d6f0] Merge pull request #108 from sangnp-s-organiztion/sangnpdev-patch-1
- [b3d1183] Cập nhật claude-start.ps1
- [2430802] Merge pull request #107 from sangnp-s-organiztion/document
- [04ff067] Đổi tên CLAUDE.MD thành CLAUDE.md
- [a43af48] Cập nhật CLAUDE.MD
- [a6b2ff8] Cập nhật CLAUDE.MD

## 2026-03-17

### Tóm tắt trong ngày

- Added:
  - Add unit tests for POIService and project file for testing setup
  - Add integration and unit tests for POI and history services
  - feat: add integration tests for Food Market Narrator API
  - Add unit tests for various services in the food market narrator application
  - Refactor documentation and feature requirements for Food Market Narrator
- Đã thay đổi:
  - tái cấu trúc: loại bỏ old project file and update language loading logic in SettingsPage
  - change name
  - claude start
  - tái cấu trúc: tổ chức lại claude-start.ps1 for improved readability and organization
- Merged:
  - Merge pull request #106 from sangnp-s-organiztion/main
  - Merge pull request #105 from sangnp-s-organiztion/release
  - Merge pull request #104 from sangnp-s-organiztion/develop
  - Merge pull request #103 from sangnp-s-organiztion/test-unit
  - Merge pull request #101 from sangnp-s-organiztion/main
  - ... và 3 thay đổi khác

### Nguyen Phuoc Sang (8 commits)

- [f8cc21a] Merge pull request #106 from sangnp-s-organiztion/main
- [0a5f54e] Merge pull request #105 from sangnp-s-organiztion/release
- [8407c13] Merge pull request #104 from sangnp-s-organiztion/develop
- [164c3b8] Merge pull request #103 from sangnp-s-organiztion/test-unit
- [cf3502b] Merge pull request #101 from sangnp-s-organiztion/main
- [c59a3f2] Merge pull request #100 from sangnp-s-organiztion/release
- [d9da866] Merge pull request #99 from sangnp-s-organiztion/develop
- [dd73a51] Merge pull request #98 from sangnp-s-organiztion/document-v1.1

### sangnpdev (9 commits)

- [7accd65] refactor: remove old project file and update language loading logic in SettingsPage
- [04f7321] Add unit tests for POIService and project file for testing setup
- [be19fc8] Add integration and unit tests for POI and history services
- [7826444] change name
- [c058689] feat: add integration tests for Food Market Narrator API
- [abafc44] claude start
- [b5f414b] refactor: restructure claude-start.ps1 for improved readability and organization
- [d9f7346] Add unit tests for various services in the food market narrator application
- [8137018] Refactor documentation and feature requirements for Food Market Narrator

## 2026-03-16

### Tóm tắt trong ngày

- Added:
  - add favorite page và history page
  - Add documentation for admin, seller, and visitor features in the Food Market Narrator application
  - feat(api): implement API service for restaurant management including authentication, dishes, images, and audio handling
  - feat: refactor DishesPage and ImagesPage to use API service for data fetching and manipulation
- Đã thay đổi:
  - tái cấu trúc: cập nhật language popup overlay to use Grid layout in SettingsPage
  - change ui of mainpage and remove change language to setting page
  - Cập nhật tài liệu và bổ sung file mới cho dự án Food Market Narrator
  - Tái cấu trúc(settings): centralize API host configuration and improve URL handling for Android
  - config for real android
  - ... và 1 thay đổi khác
- Fixed:
  - fix icon policy in setting
  - sua giao dien cua setting
  - fix logic add to favorite
  - fix favorite icon
- Merged:
  - Merge pull request #97 from sangnp-s-organiztion/release
  - Merge pull request #96 from sangnp-s-organiztion/main
  - Merge pull request #95 from sangnp-s-organiztion/develop
  - Merge pull request #94 from sangnp-s-organiztion/document
  - Merge pull request #93 from sangnp-s-organiztion/release
  - ... và 9 thay đổi khác

### Nguyen Phuoc Sang (14 commits)

- [14909b1] Merge pull request #97 from sangnp-s-organiztion/release
- [a14e7da] Merge pull request #96 from sangnp-s-organiztion/main
- [1d5e442] Merge pull request #95 from sangnp-s-organiztion/develop
- [6f9f142] Merge pull request #94 from sangnp-s-organiztion/document
- [371769b] Merge pull request #93 from sangnp-s-organiztion/release
- [22192c2] Merge pull request #92 from sangnp-s-organiztion/develop
- [e9d9c88] Merge pull request #91 from sangnp-s-organiztion/run-on-android
- [24e331d] Merge pull request #90 from sangnp-s-organiztion/run-on-android
- [7d9ce73] Merge pull request #89 from sangnp-s-organiztion/release
- [063704f] Merge pull request #88 from sangnp-s-organiztion/develop
- [cd5204f] Merge pull request #87 from sangnp-s-organiztion/doccument
- [e1e4ab8] Merge pull request #86 from sangnp-s-organiztion/release
- [52bd7db] Merge pull request #85 from sangnp-s-organiztion/develop
- [7434ae0] Merge pull request #84 from sangnp-s-organiztion/selers/add-api

### sangnpdev (13 commits)

- [b449580] refactor: update language popup overlay to use Grid layout in SettingsPage
- [bc4c001] change ui of mainpage and remove change language to setting page
- [402f8b6] fix icon policy in setting
- [3ba30a0] sua giao dien cua setting
- [ffaa05d] fix logic add to favorite
- [b4e77ab] fix favorite icon
- [ac49d57] add favorite page và history page
- [0c23869] Cập nhật tài liệu và bổ sung file mới cho dự án Food Market Narrator
- [4c2e670] refactor(settings): centralize API host configuration and improve URL handling for Android
- [3ccd804] config for real android
- [cdb40dc] Add documentation for admin, seller, and visitor features in the Food Market Narrator application
- [6a83487] refactor(api): improve code formatting and structure for better readability
- [132da06] feat: refactor DishesPage and ImagesPage to use API service for data fetching and manipulation

### giathieu0311 (1 commits)

- [f91d485] feat(api): implement API service for restaurant management including authentication, dishes, images, and audio handling

## 2026-03-15

### Tóm tắt trong ngày

- Added:
  - feat: add geofence cooldown and debounce documentation
  - feat: implement foreground service for background location tracking and update permissions in AndroidManifest
  - feat: add CenterOnUserLocation method and update UI by location in MainPage
  - feat: enhance narration control during language change
  - feat: implement platform audio focus handling for Android
  - ... và 6 thay đổi khác
- Đã thay đổi:
  - tái cấu trúc: loại bỏ PublicEndpoint attribute and implement PublicEndpointConvention for endpoint authorization
  - ss
  - sss
  - rác
  - Tái cấu trúc mã để tăng khả năng đọc và bảo trì
  - ... và 3 thay đổi khác
- Merged:
  - Merge pull request #83 from sangnp-s-organiztion/release
  - Merge pull request #82 from sangnp-s-organiztion/develop
  - Merge pull request #81 from sangnp-s-organiztion/visitor/debounce-cooldown
  - Merge pull request #80 from sangnp-s-organiztion/visitor/background-tracking
  - Merge pull request #79 from sangnp-s-organiztion/visitor/audio-focus-interruption
  - ... và 14 thay đổi khác

### Nguyen Phuoc Sang (19 commits)

- [9e40c41] Merge pull request #83 from sangnp-s-organiztion/release
- [64728e8] Merge pull request #82 from sangnp-s-organiztion/develop
- [aebfc0a] Merge pull request #81 from sangnp-s-organiztion/visitor/debounce-cooldown
- [c083e9f] Merge pull request #80 from sangnp-s-organiztion/visitor/background-tracking
- [35ed9b2] Merge pull request #79 from sangnp-s-organiztion/visitor/audio-focus-interruption
- [157bc9a] Merge pull request #78 from sangnp-s-organiztion/release
- [58aaded] Merge pull request #76 from sangnp-s-organiztion/develop
- [ac04074] Merge pull request #75 from sangnp-s-organiztion/hotfix/author-api
- [9cd39c1] Merge pull request #74 from sangnp-s-organiztion/develop
- [13496d0] Merge pull request #73 from sangnp-s-organiztion/saler/init
- [af24d67] Merge pull request #72 from sangnp-s-organiztion/release
- [e9c75d5] Merge pull request #71 from sangnp-s-organiztion/develop
- [727e519] Merge pull request #70 from sangnp-s-organiztion/admin/init
- [a475391] Merge pull request #69 from sangnp-s-organiztion/release
- [3ef3897] Merge pull request #68 from sangnp-s-organiztion/develop
- [ff29003] Merge pull request #67 from sangnp-s-organiztion/visitor/update-ui
- [f339bdf] Merge pull request #66 from sangnp-s-organiztion/release
- [ff696b8] Merge pull request #65 from sangnp-s-organiztion/develop
- [5589dbf] Merge pull request #64 from sangnp-s-organiztion/visitor/update-ui

### sangnpdev (18 commits)

- [8b7feb3] feat: add geofence cooldown and debounce documentation
- [cb4422b] feat: implement foreground service for background location tracking and update permissions in AndroidManifest
- [6c39ad7] feat: add CenterOnUserLocation method and update UI by location in MainPage
- [673e516] feat: enhance narration control during language change
- [8dffcbb] feat: implement platform audio focus handling for Android
- [f632260] refactor: remove PublicEndpoint attribute and implement PublicEndpointConvention for endpoint authorization
- [1b7135f] feat: add PublicEndpoint attribute and update controllers to use it
- [082c821] Implement Saler API and Controllers
- [5e47225] feat: add initial pages and components for restaurant management app
- [8928e17] ss
- [49ddb64] sss
- [6d32f16] rác
- [c101183] feat: Add POI management page with modal functionality and local storage integration
- [2634ad6] Tái cấu trúc mã để tăng khả năng đọc và bảo trì
- [b2d98fd] Refactor application startup and improve UI initialization for better performance
- [25eb63f] Enhance location tracking and map loading efficiency with debounce logic and state management
- [99d293f] Enhance search functionality in MapPage with suggestions and highlighting features
- [c82c201] Refactor audio generation scripts to use Edge TTS and update language configurations

### giathieu0311 (1 commits)

- [71e46ac] add api thieu

## 2026-03-14

### Tóm tắt trong ngày

- Added:
  - Add audio cache management features and settings integration
  - Add QR code content documentation and image for app launch
  - Add intent filter for deep linking and enhance location tracking initialization
  - Add documentation for POI popup functionality on MapPage
  - Implement zoom controls and current location tracking on map
- Đã thay đổi:
  - Cải thiện audio and language services with caching functionality and improve offline support
  - Cập nhật UI text to Vietnamese for better localization
  - Cải thiện POI selection and detail display on map interaction
  - Cải thiện real-time location tracking and improve map loading functionality
- Fixed:
  - Fix theme setting in MainActivity to use NoActionBar style
  - Fix real-time POI updates and enhance map highlight functionality
  - Fix real-time location tracking and enhance audio generation scripts
- Merged:
  - Merge pull request #63 from sangnp-s-organiztion/release
  - Merge pull request #62 from sangnp-s-organiztion/develop
  - Merge pull request #61 from sangnp-s-organiztion/fixbug/not-tracking-location-realtime
  - Merge pull request #60 from sangnp-s-organiztion/release
  - Merge pull request #59 from sangnp-s-organiztion/develop
  - ... và 1 thay đổi khác

### sangnpdev (12 commits)

- [af9700d] Add audio cache management features and settings integration
- [cb9b8ad] Enhance audio and language services with caching functionality and improve offline support
- [52202c9] Update UI text to Vietnamese for better localization
- [75486f4] Fix theme setting in MainActivity to use NoActionBar style
- [7ce21de] Add QR code content documentation and image for app launch
- [6e9a02f] Add intent filter for deep linking and enhance location tracking initialization
- [9d5854a] Add documentation for POI popup functionality on MapPage
- [7adadfd] Enhance POI selection and detail display on map interaction
- [78fba97] Implement zoom controls and current location tracking on map
- [367cb98] Enhance real-time location tracking and improve map loading functionality
- [dafa6cb] Fix real-time POI updates and enhance map highlight functionality
- [1b8ddc9] Fix real-time location tracking and enhance audio generation scripts

### Nguyen Phuoc Sang (6 commits)

- [7973e07] Merge pull request #63 from sangnp-s-organiztion/release
- [662f4de] Merge pull request #62 from sangnp-s-organiztion/develop
- [0ba51a1] Merge pull request #61 from sangnp-s-organiztion/fixbug/not-tracking-location-realtime
- [a810352] Merge pull request #60 from sangnp-s-organiztion/release
- [e8e289d] Merge pull request #59 from sangnp-s-organiztion/develop
- [08abad7] Merge pull request #58 from sangnp-s-organiztion/fixbug/not-tracking-location-realtime

## 2026-03-13

### Tóm tắt trong ngày

- Added:
  - Add user management functionality with User model, repository, and service
  - Implement GetAllLanguages API endpoint and corresponding service method
  - Add API get language by code - Language feature with repository, service, and controller
  - Add comprehensive feature overview documentation for MAUI app
  - Add copyright section to README.md
- Đã thay đổi:
  - Hide current and total time labels in audio guide section on POI detail page
  - Cải thiện audio service with current track tracking and improve playback control in POI detail page
  - Tái cấu trúc API route definitions to remove dư thừa 'api/' prefix for consistency
  - Tái cấu trúc language selection logic to improve user experience and persist language preference
  - Cải thiện language service with API integration for language retrieval and selection instead of hardcode
  - ... và 1 thay đổi khác
- Fixed:
  - Fix narration flow initialization and synchronize audio UI with service on page appearance
  - Fix real-time location tracking and update user location on map
- Merged:
  - Merge pull request #57 from sangnp-s-organiztion/release
  - Merge pull request #56 from sangnp-s-organiztion/develop
  - Merge pull request #55 from sangnp-s-organiztion/fixbug/not-tracking-location-realtime
  - Merge pull request #53 from sangnp-s-organiztion/release
  - Merge pull request #52 from sangnp-s-organiztion/develop
  - ... và 1 thay đổi khác

### sangnpdev (13 commits)

- [f272023] Hide current and total time labels in audio guide section on POI detail page
- [efc2df1] Enhance audio service with current track tracking and improve playback control in POI detail page
- [6a6426e] Fix narration flow initialization and synchronize audio UI with service on page appearance
- [6b8c3ae] Add user management functionality with User model, repository, and service
- [450b837] Refactor API route definitions to remove redundant 'api/' prefix for consistency
- [b3999e3] Fix real-time location tracking and update user location on map
- [16d81ca] Refactor language selection logic to improve user experience and persist language preference
- [a012be3] Enhance language service with API integration for language retrieval and selection instead of hardcode
- [c6a027e] Implement GetAllLanguages API endpoint and corresponding service method
- [4f49cf1] Add API get language by code - Language feature with repository, service, and controller
- [3dc7eeb] Refactor POI service to use interface; update related services and components
- [4c933ba] Add comprehensive feature overview documentation for MAUI app
- [67eecbc] Add copyright section to README.md

### Nguyen Phuoc Sang (6 commits)

- [ddd6522] Merge pull request #57 from sangnp-s-organiztion/release
- [698508b] Merge pull request #56 from sangnp-s-organiztion/develop
- [474ee9f] Merge pull request #55 from sangnp-s-organiztion/fixbug/not-tracking-location-realtime
- [fd614ea] Merge pull request #53 from sangnp-s-organiztion/release
- [37f1586] Merge pull request #52 from sangnp-s-organiztion/develop
- [7860d13] Merge pull request #51 from sangnp-s-organiztion/visitor/language-process-selected

## 2026-03-11

### Tóm tắt trong ngày

- Đã thay đổi:
  - Tái cấu trúc map handling to use Mapsui instead of Google Maps; update related services and UI components

### sangnpdev (1 commits)

- [a6e26f5] Refactor map handling to use Mapsui instead of Google Maps; update related services and UI components

## 2026-03-10

### Tóm tắt trong ngày

- Đã thay đổi:
  - Tái cấu trúc gg map sdk sang open street map
- Fixed:
  - cong bug di chuyển giữa các poi gần nhau thì nó ko cập nhật location
  - fix icon

### sangnpdev (3 commits)

- [b982430] cong bug di chuyển giữa các poi gần nhau thì nó ko cập nhật location
- [b906da8] fix icon
- [720d2ef] refactor gg map sdk sang open street map

## 2026-03-01

### Tóm tắt trong ngày

- Đã thay đổi:
  - khởi tạo saler
- Merged:
  - Merge pull request #50 from sangnp-s-organiztion/release
  - Merge pull request #49 from sangnp-s-organiztion/develop
  - Merge pull request #48 from sangnp-s-organiztion/saler/first

### Nguyen Phuoc Sang (3 commits)

- [f23a104] Merge pull request #50 from sangnp-s-organiztion/release
- [ee52912] Merge pull request #49 from sangnp-s-organiztion/develop
- [a8c5c1e] Merge pull request #48 from sangnp-s-organiztion/saler/first

### NguyenPhuocSang1695 (1 commits)

- [080cc3b] khởi tạo saler

## 2026-02-28

### Tóm tắt trong ngày

- Added:
  - thêm pause và resume cho phần phát audio ở poi detail - fix bug khi nhấn nút tạm dừng thì ko dừng mà tiếp tục phát lại audio
- Đã thay đổi:
  - ở chi tiết POI nhấn nút để nghe audio, hiệu ứng động cho thanh thời gian audio
- Merged:
  - Merge pull request #47 from sangnp-s-organiztion/release
  - Merge pull request #46 from sangnp-s-organiztion/develop
  - Merge pull request #45 from sangnp-s-organiztion/visitor/offline-audio-download

### Nguyen Phuoc Sang (3 commits)

- [27cee35] Merge pull request #47 from sangnp-s-organiztion/release
- [919b8dd] Merge pull request #46 from sangnp-s-organiztion/develop
- [1472039] Merge pull request #45 from sangnp-s-organiztion/visitor/offline-audio-download

### NguyenPhuocSang1695 (2 commits)

- [7bf8a46] thêm pause và resume cho phần phát audio ở poi detail - fix bug khi nhấn nút tạm dừng thì ko dừng mà tiếp tục phát lại audio
- [0c588cf] ở chi tiết POI nhấn nút để nghe audio, hiệu ứng động cho thanh thời gian audio

## 2026-02-26

### Tóm tắt trong ngày

- Added:
  - thêm tính năng dừng thuyết minh manual
- Đã thay đổi:
  - phát audio tiếp tục sau khi bấm dừng, giao diện của nút được cập nhật
  - ẩn nút thuyết minh khi ở xa POI để giao diện đỡ rối
  - xem chi tiết POI
  - tạo api get restaurant theo id và get all audio
- Fixed:
  - sửa lại flow phát audio: sau khi chọn ngôn ngữ thì tự phát audio luôn
- Merged:
  - Merge pull request #44 from sangnp-s-organiztion/release
  - Merge pull request #43 from sangnp-s-organiztion/develop
  - Merge pull request #42 from sangnp-s-organiztion/visitor/offline-audio-download

### Nguyen Phuoc Sang (3 commits)

- [099c720] Merge pull request #44 from sangnp-s-organiztion/release
- [5044f43] Merge pull request #43 from sangnp-s-organiztion/develop
- [3135fc0] Merge pull request #42 from sangnp-s-organiztion/visitor/offline-audio-download

### NguyenPhuocSang1695 (6 commits)

- [d1184b0] phát audio tiếp tục sau khi bấm dừng, giao diện của nút được cập nhật
- [f8899cb] thêm tính năng dừng thuyết minh manual
- [600d93f] sửa lại flow phát audio: sau khi chọn ngôn ngữ thì tự phát audio luôn
- [98547b2] ẩn nút thuyết minh khi ở xa POI để giao diện đỡ rối
- [8224bec] xem chi tiết POI
- [8f85cf0] tạo api get restaurant theo id và get all audio

## 2026-02-23

### Tóm tắt trong ngày

- Added:
  - thêm ảnh cho các quán, tạo dto để tránh loop giữa các models khi gọi api (json serialize)
- Đã thay đổi:
  - tạo trang POIDetailPage
- Merged:
  - Merge pull request #41 from sangnp-s-organiztion/develop
  - Merge pull request #40 from sangnp-s-organiztion/visitor/poi-detail-page

### Nguyen Phuoc Sang (2 commits)

- [b593e13] Merge pull request #41 from sangnp-s-organiztion/develop
- [1c0fd08] Merge pull request #40 from sangnp-s-organiztion/visitor/poi-detail-page

### NguyenPhuocSang1695 (2 commits)

- [821da14] tạo trang POIDetailPage
- [b564972] thêm ảnh cho các quán, tạo dto để tránh loop giữa các models khi gọi api (json serialize)

## 2026-02-22

### Tóm tắt trong ngày

- Đã thay đổi:
  - hightlight poi nearest, xóa rác
  - T�i c?u tr�c lại cấu trúc thư mục, tạo Interfaces để lưu interface của services

### NguyenPhuocSang1695 (2 commits)

- [e1ecc38] hightlight poi nearest, xóa rác
- [0fcd657] refactor lại cấu trúc thư mục, tạo Interfaces để lưu interface của services

## 2026-02-21

### Tóm tắt trong ngày

- Đã thay đổi:
  - tu dong hien PopUp chon ngon ngu khi vua load app
  - tao giao dien chon ngon ngu
  - doi ten file audio alo-quan-beer-seafood.mp3 â†’ sot-lau-alo-quan.mp3 quan-be-oc.mp3 â†’ quan-bo-oc.mp3 quan-oc-thao-quan-4.mp3 â†’ quan-oc-thao.mp3

### NguyenPhuocSang1695 (3 commits)

- [6780916] tu dong hien PopUp chon ngon ngu khi vua load app
- [dfb0943] tao giao dien chon ngon ngu
- [edec72e] doi ten file audio alo-quan-beer-seafood.mp3 â†’ sot-lau-alo-quan.mp3 quan-be-oc.mp3 â†’ quan-bo-oc.mp3 quan-oc-thao-quan-4.mp3 â†’ quan-oc-thao.mp3

## 2026-02-20

### Tóm tắt trong ngày

- Added:
  - thêm tính năng phát audio
- Đã thay đổi:
  - Tái cấu trúc lai giao dien
- Merged:
  - Merge pull request #39 from sangnp-s-organiztion/visitor/refactor-interface
  - Merge pull request #38 from sangnp-s-organiztion/visitor/refactor-interface

### Nguyen Phuoc Sang (2 commits)

- [f131dba] Merge pull request #39 from sangnp-s-organiztion/visitor/refactor-interface
- [3d694e2] Merge pull request #38 from sangnp-s-organiztion/visitor/refactor-interface

### NguyenPhuocSang1695 (2 commits)

- [e14e2e2] thêm tính năng phát audio
- [bf4a28d] refactor lai giao dien

## 2026-02-19

### Tóm tắt trong ngày

- Added:
  - thêm maui theory và cheatsheet
- Đã thay đổi:
  - tạo extensions
  - Delete food-market-narrator-maui/.github/workflows/auto-pr-release.yml
  - tạo readme.md
- Fixed:
  - sua giao dien trang MainPage
- Merged:
  - Merge pull request #37 from sangnp-s-organiztion/release
  - Merge pull request #36 from sangnp-s-organiztion/develop
  - Merge pull request #35 from sangnp-s-organiztion/visitor/refactor-interface
  - Merge pull request #34 from sangnp-s-organiztion/main
  - Merge pull request #33 from sangnp-s-organiztion/release
  - ... và 1 thay đổi khác

### NguyenPhuocSang1695 (4 commits)

- [121ad79] tạo extensions
- [f6be9a8] tạo readme.md
- [ba78983] thêm maui theory và cheatsheet
- [67397cb] sua giao dien trang MainPage

### Nguyen Phuoc Sang (7 commits)

- [ceb2615] Merge pull request #37 from sangnp-s-organiztion/release
- [65251f2] Merge pull request #36 from sangnp-s-organiztion/develop
- [e057ca5] Delete food-market-narrator-maui/.github/workflows/auto-pr-release.yml
- [621eecb] Merge pull request #35 from sangnp-s-organiztion/visitor/refactor-interface
- [f7a914a] Merge pull request #34 from sangnp-s-organiztion/main
- [6d54924] Merge pull request #33 from sangnp-s-organiztion/release
- [f14bd0c] Merge pull request #32 from sangnp-s-organiztion/develop

## 2026-02-15

### Tóm tắt trong ngày

- Đã thay đổi:
  - doi git ignore ra ngoai
- Merged:
  - Merge pull request #31 from sangnp-s-organiztion/visitor/connect-database
  - Merge pull request #30 from sangnp-s-organiztion/visitor/connect-database

### Nguyen Phuoc Sang (2 commits)

- [bcc5e16] Merge pull request #31 from sangnp-s-organiztion/visitor/connect-database
- [195f04b] Merge pull request #30 from sangnp-s-organiztion/visitor/connect-database

### NguyenPhuocSang1695 (1 commits)

- [6bb35fa] doi git ignore ra ngoai

## 2026-02-14

### Tóm tắt trong ngày

- Added:
  - them api de thao tac voi database
  - thêm .net webapi
- Đã thay đổi:
  - xoa webapi
  - lam chuc nang thuyet minh tu dong khi den gan POI

### NguyenPhuocSang1695 (4 commits)

- [9dbd3d9] them api de thao tac voi database
- [aa35e91] xoa webapi
- [ce4622c] thêm .net webapi
- [dd48ec9] lam chuc nang thuyet minh tu dong khi den gan POI

## 2026-02-13

### Tóm tắt trong ngày

- Added:
  - Add bin/, obj/, and .vs/ to .gitignore
- Đã thay đổi:
  - tao class narrationflowservice.cs
  - xoa .vs
  - xoa bin va obj
  - doi file fa solid 990 ttf
  - tao service moi - AudioService
  - ... và 5 thay đổi khác
- Fixed:
  - debug
  - sua git ignore
  - sua gitignore
  - sửa lỗi ko nhận AppResource - không hiện text, và sửa lỗi ko hiện icon
  - sửa lỗi khi bấm vào map lần thứ 2 trở đi thì không load các POIS
- Merged:
  - Merge pull request #29 from sangnp-s-organiztion/release
  - Merge pull request #28 from sangnp-s-organiztion/develop
  - Merge pull request #27 from sangnp-s-organiztion/visitor/auto-narrator
  - Merge pull request #26 from sangnp-s-organiztion/visitor/auto-narrator
  - Merge pull request #25 from sangnp-s-organiztion/release
  - ... và 12 thay đổi khác

### Nguyen Phuoc Sang (15 commits)

- [2b2e0e0] Merge pull request #29 from sangnp-s-organiztion/release
- [68e9535] Merge pull request #28 from sangnp-s-organiztion/develop
- [0a39e83] Merge pull request #27 from sangnp-s-organiztion/visitor/auto-narrator
- [1a10dfb] Merge pull request #26 from sangnp-s-organiztion/visitor/auto-narrator
- [e71796c] Merge pull request #25 from sangnp-s-organiztion/release
- [b05536f] Merge pull request #24 from sangnp-s-organiztion/develop
- [c46fa1e] Merge pull request #23 from sangnp-s-organiztion/visitor/auto-narrator
- [9709997] Merge pull request #22 from sangnp-s-organiztion/release
- [7d67984] Merge pull request #21 from sangnp-s-organiztion/develop
- [d7de6d6] Merge pull request #20 from sangnp-s-organiztion/visitor/auto-narrator
- [764003f] Merge branch 'develop' into visitor/auto-narrator
- [a91abbb] Add bin/, obj/, and .vs/ to .gitignore
- [3a49583] Merge pull request #18 from sangnp-s-organiztion/develop
- [c1866be] Merge pull request #16 from sangnp-s-organiztion/develop
- [a69cb8a] Merge pull request #15 from sangnp-s-organiztion/visitor/hightlight-nearest-poi

### NguyenPhuocSang1695 (22 commits)

- [957ba47] tao class narrationflowservice.cs
- [6f7b71e] xoa .vs
- [058b013] xoa bin va obj
- [805369b] Merge branch 'visitor/auto-narrator' of https://github.com/sangnp-s-organiztion/food-market-narrator into visitor/auto-narrator
- [9edb2ac] doi file fa solid 990 ttf
- [dd5d540] tao service moi - AudioService
- [f36cf7b] debug
- [0d5f14d] debug
- [a8548fc] Merge branch 'release'
- [6dbe994] sua git ignore
- [5dec496] sua gitignore
- [f705d3f] debug
- [02c99cc] merrge
- [eb65632] Merge branch 'visitor/hightlight-nearest-poi' into develop
- [b7e4327] debug
- [ced092a] sửa lỗi ko nhận AppResource - không hiện text, và sửa lỗi ko hiện icon
- [caf2874] debug
- [e899fb0] theem github action, update git ignore
- [f7b34eb] ok xóa nhánh
- [6ad2ba7] ok
- [6a24a92] sửa lỗi khi bấm vào map lần thứ 2 trở đi thì không load các POIS
- [7ce847b] đổi tên nhánh auto-narrator-tts thành isitor/hightlight-nearest-poi

## 2026-02-12

### Tóm tắt trong ngày

- Added:
  - them file audio bang azure
- Đã thay đổi:
  - xong tính năng get all pois và highlight nearest poi, thay đổi ngôn ngữ theo lựa chọn
  - hoan thanh tinh nang hightlight POI gan nhat va hien tat ca cac POIs
  - thay background trang MainPage
  - xong tinh nang thay doi ngon ngu
- Merged:
  - Merge pull request #14 from sangnp-s-organiztion/develop
  - Merge pull request #13 from sangnp-s-organiztion/visitor/auto-narrator-tts
  - Merge pull request #12 from sangnp-s-organiztion/release
  - Merge pull request #11 from sangnp-s-organiztion/develop
  - Merge pull request #10 from sangnp-s-organiztion/visitor/auto-narrator-tts
  - ... và 2 thay đổi khác

### Nguyen Phuoc Sang (7 commits)

- [d193f99] Merge pull request #14 from sangnp-s-organiztion/develop
- [7b44a51] Merge pull request #13 from sangnp-s-organiztion/visitor/auto-narrator-tts
- [e4fec28] Merge pull request #12 from sangnp-s-organiztion/release
- [14739c7] Merge pull request #11 from sangnp-s-organiztion/develop
- [a768164] Merge pull request #10 from sangnp-s-organiztion/visitor/auto-narrator-tts
- [e4b4c44] Merge pull request #9 from sangnp-s-organiztion/visitor/auto-narrator-tts
- [7b5cebe] Merge pull request #8 from sangnp-s-organiztion/develop

### NguyenPhuocSang1695 (5 commits)

- [47b6887] xong tính năng get all pois và highlight nearest poi, thay đổi ngôn ngữ theo lựa chọn
- [6e9c067] hoan thanh tinh nang hightlight POI gan nhat va hien tat ca cac POIs
- [9378608] thay background trang MainPage
- [5e822f5] xong tinh nang thay doi ngon ngu
- [60ce1f3] them file audio bang azure

## 2026-02-11

### Tóm tắt trong ngày

- Added:
  - them file fa-solid-900.ttf o Fonts
- Đã thay đổi:
  - hoan thanh chuc nang hien thi tat ca POI tren ban do
  - hoan thanh auto tracking sau 3s
  - xong tính nang khi mo ban do thi tu dong focus vao vi tri hien tai
  - tao .net maui
- Merged:
  - Merge pull request #7 from sangnp-s-organiztion/visitor/display-all-pois
  - Merge pull request #6 from sangnp-s-organiztion/visitor/auto-tracking

### Nguyen Phuoc Sang (2 commits)

- [2014101] Merge pull request #7 from sangnp-s-organiztion/visitor/display-all-pois
- [1a8036b] Merge pull request #6 from sangnp-s-organiztion/visitor/auto-tracking

### NguyenPhuocSang1695 (5 commits)

- [7fdd9d8] hoan thanh chuc nang hien thi tat ca POI tren ban do
- [23ab7a2] hoan thanh auto tracking sau 3s
- [fec2811] xong tính nang khi mo ban do thi tu dong focus vao vi tri hien tai
- [c899d29] them file fa-solid-900.ttf o Fonts
- [faa9f27] tao .net maui

## 2026-02-03

### Tóm tắt trong ngày

- Added:
  - them audios and scripts tieng anh va trung
  - them file flow.md
  - them file features.txt va restaurants-list.md
  - them file script mo ta cac quan ac va file audio cho cac quan an trong thu muc narration
- Merged:
  - Merge pull request #5 from sangnp-s-organiztion/add_file/folder

### Nguyen Phuoc Sang (1 commits)

- [67769a0] Merge pull request #5 from sangnp-s-organiztion/add_file/folder

### NguyenPhuocSang1695 (4 commits)

- [48f32a9] them audios and scripts tieng anh va trung
- [0bf2f34] them file flow.md
- [a694576] them file features.txt va restaurants-list.md
- [49c885d] them file script mo ta cac quan ac va file audio cho cac quan an trong thu muc narration

## 2026-02-02

### Tóm tắt trong ngày

- Added:
  - them file .txt o tung thu muc - tac dung cua tung thu muc
  - them noi dung file index
  - them file index
- Đã thay đổi:
  - Bump actions/setup-dotnet from 3 to 5
  - Bump actions/checkout from 4 to 6
  - xoa file code-style.yml trong github/workflows, comment file checkfolder.yml
  - Bump github/codeql-action from 2 to 4
  - tao file check-folder de chan tao thu muc
  - ... và 1 thay đổi khác

### NguyenPhuocSang1695 (6 commits)

- [6c7c0f5] them file .txt o tung thu muc - tac dung cua tung thu muc
- [95aa581] xoa file code-style.yml trong github/workflows, comment file checkfolder.yml
- [b2a3258] them noi dung file index
- [84509c8] them file index
- [ef4a9ad] tao file check-folder de chan tao thu muc
- [b129d70] tao cau truc thu muc co ban

### dependabot[bot] (3 commits)

- [16e8d20] Bump actions/setup-dotnet from 3 to 5
- [49869e8] Bump actions/checkout from 4 to 6
- [4393f23] Bump github/codeql-action from 2 to 4

## 2026-02-01

### Tóm tắt trong ngày

- Đã thay đổi:
  - Cải thiện phần giới thiệu trong README bằng định dạng in đậm
  - Chỉnh sửa README với phần tổng quan dự án đầy đủ hơn
  - Cập nhật thông tin tác giả trong README.md
  - Cập nhật lệnh chạy backend trong README
  - Điều chỉnh lại định dạng phần cấu trúc thư mục trong README
  - ... và 2 thay đổi khác

### Nguyen Phuoc Sang (6 commits)

- [dc1fcb6] Cải thiện phần giới thiệu trong README bằng định dạng in đậm
- [bd9c934] Chỉnh sửa README với phần tổng quan dự án đầy đủ hơn
- [dae8c4b] Cập nhật thông tin tác giả trong README.md
- [4e6afd9] Cập nhật lệnh chạy backend trong README
- [300b59c] Điều chỉnh lại định dạng phần cấu trúc thư mục trong README
- [df8deb1] Revise README with project details and instructions

### NguyenPhuocSang1695 (1 commits)

- [fe314f3] tao dotnet new console

## 2026-01-19

### Tóm tắt trong ngày

- Đã thay đổi:
  - Initial commit

### Nguyen Phuoc Sang (1 commits)

- [685c145] Initial commit

## 2026-03-29

### Tóm tắt trong ngày

- Không có commit trong ngày này (đã kiểm tra theo all refs).
