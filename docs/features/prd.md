# Tổng quan ứng dụng thuyết minh tự động – Phố ẩm thực Vĩnh Khánh

## 1. Giới thiệu

Phố ẩm thực Vĩnh Khánh (đường Vĩnh Khánh, Quận 4, TP. Hồ Chí Minh) là một trong những khu vực nổi tiếng với nhiều món ăn đặc trưng và thu hút đông đảo du khách. Tuy nhiên, phần lớn khách tham quan chỉ trải nghiệm ẩm thực mà chưa có nhiều thông tin về nguồn gốc món ăn, lịch sử quán, hay các điểm đặc sắc của khu phố.

Ứng dụng **Food Market Narrator** được xây dựng nhằm cung cấp hệ thống **thuyết minh tự động** cho du khách khi tham quan phố ẩm thực Vĩnh Khánh. Khi người dùng di chuyển đến các vị trí nhất định trong khu vực, ứng dụng sẽ tự động phát nội dung thuyết minh về địa điểm, quán ăn hoặc món đặc trưng tại vị trí đó.

Hệ thống giúp nâng cao trải nghiệm du lịch, giúp người dùng vừa khám phá ẩm thực vừa hiểu thêm về văn hóa và lịch sử của khu phố.

---

## 2. Mục tiêu của hệ thống

Ứng dụng được xây dựng nhằm đạt các mục tiêu sau:

- Cung cấp **thông tin thuyết minh tự động** khi người dùng đến gần các địa điểm trong phố ẩm thực.
- Giúp du khách **khám phá các quán ăn nổi bật** một cách thuận tiện.
- Tăng trải nghiệm du lịch thông qua **nội dung audio thuyết minh**.
- Hỗ trợ quản lý nội dung thuyết minh cho từng địa điểm thông qua hệ thống quản trị.

---

## 3. Phạm vi hệ thống

Hệ thống tập trung vào khu vực **phố ẩm thực Vĩnh Khánh, Quận 4** và bao gồm các chức năng chính:

- Xác định vị trí người dùng thông qua GPS.
- Tự động kích hoạt thuyết minh khi người dùng đi vào vùng địa lý của một địa điểm.
- Phát nội dung thuyết minh (sử dụng file audio).
- Hiển thị thông tin cơ bản về quán ăn hoặc địa điểm.
- Quản lý dữ liệu địa điểm và nội dung thuyết minh.

---

## 4. Đối tượng sử dụng

### 4.1 Du khách

- Khám phá các địa điểm ăn uống trong khu phố.
- Nghe thuyết minh tự động khi đi qua các vị trí.

### 4.2 Người quản trị / quản lý nội dung

- Thêm, chỉnh sửa và quản lý thông tin địa điểm.
- Quản lý và kiểm duyệt nội dung thuyết minh (audio, văn bản).

---

## 5. Giá trị mang lại

Ứng dụng mang lại các giá trị sau:

- Giúp du khách có trải nghiệm khám phá ẩm thực **tương tác và sinh động hơn**.
- Tạo một **hệ thống hướng dẫn tự động** mà không cần hướng dẫn viên.
- Góp phần quảng bá **văn hóa ẩm thực địa phương**.
- Hỗ trợ quản lý và cập nhật nội dung một cách linh hoạt.

## 6. Problem Statement (Vấn đề cần giải quyết)

Phố ẩm thực Vĩnh Khánh là một địa điểm nổi tiếng thu hút nhiều du khách trong và ngoài nước. Tuy nhiên, khi đến tham quan và trải nghiệm ẩm thực tại đây, người dùng thường gặp một số khó khăn trong việc tìm kiếm thông tin và khám phá các quầy bán hàng.

Hiện nay, phần lớn thông tin về các quán ăn, món đặc trưng hoặc vị trí quầy bán chưa được cung cấp một cách trực quan và thuận tiện cho du khách. Điều này dẫn đến trải nghiệm tham quan và thưởng thức ẩm thực chưa được tối ưu.

Một số vấn đề phổ biến mà người dùng thường gặp:

- **Khó tìm quầy bán mong muốn**  
  Khu phố có nhiều quầy bán và hàng quán nằm gần nhau, khiến du khách khó xác định vị trí của quán ăn nổi tiếng hoặc quầy bán món mình muốn thử.

- **Không biết thông tin về menu hoặc món đặc trưng**  
  Nhiều du khách không biết quán nào nổi tiếng với món gì hoặc món nào đáng thử khi đến khu phố.

- **Thiếu thông tin giới thiệu về quán ăn**  
  Người dùng không biết lịch sử, đặc điểm nổi bật hoặc điểm đặc trưng của từng quán ăn.

- **Xếp hàng lâu tại các quán nổi tiếng**  
  Một số quán đông khách khiến người dùng phải chờ lâu mà không biết trước tình trạng đông đúc.

- **Trải nghiệm khám phá ẩm thực còn bị động**  
  Du khách thường chỉ đi dọc khu phố và chọn quán ngẫu nhiên, thiếu thông tin hướng dẫn hoặc gợi ý.

Do đó, cần có một hệ thống hỗ trợ cung cấp thông tin và thuyết minh tự động khi người dùng di chuyển trong khu vực phố ẩm thực, giúp họ dễ dàng khám phá các quán ăn và món đặc trưng một cách thuận tiện hơn.

## 7. Goals / Objectives (Mục tiêu sản phẩm)

Ứng dụng thuyết minh tự động về phố ẩm thực Vĩnh Khánh được xây dựng nhằm cải thiện trải nghiệm khám phá ẩm thực của du khách, đồng thời hỗ trợ quảng bá các quán ăn trong khu phố. Các mục tiêu chính của sản phẩm bao gồm:

- **Giảm thời gian tìm kiếm quán ăn**  
  Giúp người dùng nhanh chóng xác định vị trí các quán ăn hoặc điểm nổi bật trong khu phố thông qua hệ thống định vị và gợi ý.

- **Cung cấp thông tin món ăn rõ ràng và dễ tiếp cận**  
  Giúp người dùng biết được menu, món đặc trưng và thông tin cơ bản của quán trước khi quyết định trải nghiệm.

- **Tăng tốc độ ra quyết định của khách hàng**  
  Khi có thông tin thuyết minh và giới thiệu món ăn, người dùng có thể lựa chọn quán ăn hoặc món ăn nhanh hơn.

- **Nâng cao trải nghiệm tham quan và khám phá ẩm thực**  
  Cung cấp nội dung thuyết minh tự động giúp người dùng hiểu thêm về món ăn, quán ăn và văn hóa ẩm thực của khu phố.

- **Hỗ trợ quảng bá các quán ăn địa phương**  
  Tăng khả năng tiếp cận của các quán ăn với du khách thông qua hệ thống giới thiệu và thuyết minh tự động.

- **Góp phần tăng lượng khách và doanh thu cho các quán ăn**  
  Khi du khách dễ dàng tiếp cận thông tin và được gợi ý quán ăn, khả năng ghé thăm và sử dụng dịch vụ tại các quán sẽ tăng lên.

## 8. User Personas

Phần này mô tả các nhóm người dùng chính của hệ thống và nhu cầu của họ khi sử dụng ứng dụng thuyết minh tự động tại phố ẩm thực Vĩnh Khánh.

### 8.1 Du khách / Khách tham quan (Visitor)

**Độ tuổi:** 13+  
**Thiết bị sử dụng:** Smartphone (Android / iOS)

**Đặc điểm:**

- Thường là khách du lịch hoặc người lần đầu đến phố ẩm thực Vĩnh Khánh.
- Thích khám phá các món ăn mới và các quán nổi tiếng.
- Di chuyển trong khu phố và tìm kiếm các quán ăn đáng thử.

**Nhu cầu:**

- Biết được quán ăn nào nổi bật trong khu vực.
- Nghe giới thiệu nhanh về món ăn hoặc quán khi đi ngang qua.
- Có thông tin cơ bản như món đặc trưng, giá, vị trí.

**Pain points:**

- Không biết quán nào ngon hoặc đáng thử.
- Khó tìm vị trí quán nổi tiếng trong khu phố đông đúc.
- Thiếu thông tin về món ăn trước khi quyết định mua.

---

### 8.2 Người quản lý / Admin

**Độ tuổi:** 22 – 40+  
**Thiết bị sử dụng:** Máy tính

**Đặc điểm:**

- Quản lý nội dung của hệ thống.
- Chịu trách nhiệm cập nhật thông tin về quán ăn và nội dung thuyết minh.

**Nhu cầu:**

- Thêm mới các địa điểm hoặc quán ăn vào hệ thống.
- Chỉnh sửa thông tin như mô tả, menu, nội dung thuyết minh.
- Quản lý dữ liệu địa điểm một cách dễ dàng.

**Pain points:**

- Cần một hệ thống quản lý đơn giản và dễ sử dụng.
- Phải đảm bảo thông tin hiển thị cho người dùng luôn chính xác và cập nhật.

---

### 8.3 Chủ quán / Người bán (Seller)

**Độ tuổi:** 20 – 50  
**Thiết bị sử dụng:** Smartphone hoặc máy tính

**Đặc điểm:**

- Là chủ quán hoặc người quản lý quán ăn trong khu phố.
- Muốn quảng bá quán ăn và món đặc trưng của mình.

**Nhu cầu:**

- Quán của mình được giới thiệu cho nhiều khách hơn.
- Có thể cập nhật menu hoặc thông tin quán.

**Pain points:**

- Khó tiếp cận khách du lịch mới.
- Cạnh tranh với nhiều quán khác trong khu vực.

## 9. User Stories

Phần này mô tả các hành vi chính của người dùng khi sử dụng ứng dụng thuyết minh tự động tại phố ẩm thực Vĩnh Khánh.

Format chuẩn:

**Với vai trò là [user]**  
**Tôi muốn [action]**  
**Để [goal]**

---

### 9.1 Visitor / Du khách

**Story V1 - Mở ứng dụng nhanh**

Với vai trò là du khách  
Tôi muốn quét mã QR  
Để có thể mở nhanh ứng dụng chợ ẩm thực.

**Story V2 - Cấp quyền vị trí**

Với vai trò là du khách  
Tôi muốn ứng dụng xin quyền vị trí một cách rõ ràng  
Để tôi hiểu vì sao vị trí là cần thiết cho tính năng thuyết minh.

**Story V3 - Xem bản đồ POI**

Với vai trò là du khách  
Tôi muốn xem tất cả POI ẩm thực lân cận trên bản đồ  
Để tôi có thể chọn nơi sẽ đi tiếp theo.

**Story V4 - Xem chi tiết quán**

Với vai trò là du khách  
Tôi muốn xem chi tiết một quán ăn đã chọn  
Để tôi có thể quyết định quán đó có phù hợp với sở thích của mình hay không.

**Story V5 - Nghe thuyết minh tự động**

Với vai trò là du khách  
Tôi muốn thuyết minh tự động phát khi tôi đi vào vùng POI  
Để tôi có thể tìm hiểu thông tin mà không cần tự tìm thủ công.

**Story V6 - Đổi ngôn ngữ thuyết minh**

Với vai trò là du khách  
Tôi muốn đổi ngôn ngữ thuyết minh  
Để tôi có thể nghe bằng ngôn ngữ mình ưu tiên.

**Story V7 - Phát lại thủ công**

Với vai trò là du khách  
Tôi muốn phát lại thủ công audio của POI  
Để tôi có thể nghe lại những thông tin đã bỏ lỡ.

**Story V8 - Hoạt động khi mạng yếu**

Với vai trò là du khách  
Tôi muốn ứng dụng dùng dữ liệu POI/audio đã cache khi mạng không ổn định  
Để trải nghiệm của tôi không bị gián đoạn.

**Story V9 - Nhận gợi ý quán gần nhất**

Với vai trò là du khách  
Tôi muốn thấy POI gần nhất được làm nổi bật  
Để tôi có thể nhanh chóng khám phá các địa điểm xung quanh.

**Story V10 - Không phát lặp gây phiền**

Với vai trò là du khách  
Tôi muốn ứng dụng tránh tự động phát lặp cùng một nội dung thuyết minh trong một phiên  
Để tôi không bị khó chịu vì âm thanh lặp lại.

---

### 9.2 Seller / Chủ quán

**Story S1 - Đăng nhập quản lý quán**

Với vai trò là chủ quán  
Tôi muốn đăng nhập vào cổng quản lý của mình  
Để tôi có thể quản lý dữ liệu nhà hàng một cách an toàn.

**Story S2 - Cập nhật thông tin quán**

Với vai trò là chủ quán  
Tôi muốn chỉnh sửa thông tin hồ sơ nhà hàng  
Để du khách luôn thấy thông tin chính xác.

**Story S3 - Quản lý menu món ăn**

Với vai trò là chủ quán  
Tôi muốn tạo, cập nhật và xóa món ăn  
Để menu phản ánh đúng các món đang phục vụ.

**Story S4 - Quản lý hình ảnh quán**

Với vai trò là chủ quán  
Tôi muốn tải lên và sắp xếp lại hình ảnh nhà hàng  
Để quán của tôi trông hấp dẫn và cung cấp thông tin tốt hơn.

**Story S5 - Quản lý audio thuyết minh**

Với vai trò là chủ quán  
Tôi muốn tải lên audio thuyết minh theo từng ngôn ngữ  
Để du khách có thể nghe phần giới thiệu của quán.

**Story S6 - Bật/tắt nội dung không còn phù hợp**

Với vai trò là chủ quán  
Tôi muốn bật hoặc tắt các bản ghi audio  
Để nội dung đã lỗi thời không bị phát cho du khách.

**Story S7 - Chọn nhà hàng cần thao tác**

Với vai trò là chủ quán  
Tôi muốn chọn đúng nhà hàng trước khi chỉnh sửa dữ liệu  
Để tránh cập nhật nhầm địa điểm.

---

### 9.3 Admin / Quản trị hệ thống

**Story A1 - Quản lý tài khoản người dùng**

Với vai trò là quản trị viên  
Tôi muốn quản lý tài khoản người dùng và vai trò  
Để quyền truy cập được kiểm soát đúng.

**Story A2 - Quản lý danh mục ngôn ngữ**

Với vai trò là quản trị viên  
Tôi muốn duy trì danh mục ngôn ngữ khả dụng  
Để hệ thống hỗ trợ thuyết minh đa ngôn ngữ.

**Story A3 - Quản lý toàn bộ nhà hàng**

Với vai trò là quản trị viên  
Tôi muốn xem và cập nhật toàn bộ nhà hàng  
Để chất lượng dữ liệu được đồng nhất trong toàn hệ thống.

**Story A4 - Kiểm duyệt nội dung media**

Với vai trò là quản trị viên  
Tôi muốn kiểm duyệt hình ảnh và audio đã tải lên  
Để nội dung không phù hợp hoặc bị lỗi được loại bỏ.

**Story A5 - Cấu hình endpoint public/private**

Với vai trò là quản trị viên  
Tôi muốn kiểm soát API nào là public hoặc protected  
Để chính sách bảo mật được áp dụng đúng.

**Story A6 - Theo dõi vận hành hệ thống**

Với vai trò là quản trị viên  
Tôi muốn theo dõi sức khỏe hệ thống và hoạt động cập nhật nội dung  
Để có thể phát hiện và xử lý vấn đề sớm.

**Story A7 - Khóa/mở trạng thái nhà hàng**

Với vai trò là quản trị viên  
Tôi muốn bật hoặc tắt trạng thái hoạt động của nhà hàng  
Để các địa điểm không hợp lệ hoặc không hoạt động không ảnh hưởng đến du khách.

## 10. Features / Functional Requirements

Đây là phần đặc tả chức năng chi tiết của sản phẩm theo 3 nhóm người dùng: Visitor, Seller, Admin.

### 10.1 Visitor Features (Du khách)

**Feature V-01: Scan QR mở ứng dụng**

- Mô tả: Người dùng quét QR để mở nhanh ứng dụng tại khu phố ẩm thực.
- Actor: Visitor.
- Functional requirements:
  - Hệ thống hỗ trợ deep link mở đúng màn hình khởi động app.
  - Nếu app chưa cài, điều hướng đến trang cài đặt phù hợp nền tảng.
  - Ghi nhận sự kiện mở app từ QR để đo hiệu quả onboarding.
- Flow:
  - Quét QR
  - -> mở app
  - -> kiểm tra trạng thái đăng nhập/quyền vị trí
  - -> hiển thị bản đồ POI
  - -> phát audio nếu ở gần POI

**Feature V-02: Hiển thị bản đồ và POI**

- Mô tả: Hiển thị danh sách điểm ăn uống trên bản đồ theo dữ liệu API.
- Actor: Visitor.
- Functional requirements:
  - Tải danh sách POI từ endpoint công khai.
  - Hiển thị marker POI và vị trí hiện tại của người dùng.
  - Cho phép chọn marker để mở thông tin tóm tắt.
  - Hỗ trợ fallback cache POI khi mất mạng.
- Flow:
  - Mở app
  - -> gọi API lấy POI
  - -> render marker
  - -> người dùng chạm marker
  - -> hiển thị card thông tin

**Feature V-03: Xem chi tiết quán và menu**

- Mô tả: Người dùng xem thông tin đầy đủ của quán trước khi quyết định trải nghiệm.
- Actor: Visitor.
- Functional requirements:
  - Hiển thị tên quán, mô tả, địa chỉ.
  - Hiển thị danh sách ảnh quán.
  - Hiển thị danh sách món ăn (menu) theo quán.
  - Hiển thị audio có sẵn theo ngôn ngữ.
- Flow:
  - Chọn POI
  - -> mở trang chi tiết
  - -> gọi API ảnh + món + audio
  - -> hiển thị nội dung đầy đủ

**Feature V-04: Thuyết minh tự động theo vị trí**

- Mô tả: App tự phát audio khi người dùng vào vùng kích hoạt của POI.
- Actor: Visitor.
- Functional requirements:
  - Theo dõi vị trí theo chu kỳ khi bật narration.
  - Tính khoảng cách đến POI gần nhất.
  - Tự phát audio nếu khoảng cách <= ngưỡng trigger.
  - Không tự phát lặp lại cùng POI trong một phiên.
  - Hỗ trợ phát lại thủ công theo yêu cầu người dùng.
- Flow:
  - Bật narration
  - -> nhận cập nhật vị trí
  - -> tìm POI gần nhất
  - -> kiểm tra điều kiện trigger
  - -> phát audio

**Feature V-05: Chọn ngôn ngữ thuyết minh**

- Mô tả: Visitor chọn ngôn ngữ nghe nội dung audio.
- Actor: Visitor.
- Functional requirements:
  - Tải danh sách ngôn ngữ khả dụng từ API.
  - Lưu lựa chọn ngôn ngữ hiện tại trong phiên.
  - Khi phát audio, ưu tiên file theo ngôn ngữ đã chọn.
  - Nếu thiếu audio ngôn ngữ tương ứng, hiển thị thông báo phù hợp.
- Flow:
  - Mở cài đặt ngôn ngữ
  - -> chọn ngôn ngữ
  - -> lưu cấu hình
  - -> lần phát tiếp theo dùng ngôn ngữ mới

**Feature V-06: Offline cache cho trải nghiệm liên tục**

- Mô tả: Dữ liệu đã tải được cache để dùng khi mạng yếu.
- Actor: Visitor.
- Functional requirements:
  - Cache POI vào bộ nhớ cục bộ.
  - Cache audio đã phát theo khóa định danh.
  - Khi API lỗi/timeout, tự fallback sang cache.
  - Không chặn toàn bộ UI khi chỉ một endpoint lỗi.
- Flow:
  - App gọi API
  - -> nếu thành công thì cập nhật cache
  - -> nếu thất bại thì đọc cache
  - -> hiển thị dữ liệu khả dụng

### 10.2 Seller Features (Chủ quán)

**Feature S-01: Đăng nhập Seller Portal**

- Mô tả: Seller đăng nhập để quản lý dữ liệu quán của mình.
- Actor: Seller.
- Functional requirements:
  - Hỗ trợ đăng nhập bằng username/password.
  - Duy trì phiên làm việc bằng cookie auth.
  - Chặn truy cập route quản trị khi chưa đăng nhập.
- Flow:
  - Mở trang seller
  - -> nhập tài khoản
  - -> xác thực thành công
  - -> vào dashboard

**Feature S-02: Chọn nhà hàng cần quản lý**

- Mô tả: Seller chọn đúng nhà hàng trước khi thao tác.
- Actor: Seller.
- Functional requirements:
  - Hiển thị danh sách nhà hàng thuộc quyền seller.
  - Lưu nhà hàng đang chọn cho các màn hình chức năng.
  - Cảnh báo nếu seller thao tác khi chưa chọn nhà hàng.
- Flow:
  - Đăng nhập
  - -> chọn nhà hàng
  - -> chuyển vào dashboard theo ngữ cảnh nhà hàng

**Feature S-03: Quản lý thông tin quán (Restaurant Profile)**

- Mô tả: Seller cập nhật thông tin hồ sơ quán.
- Actor: Seller.
- Functional requirements:
  - Cho phép sửa tên, mô tả, địa chỉ, trạng thái hoạt động.
  - Validate dữ liệu đầu vào trước khi lưu.
  - Sau khi lưu thành công, hiển thị dữ liệu mới ngay trên giao diện.
- Flow:
  - Mở tab Restaurant
  - -> chỉnh thông tin
  - -> lưu
  - -> API cập nhật
  - -> hiển thị kết quả mới

**Feature S-04: Quản lý menu món ăn**

- Mô tả: Seller quản lý danh sách món ăn theo quán.
- Actor: Seller.
- Functional requirements:
  - Tạo món mới.
  - Sửa thông tin món hiện có.
  - Xóa món không còn kinh doanh.
  - Hỗ trợ phân trang khi danh sách món lớn.
- Flow:
  - Mở tab Dishes
  - -> thêm/sửa/xóa món
  - -> xác nhận thao tác
  - -> cập nhật danh sách

**Feature S-05: Quản lý hình ảnh quán**

- Mô tả: Seller upload và sắp xếp ảnh hiển thị cho quán.
- Actor: Seller.
- Functional requirements:
  - Upload ảnh mới theo nhà hàng.
  - Đặt ảnh đại diện (primary image).
  - Sắp xếp thứ tự hiển thị ảnh.
  - Xóa ảnh không còn phù hợp.
- Flow:
  - Mở tab Images
  - -> upload/chọn ảnh
  - -> đặt primary hoặc reorder
  - -> lưu
  - -> đồng bộ hiển thị

**Feature S-06: Quản lý audio thuyết minh**

- Mô tả: Seller quản lý audio giới thiệu quán theo ngôn ngữ.
- Actor: Seller.
- Functional requirements:
  - Upload audio theo language.
  - Bật/tắt trạng thái hoạt động của audio.
  - Xóa audio lỗi hoặc không còn dùng.
  - Hạn chế kích thước file upload theo cấu hình hệ thống.
- Flow:
  - Mở tab Audio
  - -> upload/chỉnh trạng thái/xóa
  - -> API xử lý
  - -> cập nhật danh sách audio

### 10.3 Admin Features (Quản trị hệ thống)

Ghi chú phạm vi:

- Nhóm A-01 đến A-05 là phạm vi đã có trong trạng thái hiện tại.
- Nhóm A-06 đến A-08 là roadmap, chưa phải module hoàn chỉnh trên admin web.

**Feature A-01: Đăng nhập quản trị**

- Mô tả: Admin đăng nhập để truy cập các chức năng quản trị hệ thống.
- Actor: Admin.
- Functional requirements:
  - Xác thực bằng tài khoản admin.
  - Dùng cookie auth cho phiên quản trị.
  - Từ chối truy cập khi user không đủ role.
- Flow:
  - Mở trang admin
  - -> đăng nhập
  - -> kiểm tra role
  - -> vào trang quản trị

**Feature A-02: Quản lý user và role**

- Mô tả: Admin quản lý tài khoản người dùng nội bộ hệ thống.
- Actor: Admin.
- Functional requirements:
  - Xem danh sách user.
  - Cập nhật role user theo chính sách phân quyền.
  - Khóa/mở trạng thái tài khoản khi cần.
- Flow:
  - Mở module Users
  - -> chọn user
  - -> chỉnh role/trạng thái
  - -> lưu thay đổi

**Feature A-03: Quản lý toàn cục nhà hàng**

- Mô tả: Admin giám sát và cập nhật trạng thái nhà hàng trên toàn hệ thống.
- Actor: Admin.
- Functional requirements:
- Xem toàn bộ nhà hàng không phụ thuộc owner.
- Cập nhật trạng thái hoạt động của nhà hàng.
- Kiểm tra tính đầy đủ dữ liệu cơ bản để phục vụ vận hành.
- Flow:
- Mở module Restaurants
- -> lọc/chọn nhà hàng
- -> chỉnh trạng thái hoặc dữ liệu
  - -> lưu

**Feature A-04: Dashboard analytics vận hành**

- Mô tả: Admin theo dõi chỉ số vận hành và hành vi nghe để ra quyết định.
- Actor: Admin.
- Functional requirements:
- Xem KPI tổng quan, heatmap, top restaurants, movement paths.
- Hỗ trợ session limit cho movement paths (bao gồm chế độ all).
- Tự refresh dữ liệu analytics theo chu kỳ phù hợp.
- Flow:
- Mở dashboard
- -> gọi analytics APIs
- -> render widgets/charts
  - -> lưu

**Feature A-05: Theo dõi recent activity và audit logs**

- Mô tả: Admin theo dõi hoạt động gần đây để phát hiện bất thường sớm.
- Actor: Admin.
- Functional requirements:
- Xem danh sách recent activity từ analytics.
- Xem audit logs phục vụ truy vết thao tác hệ thống.
- Hỗ trợ phân loại nhanh hành vi để điều tra khi cần.
- Flow:
- Mở module Logs
- -> lấy recent activity/audit logs
- -> lọc theo nhu cầu
- -> đối chiếu sự kiện

**Feature A-06 (Roadmap): Quản lý ngôn ngữ hệ thống**

- Mô tả: Duy trì danh mục ngôn ngữ phục vụ thuyết minh đa ngôn ngữ.
- Actor: Admin.
- Functional requirements:
- Xem danh sách ngôn ngữ hiện có.
- Thêm/sửa thông tin ngôn ngữ.
- Ngăn trùng mã ngôn ngữ.
- Flow:
- Mở module Languages
- -> thêm/sửa ngôn ngữ
- -> validate
- -> lưu

**Feature A-07 (Roadmap): Kiểm duyệt media và chất lượng nội dung**

- Mô tả: Admin kiểm duyệt nội dung media để đảm bảo chất lượng trải nghiệm visitor.
- Actor: Admin.
- Functional requirements:
  - Rà soát ảnh/audio bị lỗi hoặc không phù hợp.
  - Gỡ hoặc vô hiệu hóa media vi phạm.
  - Ghi nhận lịch sử thao tác kiểm duyệt.
- Flow:
  - Mở module Media Review
  - -> xem danh sách media
  - -> duyệt hoặc từ chối
  - -> cập nhật trạng thái

**Feature A-08 (Roadmap): Cấu hình chính sách bảo mật endpoint**

- Mô tả: Admin kiểm tra nhóm endpoint public/private theo chính sách hệ thống.
- Actor: Admin.
- Functional requirements:
  - Theo dõi danh sách endpoint công khai.
  - Đảm bảo endpoint nhạy cảm luôn yêu cầu xác thực.
  - Kiểm tra phản hồi 401/403 đúng chuẩn API khi không đủ quyền.
- Flow:
  - Mở module Security
  - -> xem cấu hình endpoint
  - -> cập nhật chính sách
  - -> kiểm tra lại phân quyền truy cập

### 10.4 Functional Requirement Summary

- FR-01: Visitor có thể mở app nhanh bằng QR/deep link.
- FR-02: Visitor xem bản đồ POI và chi tiết quán (ảnh, món, audio).
- FR-03: Hệ thống tự động thuyết minh theo vị trí và ngôn ngữ đã chọn.
- FR-04: Ứng dụng hỗ trợ cache POI/audio để hoạt động ổn định khi mạng yếu.
- FR-05: Seller quản lý dữ liệu nhà hàng, món ăn, ảnh, audio theo quyền sở hữu.
- FR-06: Admin quản lý user/role, restaurants, analytics và logs để vận hành hệ thống.
- FR-07: Hệ thống áp dụng phân quyền và bảo vệ endpoint theo nguyên tắc secure-by-default.
- FR-08: Các module admin nâng cao (language/media review/security) được quản lý theo roadmap.

## 11. Technical Requirements

Phần này mô tả yêu cầu kỹ thuật cốt lõi để đội dev triển khai hệ thống thống nhất.

### 11.1 Tech stack chính

- Backend: ASP.NET Core Web API (.NET 10.0), kiến trúc Model - Controller - Service - Repository.
- Database: SQL Server, truy cập qua Entity Framework Core.
- Mobile app (Visitor): .NET MAUI (Android), hỗ trợ bản đồ và audio playback.
- Web app Seller: React + Vite + TypeScript.
- Web app Admin: React + Vite + TypeScript.
- API format: REST (JSON request/response).
- Authentication: Cookie-based authentication + role claims.

### 11.2 API và tích hợp

- Public APIs cho Visitor:
  - GET /Restaurant
  - GET /Restaurant/{id}
  - GET /Language
  - GET /Restaurant/{restaurantId}/images
  - GET /public/Restaurant/{restaurantId}/dishes
  - GET /public/Restaurant/{restaurantId}/audios
- Protected APIs cho Seller/Admin:
  - CRUD Restaurant, Dish, Image, Audio
  - User/Role management
- Chuẩn dữ liệu:
  - JSON UTF-8
  - HTTP status code rõ ràng (200/400/401/403/404/500)

### 11.3 Data và media

- Dữ liệu nghiệp vụ lưu trên SQL Server.
- Ảnh và audio lưu dưới dạng file .mp3, truy cập bằng URL media do API cung cấp.
- Mobile cần cơ chế cache POI và cache audio để hỗ trợ mạng yếu/mất mạng.

### 11.4 Môi trường triển khai

- Môi trường tối thiểu:
  - Dev: local API + SQL Server
  - Staging: kiểm thử tích hợp end-to-end
  - Production: API ổn định, backup dữ liệu định kỳ
- CORS cho frontend web tại localhost ở môi trường phát triển.

### 11.5 Kiến trúc hệ thống (Architecture)

```text
+----------------------------------------------------------------------------------+
|                                   FRONTEND LAYER                                 |
|  Mobile MAUI (Visitor) | Seller Web (React + Vite) | Admin Web (React + Vite)  |
|  UI + Services + Local Cache (POI/Audio)                                          |
+----------------------------------------------------------------------------------+
                                      |
                                      | HTTPS REST + Cookie Auth
                                      v
+----------------------------------------------------------------------------------+
|                                 BACKEND LAYER                                    |
|                         ASP.NET Core Web API (.NET 10)                           |
|              Controllers -> Services -> Repositories -> EF Core                  |
|  Security: Fallback Auth Policy + Public Endpoint Convention + RBAC             |
+----------------------------------------------------------------------------------+
                    |                                           |
                    | SQL                                       | Static files
                    v                                           v
         +----------------------------+            +--------------------------------+
         | SQL Server                 |            | Media Storage                  |
         | Restaurant, Dish, Audio,   |            | /maui-images, /maui-audios,   |
         | Language, User, Image      |            | /uploads/audios               |
         +----------------------------+            +--------------------------------+
```

### 11.6 Luồng dữ liệu hoạt động (How Data Works)

```text
Visitor App Open
  -> GET /Restaurant
  -> API đọc SQL Server
  -> trả JSON POI
  -> render bản đồ + lưu offline_cache

Visitor Tap POI
  -> GET /Restaurant/{restaurantId}/images
  -> GET /public/Restaurant/{restaurantId}/dishes
  -> GET /public/Restaurant/{restaurantId}/audios
  -> hiển thị ảnh + menu + audio

Visitor Start Narration
  -> LocationService cập nhật GPS
  -> NarrationFlowService tính khoảng cách
  -> nếu đủ gần thì chọn audio theo language
  -> AudioService phát từ cache
  -> nếu chưa có cache: tải từ API -> lưu cache -> phát

Seller/Admin Data Update
  -> login (cookie auth)
  -> gọi protected APIs (CRUD)
  -> API ghi SQL + cập nhật media storage
  -> Visitor phiên sau nhận dữ liệu mới khi sync

Network Failure Fallback
  -> API timeout / mất mạng
  -> app đọc POI/audio từ cache local
  -> UI vẫn hoạt động ở chế độ offline cơ bản
```

## 12. Non-functional Requirements

### 12.1 Performance

- API read phổ biến (Restaurant/Language/public data) có thời gian phản hồi trung bình < 500ms ở điều kiện bình thường.
- API ghi dữ liệu (create/update/delete) có thời gian phản hồi trung bình < 800ms.
- Thời gian tải màn hình bản đồ ban đầu trên mobile < 3 giây với mạng ổn định.
- Thời gian bắt đầu phát audio:
  - < 2 giây với audio đã cache.
  - < 5 giây với audio tải từ mạng.

### 12.2 Security

- Mọi endpoint mặc định yêu cầu xác thực, chỉ public các endpoint được khai báo rõ ràng.
- Role-based access control cho Seller và Admin.
- Trả về 401/403 đúng chuẩn API khi chưa đăng nhập hoặc không đủ quyền.
- Không trả dữ liệu nhạy cảm (mật khẩu, hash) trong response.

### 12.3 Scalability

- Hệ thống hỗ trợ tối thiểu 1.000 người dùng hoạt động/ngày ở giai đoạn đầu.
- Cho phép mở rộng ngang API khi tăng tải (scale out).
- Thiết kế module cho phép mở rộng thêm khu ẩm thực ngoài Vĩnh Khánh.

### 12.4 Reliability và Availability

- Uptime mục tiêu cho API: >= 99.5%/tháng.
- Có fallback cache trên mobile khi API tạm thời không khả dụng.
- Upload media cần cơ chế retry hợp lý khi mạng gián đoạn.

### 12.5 Maintainability

- Tài liệu API và PRD phải cập nhật theo phiên bản phát hành.
- Coding convention thống nhất cho backend và frontend.
- Mỗi thay đổi lớn phải có test case chức năng tương ứng.

### 12.6 Observability

- Ghi log cho các sự kiện quan trọng: login, upload media, lỗi API, trigger narration.
- Có dashboard theo dõi các chỉ số vận hành chính (API latency, error rate, audio success rate).

## 13. Success Metrics

Phần này dùng để đo mức độ thành công của sản phẩm sau khi triển khai.

### 13.1 Visitor metrics

- > = 80% phiên mở app bắt đầu từ QR/deep link tại điểm tham quan.
- > = 70% visitor bật narration ít nhất 1 lần trong phiên.
- > = 60% visitor nghe ít nhất 1 audio hoàn chỉnh (>= 80% thời lượng file).
- Tỷ lệ lỗi phát audio < 3% tổng số lượt phát.
- Tỷ lệ dùng cache thành công khi offline >= 90% với dữ liệu đã từng tải.

### 13.2 Seller metrics

- > = 70% seller cập nhật nội dung quán (món/ảnh/audio) ít nhất 1 lần mỗi tuần.
- > = 90% thao tác CRUD từ seller thành công ngay lần đầu.
- Thời gian trung bình cập nhật một mục nội dung < 2 phút.

### 13.3 Admin metrics

- 100% tài khoản quản trị được gán role hợp lệ.
- > = 95% bản ghi bất thường trong recent activity/audit logs được phát hiện và xử lý trong vòng 24 giờ.
- Tỷ lệ sự cố phân quyền (truy cập sai quyền) < 1% tổng request protected.

### 13.4 System/business metrics tổng hợp

- API error rate (5xx) < 1% theo ngày.
- P95 API latency cho endpoint public < 800ms.
- Số phiên sử dụng thành công mỗi ngày tăng trưởng đều theo tháng.
- Mục tiêu giai đoạn đầu: đạt tối thiểu 500 phiên trải nghiệm/ngày trong khu vực triển khai.
