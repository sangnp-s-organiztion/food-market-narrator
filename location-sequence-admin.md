# Mapping vi tri ham cho sequence diagram admin (React + TypeScript)

Nguon sequence: docs/diagram/sequence/admin

Quy uoc:

- Dinh dang vi tri: DuongDanFile:start-end
- Moi anh sequence duoc map toi UI handler/context va API function

## DangNhap

| Diagram                              | Ham va vi tri code                                                                                                                                                                                                                         |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| DangNhap/1_KiemTraDangNhap.png       | AuthProvider (bootstrap auth) - admin/src/contexts/AuthContext.tsx:23-65<br>AuthContext.refreshMe - admin/src/contexts/AuthContext.tsx:28-37<br>authApi.getMe - admin/src/lib/authApi.ts:37-47<br>ProtectedRoute - admin/src/App.tsx:68-76 |
| DangNhap/2_DangNhapAdmin.png         | LoginPage.handleSubmit - admin/src/pages/LoginPage.tsx:28-49<br>AuthContext.login - admin/src/contexts/AuthContext.tsx:48-52<br>authApi.login - admin/src/lib/authApi.ts:22-35                                                             |
| DangNhap/3_DangXuat.png              | AdminSidebar.handleLogout - admin/src/components/AdminSidebar.tsx:41-45<br>AuthContext.logout - admin/src/contexts/AuthContext.tsx:54-58<br>authApi.logout - admin/src/lib/authApi.ts:49-54                                                |
| DangNhap/4_DieuHuongVaBaoVeRoute.png | ProtectedRoute - admin/src/App.tsx:68-76<br>AppRoutes - admin/src/App.tsx:78-164                                                                                                                                                           |

## DashBoard

| Diagram                                   | Ham va vi tri code                                                                                                                                                                                                                                                                                    |
| ----------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DashBoard/5_XemDashboard.png              | Dashboard page - admin/src/pages/Index.tsx:43-234<br>analyticsApi.getKpis - admin/src/lib/analyticsApi.ts:35-37<br>analyticsApi.getTopAudios - admin/src/lib/analyticsApi.ts:56-60<br>analyticsApi.getTopRestaurants - admin/src/lib/analyticsApi.ts:66-70                                            |
| DashBoard/6_BoLocHeatMap.png              | Dashboard heatmap state (heatmapHours) - admin/src/pages/Index.tsx:44-44<br>analyticsApi.getHeatmap - admin/src/lib/analyticsApi.ts:43-49<br>HeatmapSection - admin/src/components/HeatmapSection.tsx:75-422<br>HeatmapSection.handleRecenterMap - admin/src/components/HeatmapSection.tsx:163-189    |
| DashBoard/7_XemTuyenDiChuyenNguoiDung.png | TrajectoryPage - admin/src/pages/TrajectoryPage.tsx:7-32<br>analyticsApi.getMovementPaths - admin/src/lib/analyticsApi.ts:74-83<br>TrajectorySection - admin/src/components/TrajectorySection.tsx:117-657<br>TrajectorySection.handleRecenterMap - admin/src/components/TrajectorySection.tsx:284-298 |

## NhaHang

| Diagram                            | Ham va vi tri code                                                                                                                                                                                                                                                                                                                         |
| ---------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NhaHang/8_TimKiemVaHienNhaHang.png | RestaurantsPage - admin/src/pages/Restaurants.tsx:125-851<br>search state - admin/src/pages/Restaurants.tsx:127-127<br>toPageRestaurant - admin/src/pages/Restaurants.tsx:44-59<br>restaurantApi.getAll - admin/src/lib/adminApi.ts:368-368                                                                                                |
| NhaHang/9_XemChiTietNhaHang.png    | RestaurantsPage.handleOpenDetail - admin/src/pages/Restaurants.tsx:361-364<br>RestaurantsPage.handleDetailDialogChange - admin/src/pages/Restaurants.tsx:366-371<br>restaurantApi.getById - admin/src/lib/adminApi.ts:376-377                                                                                                              |
| NhaHang/10_TaoNhaHangMoi.png       | RestaurantsPage.handleCreateRestaurant - admin/src/pages/Restaurants.tsx:265-293<br>RestaurantsPage.createMutation - admin/src/pages/Restaurants.tsx:221-256<br>restaurantApi.create - admin/src/lib/adminApi.ts:370-374                                                                                                                   |
| NhaHang/10a_DienToaDoaGGMap.png    | RestaurantsPage.extractCoordinatesFromGoogleMapsUrl - admin/src/pages/Restaurants.tsx:92-123<br>RestaurantsPage.handleGoogleMapsUrlChange - admin/src/pages/Restaurants.tsx:311-331<br>RestaurantsPage.handleGoogleMapsUrlBlur - admin/src/pages/Restaurants.tsx:333-359<br>mapsApi.resolveCoordinates - admin/src/lib/adminApi.ts:587-590 |
| NhaHang/11_KhoaNhaHang.png         | RestaurantsPage.handleConfirmAction - admin/src/pages/Restaurants.tsx:258-263<br>RestaurantsPage.statusMutation - admin/src/pages/Restaurants.tsx:209-219<br>restaurantApi.updateStatus - admin/src/lib/adminApi.ts:385-386                                                                                                                |

## NguoiDung

| Diagram                                             | Ham va vi tri code                                                                                                                                                                                                       |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NguoiDung/12_XemDanhSachNguoiDung.png               | UsersPage - admin/src/pages/UsersPage.tsx:84-624<br>userApi.getAll - admin/src/lib/adminApi.ts:474-474<br>toPageUser - admin/src/pages/UsersPage.tsx:34-46                                                               |
| NguoiDung/13_ThemNguoiDung.png                      | UsersPage.handleCreate - admin/src/pages/UsersPage.tsx:170-203<br>UsersPage.createMutation - admin/src/pages/UsersPage.tsx:134-155<br>userApi.create - admin/src/lib/adminApi.ts:481-492                                 |
| NguoiDung/14_KhoaNguoiDung.png                      | UsersPage.statusMutation - admin/src/pages/UsersPage.tsx:157-168<br>userApi.updateStatus - admin/src/lib/adminApi.ts:493-497<br>UsersPage (ConfirmDialog onConfirm) - admin/src/pages/UsersPage.tsx:84-624               |
| NguoiDung/14a_XemChiTietNhaHang&NguoiDungQuanLy.png | UsersPage (mo detail user + detail dialog) - admin/src/pages/UsersPage.tsx:84-624<br>UsersPage.detailUserRestaurants - admin/src/pages/UsersPage.tsx:136-142<br>restaurantApi.getAll - admin/src/lib/adminApi.ts:368-368 |

## NhatKyHeThong

| Diagram                              | Ham va vi tri code                                                                                                                                                                                                       |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NhatKyHeThong/16_TuDongCapNhat.png   | LogsPage (auto refresh 30s) - admin/src/pages/LogsPage.tsx:156-511<br>auditApi.getLogs - admin/src/lib/auditApi.ts:39-41<br>analyticsApi.getRecentActivity - admin/src/lib/analyticsApi.ts:90-101                        |
| NhatKyHeThong/17_PhanTrangNhatKy.png | LogsPage (audit/audio paging state va page window) - admin/src/pages/LogsPage.tsx:156-511<br>auditApi.getLogs - admin/src/lib/auditApi.ts:39-41<br>analyticsApi.getRecentActivity - admin/src/lib/analyticsApi.ts:90-101 |

## QuanLyDich

| Diagram                              | Ham va vi tri code                                                                                                                                                                                                                                                                                                                            |
| ------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| QuanLyDich/15_XemNhatKyHeThong.png   | AppRoutes (route /logs) - admin/src/App.tsx:78-164<br>LogsPage - admin/src/pages/LogsPage.tsx:156-511<br>auditApi.getLogs - admin/src/lib/auditApi.ts:39-41                                                                                                                                                                                   |
| QuanLyDich/23_XemChiPhiDich.png      | TranslationBillingPage - admin/src/pages/TranslationBillingPage.tsx:150-523<br>translationBillingApi.getMonthly - admin/src/lib/adminApi.ts:546-557<br>translationBillingApi.getUsage - admin/src/lib/adminApi.ts:559-570<br>translationBillingApi.getAudioUsage - admin/src/lib/adminApi.ts:572-583                                          |
| QuanLyDich/24_LocVaPhanTrangDich.png | TranslationBillingPage (filter month/seller + paging controls) - admin/src/pages/TranslationBillingPage.tsx:150-523<br>getCurrentMonth - admin/src/pages/TranslationBillingPage.tsx:14-19<br>toQueryString - admin/src/lib/adminApi.ts:535-543<br>translationBillingApi.getMonthly/getUsage/getAudioUsage - admin/src/lib/adminApi.ts:546-583 |

## Tour

| Diagram                        | Ham va vi tri code                                                                                                                                                                                                                                                 |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Tour/18_XemChiTietTour.png     | ToursPage.handleOpenDetail - admin/src/pages/ToursPage.tsx:426-431<br>ToursPage.handleDialogOpenChange - admin/src/pages/ToursPage.tsx:433-452<br>tourApi.getById - admin/src/lib/adminApi.ts:419-419                                                              |
| Tour/18a_TaoTourMoi.png        | ToursPage.handleCreateTour - admin/src/pages/ToursPage.tsx:608-637<br>ToursPage.createTourMutation - admin/src/pages/ToursPage.tsx:291-339<br>tourApi.create - admin/src/lib/adminApi.ts:441-445<br>tourApi.uploadImageForTour - admin/src/lib/adminApi.ts:431-439 |
| Tour/19_LuuCapNhatTour.png     | ToursPage.handleSaveChanges - admin/src/pages/ToursPage.tsx:569-606<br>ToursPage.saveChangesMutation - admin/src/pages/ToursPage.tsx:368-424<br>tourApi.reorderStops - admin/src/lib/adminApi.ts:461-465<br>tourApi.update - admin/src/lib/adminApi.ts:467-472     |
| Tour/20_ThemNhaHangVaoTour.png | ToursPage.handleAddRestaurant - admin/src/pages/ToursPage.tsx:504-523<br>ToursPage.addRestaurantMutation - admin/src/pages/ToursPage.tsx:250-270<br>tourApi.addRestaurant - admin/src/lib/adminApi.ts:447-451                                                      |
| Tour/20a_KhoaTour.png          | ToursPage.statusMutation - admin/src/pages/ToursPage.tsx:341-366<br>tourApi.update (isActive) - admin/src/lib/adminApi.ts:467-472<br>ToursPage (ConfirmDialog onConfirm khoa/mo tour) - admin/src/pages/ToursPage.tsx:88-1237                                      |

## TaiKhoan

| Diagram                              | Ham va vi tri code                                                                                                                                                                                                                                                                                                                                          |
| ------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| TaiKhoan/22_DoiMatKhauMoTaiKhoan.png | AccountPage.startEditProfile - admin/src/pages/AccountPage.tsx:83-91<br>AccountPage.handleSaveProfile - admin/src/pages/AccountPage.tsx:93-110<br>AccountPage.handleChangePassword - admin/src/pages/AccountPage.tsx:112-134<br>userApi.updateMyProfile - admin/src/lib/adminApi.ts:505-511<br>userApi.updateMyPassword - admin/src/lib/adminApi.ts:499-503 |
