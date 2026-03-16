## Domain Entities

## 0) Bối cảnh nghiệp vụ

Hệ thống Food Market Narrator giúp du khách khám phá quán ăn tại phố ẩm thực Vĩnh Khánh bằng thuyết minh tự động theo vị trí.

Vai trò chính:

- Visitor: nghe thuyết minh khi đi gần POI.
- Seller: quản lý nội dung quán (ảnh, món, audio).
- Admin: quản lý và kiểm duyệt dữ liệu hệ thống.

## 1) Domain Entities

Language

- language_id
- language_code
- language_name

User

- user_id
- username
- password_hash
- role
- is_active
- created_at

Restaurant

- restaurant_id
- name
- description
- latitude
- longitude
- phone
- address
- open_time
- close_time
- user_id
- created_at
- is_active

RestaurantImage

- image_id
- restaurant_id
- image_url
- is_primary
- sort_order

Dish

- dish_id
- name
- price
- description
- restaurant_id
- image_id
- created_at

Audio

- audio_id
- restaurant_id
- language_id
- audio_url
- version
- is_active
- date_generation

## Entity Relationships

User

- A user can manage multiple restaurants.

Restaurant

- A restaurant belongs to one user.
- A restaurant can have multiple images.
- A restaurant can have multiple dishes.
- A restaurant can have multiple narration audios.

Audio

- Each audio belongs to a restaurant.
- Each audio belongs to a language.

## 3) Nghiệp vụ cốt lõi

- Mỗi Restaurant là một POI có tọa độ để mobile tính khoảng cách và trigger narration.
- Audio narration được gắn theo cặp Restaurant + Language.
- Restaurant có nhiều ảnh, có thể đánh dấu ảnh chính và sắp xếp thứ tự hiển thị.
- Seller chỉ nên thao tác trên nhà hàng thuộc quyền quản lý của mình (theo user_id).

## 4) Quy tắc dữ liệu quan trọng

- restaurant_id là khóa nhận diện dùng xuyên suốt mobile/frontend/API.
- language_code (vi-VN, en-US, zh-CN, ko-KR, ja-JP) quyết định file audio phát ra.
- is_active trên Restaurant và Audio kiểm soát dữ liệu hiển thị/phát thực tế.
