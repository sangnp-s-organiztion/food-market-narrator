# Đặc Tả Use Case App (Mobile)

Tài liệu này đặc tả các use case cho ứng dụng mobile dựa trên flow thực tế hiện có trong hệ thống.

Phạm vi gồm 17 use case từ khởi động ứng dụng, narration theo geofence đến các tab chức năng chính.

## UC-APP-01 - Khởi động ứng dụng

Mục tiêu: Khởi tạo giao diện và dịch vụ nền cần thiết khi mở app.

Tác nhân chính: Người dùng.

Tiền điều kiện: Ứng dụng được cài đặt và có thể chạy.

Hậu điều kiện: AppShell hiển thị, các dịch vụ warm-up/tracking/sync bắt đầu hoạt động.

Luồng chính:
1. Người dùng mở ứng dụng.
2. App tạo AppShell và hiển thị giao diện tab.
3. App xử lý deep link đầu vào nếu có.
4. App khởi động warm-up dữ liệu nền.
5. App khởi tạo audio library.
6. App start location log sync và tracking GPS.

## UC-APP-02 - Warm-up dữ liệu nền

Mục tiêu: Tải dữ liệu nền sớm để giảm độ trễ sử dụng.

Tác nhân chính: Hệ thống.

Tiền điều kiện: App vừa khởi động.

Hậu điều kiện: Language, tour, POI và dữ liệu phụ được làm ấm.

Luồng chính:
1. Hệ thống chờ startup delay theo cấu hình.
2. Hệ thống tải ngôn ngữ.
3. Hệ thống tải tour.
4. Hệ thống tải danh sách POI.
5. Hệ thống khởi chạy warm-up ảnh và món ăn offline.

## UC-APP-03 - Bootstrap audio library khi startup

Mục tiêu: Chuẩn bị audio sẵn sàng phát ngay khi cần narration.

Tác nhân chính: Hệ thống.

Tiền điều kiện: App ở giai đoạn startup audio.

Hậu điều kiện: Cờ audio_ready được thiết lập phù hợp; dữ liệu audio được prefetch theo điều kiện mạng.

Luồng chính:
1. App gọi initialize audio library.
2. Nếu online và chưa ready, hệ thống tải POI và prefetch audio active.
3. Hệ thống cập nhật audio_ready khi dữ liệu đủ.

Luồng phụ:
1. Trường hợp offline.
2. Hệ thống bỏ qua prefetch mạng và giữ trạng thái chờ sync khi có mạng.

## UC-APP-04 - Theo dõi vị trí và quyền truy cập

Mục tiêu: Bật tracking vị trí theo đúng quyền và ràng buộc hệ điều hành.

Tác nhân chính: Người dùng, Hệ thống.

Tiền điều kiện: App cần dữ liệu vị trí để narration.

Hậu điều kiện: Tracking loop chạy định kỳ hoặc bị chặn nếu từ chối quyền.

Luồng chính:
1. App yêu cầu quyền location khi sử dụng.
2. Nếu được cấp, app yêu cầu notification (Android 13+) khi cần.
3. App bật foreground service tracking.
4. App chạy vòng lặp lấy vị trí định kỳ.
5. App publish LocationChanged khi di chuyển đạt ngưỡng.

Luồng phụ:
1. Người dùng từ chối quyền.
2. App không thể tracking và thông báo trạng thái phù hợp.

## UC-APP-05 - Đồng bộ session và location logs

Mục tiêu: Ghi nhận và đồng bộ nhật ký vị trí theo batch an toàn.

Tác nhân chính: Hệ thống.

Tiền điều kiện: Dịch vụ sync đã được start.

Hậu điều kiện: Session được mở và batch logs được gửi/retry theo trạng thái mạng.

Luồng chính:
1. App gọi start location log sync service.
2. Service gọi API mở phiên người dùng.
3. Theo chu kỳ, service gửi batch location logs.
4. Khi gửi thành công, batch đã gửi được xóa khỏi buffer.

Luồng phụ:
1. Gửi batch lỗi.
2. Service hoàn trả batch vào buffer để retry lần sau.

## UC-APP-06 - Geofence enter/exit/switch

Mục tiêu: Xác định trạng thái vào/ra/chuyển POI từ dữ liệu vị trí.

Tác nhân chính: Hệ thống.

Tiền điều kiện: Có sự kiện LocationChanged hợp lệ.

Hậu điều kiện: Trả về POI cần trigger narration hoặc không thay đổi trạng thái.

Luồng chính:
1. NarrationFlow nhận vị trí mới.
2. Hệ thống debounce các mẫu di chuyển nhỏ.
3. Hệ thống gọi updateNearestPOI.
4. Hệ thống áp dụng enter/exit radius với hysteresis.
5. Nếu enter/switch thì trả POI mục tiêu.

Luồng phụ:
1. Không có thay đổi geofence.
2. Hệ thống không trigger narration.

## UC-APP-07 - Bật hoặc tắt narration tự động

Mục tiêu: Quản lý vòng đời chế độ thuyết minh tự động.

Tác nhân chính: Người dùng.

Tiền điều kiện: Người dùng đang ở app và có thể thao tác nút narration.

Hậu điều kiện: Narration được bật với state sạch hoặc tắt hoàn toàn.

Luồng chính:
1. Người dùng bấm nút thuyết minh.
2. Nếu đang tắt, app gọi StartNarration.
3. Hệ thống reset state played/cooldown/geofence.
4. Hệ thống subscribe location và bắt đầu tracking.
5. Hệ thống kiểm tra trigger narration ban đầu.

Luồng phụ:
1. Nếu đang bật narration.
2. App gọi StopNarration, hủy subscribe, xóa queue và dừng audio.

## UC-APP-08 - Trigger phát audio theo POI

Mục tiêu: Tự động phát audio đúng POI, đúng ngôn ngữ, tránh lặp.

Tác nhân chính: Hệ thống.

Tiền điều kiện: Có POI target từ geofence transition.

Hậu điều kiện: Audio được enqueue/phát và trạng thái chống lặp được cập nhật.

Luồng chính:
1. Hệ thống kiểm tra khoảng cách trigger.
2. Hệ thống kiểm tra cooldown theo POI.
3. Hệ thống kiểm tra danh sách POI đã phát trong phiên.
4. Hệ thống resolve audio active theo ngôn ngữ hiện tại.
5. Hệ thống enqueue và phát audio.
6. Hệ thống ghi nhận last played time và played POIs.

Luồng phụ:
1. Không thỏa một trong các điều kiện trigger hoặc không có audio phù hợp.
2. Hệ thống bỏ qua lần phát tự động.

## UC-APP-09 - Playback audio và cache

Mục tiêu: Phát audio ổn định với ưu tiên cache local.

Tác nhân chính: Hệ thống.

Tiền điều kiện: Có yêu cầu phát audio hợp lệ.

Hậu điều kiện: Audio phát từ cache hoặc tải mạng và lưu cache theo chính sách.

Luồng chính:
1. AudioService tìm file trong audio cache.
2. Nếu cache hit, phát stream local.
3. Nếu cache miss, tải audio từ network.
4. Hệ thống lưu file vào cache.
5. Hệ thống dọn cache theo LRU khi cần.

## UC-APP-10 - Ghi audio logs khi phát

Mục tiêu: Lưu lịch sử phát audio phục vụ thống kê.

Tác nhân chính: Hệ thống.

Tiền điều kiện: Một audio bắt đầu và kết thúc playback.

Hậu điều kiện: Bản ghi audio log được gửi backend hoặc retry nếu tạm lỗi.

Luồng chính:
1. Hệ thống ghi nhận thời điểm bắt đầu phát.
2. Khi kết thúc, hệ thống tính duration.
3. Hệ thống gọi AudioLogSyncService gửi log.
4. Service gửi POST /api/audio-logs.

Luồng phụ:
1. Backend chưa có session hoặc gửi log lỗi.
2. Service tạo session mới và retry gửi log.

## UC-APP-11 - MainPage: danh sách POI và điều hướng chi tiết

Mục tiêu: Xem POI trên trang chủ và vào trang chi tiết nhanh.

Tác nhân chính: Người dùng.

Tiền điều kiện: Người dùng mở tab Trang chủ.

Hậu điều kiện: Danh sách POI hiển thị theo filter và có thể điều hướng chi tiết.

Luồng chính:
1. MainPage load map một lần.
2. MainPage tải danh sách POI.
3. MainPage bind danh sách theo filter (All/Nearby/Favorite/Open).
4. Người dùng chọn POI.
5. App điều hướng tới POIDetailPage.

## UC-APP-12 - MapPage: lọc hoặc scope POI và tương tác bản đồ

Mục tiêu: Khám phá POI trực quan trên bản đồ theo tìm kiếm và phạm vi tour.

Tác nhân chính: Người dùng.

Tiền điều kiện: Người dùng mở tab Bản đồ.

Hậu điều kiện: Marker/callout/chi tiết POI phản ánh thao tác hiện tại.

Luồng chính:
1. MapPage tải POI nếu chưa có dữ liệu.
2. MapPage áp dụng scope theo tourPoiIds (nếu có).
3. Người dùng search/filter hoặc chạm marker.
4. MapPage highlight POI và hiển thị card chi tiết.
5. Người dùng bấm chi tiết để vào POIDetailPage.

## UC-APP-13 - POIDetail: phát thủ công, yêu thích, chỉ đường

Mục tiêu: Cung cấp thao tác trực tiếp trên một POI cụ thể.

Tác nhân chính: Người dùng.

Tiền điều kiện: Người dùng đã vào POIDetailPage.

Hậu điều kiện: Audio có thể phát thủ công, trạng thái favorite cập nhật, và map ngoài được mở khi cần.

Luồng chính:
1. Trang chi tiết tải thông tin POI và món ăn.
2. Người dùng bấm Play/Pause audio thủ công.
3. Trang gọi AudioService phát hoặc tạm dừng.
4. Người dùng bấm yêu thích.
5. Trang gọi FavoriteService thêm hoặc bỏ yêu thích.
6. Người dùng bấm Đường đi.
7. App mở launcher bản đồ với tọa độ đích.

## UC-APP-14 - Tour flow

Mục tiêu: Xem danh sách tour và bắt đầu hành trình theo tour.

Tác nhân chính: Người dùng.

Tiền điều kiện: Người dùng mở tab Hành trình.

Hậu điều kiện: Tour hiển thị và người dùng có thể vào map theo tour hoặc xem chi tiết tour.

Luồng chính:
1. TourPage gọi TourService lấy danh sách tour.
2. Service dùng cache memory/disk và refresh mạng khi cần.
3. Trang hiển thị tour active.
4. Người dùng bấm Bắt đầu hoặc Xem chi tiết.
5. App điều hướng MapPage theo tour hoặc vào TourDetailPage.

## UC-APP-15 - Favorites và History flow

Mục tiêu: Hiển thị danh sách yêu thích và lịch sử sử dụng.

Tác nhân chính: Người dùng.

Tiền điều kiện: Người dùng mở tab Yêu thích hoặc Lịch sử.

Hậu điều kiện: Danh sách favorite/history hiển thị với thông tin POI đầy đủ.

Luồng chính:
1. FavoritePage đọc danh sách favorite từ Preferences.
2. FavoritePage tải danh sách POI để map dữ liệu hiển thị.
3. HistoryPage đọc dữ liệu history in-memory.
4. HistoryPage tải POI để render thông tin liên quan.

## UC-APP-16 - Settings flow

Mục tiêu: Quản lý cấu hình và dữ liệu cục bộ của ứng dụng.

Tác nhân chính: Người dùng.

Tiền điều kiện: Người dùng mở tab Cài đặt.

Hậu điều kiện: Cài đặt được cập nhật, quyền được yêu cầu, cache và dữ liệu cục bộ được dọn theo thao tác.

Luồng chính:
1. Trang cài đặt tải danh sách ngôn ngữ và thông tin dung lượng cache.
2. Người dùng đổi ngôn ngữ.
3. Trang gọi LanguageService đổi culture.
4. Người dùng yêu cầu quyền vị trí nền.
5. Trang gọi LocationService request quyền nền.
6. Người dùng xóa cache audio hoặc toàn bộ dữ liệu cục bộ.
7. Người dùng xóa lịch sử và yêu thích.

## UC-APP-17 - Deep link QR flow

Mục tiêu: Nhận và xử lý link QR mở app đúng ngữ cảnh.

Tác nhân chính: Người dùng, Hệ thống Android.

Tiền điều kiện: Thiết bị nhận được deep link hợp lệ.

Hậu điều kiện: Link được validate và áp dụng vào app qua QrAccessService.

Luồng chính:
1. Android intent chuyển deep link vào MainActivity.
2. AppLinkDispatcher kiểm tra scheme/host hợp lệ.
3. Dispatcher chuyển payload sang lớp xử lý trong app.
4. QrAccessService áp dụng dữ liệu deep link.
5. App điều hướng hoặc cập nhật trạng thái theo payload.

Luồng phụ:
1. Link không hợp lệ hoặc payload thiếu.
2. Hệ thống bỏ qua deep link và giữ luồng app bình thường.

