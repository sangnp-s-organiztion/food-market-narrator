// Mock data for the admin dashboard

export type EntityStatus = 'active' | 'inactive';

export interface Restaurant {
  restaurant_id: number;
  name: string;
  description: string;
  latitude: number;
  longitude: number;
  phone: string;
  address: string;
  status: EntityStatus;
  created_at: string;
  user_id: number;
  open_time: string;
  close_time: string;
}

export interface RestaurantImage {
  image_id: number;
  restaurant_id: number;
  image_url: string;
  is_primary: boolean;
  sort_order: number;
}

export interface Dish {
  dish_id: number;
  name: string;
  price: number;
  description: string;
  created_at: string;
  restaurant_id: number;
  image_id: number | null;
  status: EntityStatus;
}

export interface Audio {
  audio_id: number;
  restaurant_id: number;
  language_id: number;
  audio_url: string;
  version: string;
  status: EntityStatus;
  date_generation: string;
}

export interface Language {
  language_id: number;
  language_code: string;
  language_name: string;
}

export interface User {
  user_id: number;
  username: string;
  password_hash: string;
  role: 'admin' | 'editor';
  is_active: boolean;
  created_at: string;
}

export interface ActivityLog {
  id: number;
  user: string;
  action: string;
  target: string;
  target_name: string;
  timestamp: string;
}

export const restaurants: Restaurant[] = [
  { restaurant_id: 1, name: "Phở Hà Nội", description: "Phở truyền thống Hà Nội", latitude: 21.0285, longitude: 105.8542, phone: "024-3825-1234", address: "15 Hàng Bông, Hoàn Kiếm, Hà Nội", status: "active", created_at: "2024-01-15", user_id: 1, open_time: "06:00", close_time: "22:00" },
  { restaurant_id: 2, name: "Bún Chả Đắc Kim", description: "Bún chả nổi tiếng Hà Nội", latitude: 21.0335, longitude: 105.8490, phone: "024-3826-5678", address: "1 Hàng Mành, Hoàn Kiếm, Hà Nội", status: "active", created_at: "2024-02-20", user_id: 1, open_time: "10:00", close_time: "20:00" },
  { restaurant_id: 3, name: "Cơm Tấm Sài Gòn", description: "Cơm tấm sườn bì chả", latitude: 10.7769, longitude: 106.7009, phone: "028-3822-9999", address: "260 Phan Xích Long, Phú Nhuận, TP.HCM", status: "active", created_at: "2024-03-05", user_id: 2, open_time: "06:30", close_time: "21:00" },
  { restaurant_id: 4, name: "Bánh Mì Huỳnh Hoa", description: "Bánh mì nổi tiếng Sài Gòn", latitude: 10.7721, longitude: 106.6922, phone: "028-3925-1111", address: "26 Lê Thị Riêng, Quận 1, TP.HCM", status: "inactive", created_at: "2024-03-10", user_id: 2, open_time: "15:30", close_time: "23:00" },
  { restaurant_id: 5, name: "Nem Nướng Nha Trang", description: "Nem nướng đặc sản Nha Trang", latitude: 12.2388, longitude: 109.1967, phone: "058-352-2222", address: "16A Lãn Ông, Nha Trang, Khánh Hòa", status: "active", created_at: "2024-04-01", user_id: 1, open_time: "09:00", close_time: "21:30" },
  { restaurant_id: 6, name: "Mì Quảng Bà Mua", description: "Mì Quảng truyền thống Đà Nẵng", latitude: 16.0544, longitude: 108.2022, phone: "0236-382-3333", address: "19 Trần Bình Trọng, Hải Châu, Đà Nẵng", status: "active", created_at: "2024-04-15", user_id: 1, open_time: "06:00", close_time: "14:00" },
  { restaurant_id: 7, name: "Bún Bò Huế O Xuân", description: "Bún bò Huế chuẩn vị", latitude: 16.4637, longitude: 107.5909, phone: "0234-382-4444", address: "5 Nguyễn Du, TP Huế", status: "active", created_at: "2024-05-01", user_id: 2, open_time: "05:30", close_time: "20:00" },
  { restaurant_id: 8, name: "Cao Lầu Hội An", description: "Cao lầu phố cổ", latitude: 15.8801, longitude: 108.3380, phone: "0235-386-5555", address: "12 Trần Phú, Hội An, Quảng Nam", status: "inactive", created_at: "2024-05-20", user_id: 1, open_time: "08:00", close_time: "22:00" },
];

export const dishes: Dish[] = [
  { dish_id: 1, name: "Phở bò tái", price: 55000, description: "Phở bò tái chín truyền thống", created_at: "2024-01-15", restaurant_id: 1, image_id: null, status: "active" },
  { dish_id: 2, name: "Phở gà", price: 50000, description: "Phở gà ta thả vườn", created_at: "2024-01-15", restaurant_id: 1, image_id: null, status: "active" },
  { dish_id: 3, name: "Bún chả Hà Nội", price: 45000, description: "Bún chả nướng than hoa", created_at: "2024-02-20", restaurant_id: 2, image_id: null, status: "active" },
  { dish_id: 4, name: "Nem rán", price: 30000, description: "Nem rán giòn rụm", created_at: "2024-02-20", restaurant_id: 2, image_id: null, status: "inactive" },
  { dish_id: 5, name: "Cơm tấm sườn bì chả", price: 45000, description: "Sườn nướng, bì, chả trứng", created_at: "2024-03-05", restaurant_id: 3, image_id: null, status: "active" },
  { dish_id: 6, name: "Bánh mì đặc biệt", price: 47000, description: "Bánh mì thịt nguội đầy đủ", created_at: "2024-03-10", restaurant_id: 4, image_id: null, status: "active" },
  { dish_id: 7, name: "Nem nướng Nha Trang", price: 60000, description: "Set nem nướng cuốn bánh tráng", created_at: "2024-04-01", restaurant_id: 5, image_id: null, status: "active" },
  { dish_id: 8, name: "Mì Quảng tôm thịt", price: 40000, description: "Mì Quảng tôm thịt truyền thống", created_at: "2024-04-15", restaurant_id: 6, image_id: null, status: "active" },
  { dish_id: 9, name: "Bún bò Huế đặc biệt", price: 50000, description: "Bún bò giò heo, chả cua", created_at: "2024-05-01", restaurant_id: 7, image_id: null, status: "active" },
  { dish_id: 10, name: "Cao lầu", price: 45000, description: "Cao lầu đặc sản Hội An", created_at: "2024-05-20", restaurant_id: 8, image_id: null, status: "active" },
];

export const languages: Language[] = [
  { language_id: 1, language_code: "vi", language_name: "Tiếng Việt" },
  { language_id: 2, language_code: "en", language_name: "English" },
  { language_id: 3, language_code: "ja", language_name: "日本語" },
  { language_id: 4, language_code: "ko", language_name: "한국어" },
  { language_id: 5, language_code: "zh", language_name: "中文" },
];

export const audios: Audio[] = [
  { audio_id: 1, restaurant_id: 1, language_id: 1, audio_url: "/audio/pho-hanoi-vi.mp3", version: "v1.2.0", status: "active", date_generation: "2024-06-01" },
  { audio_id: 2, restaurant_id: 1, language_id: 2, audio_url: "/audio/pho-hanoi-en.mp3", version: "v1.1.0", status: "active", date_generation: "2024-06-02" },
  { audio_id: 3, restaurant_id: 2, language_id: 1, audio_url: "/audio/buncha-vi.mp3", version: "v2.0.0", status: "active", date_generation: "2024-06-05" },
  { audio_id: 4, restaurant_id: 3, language_id: 1, audio_url: "/audio/comtam-vi.mp3", version: "v1.0.0", status: "active", date_generation: "2024-06-10" },
  { audio_id: 5, restaurant_id: 3, language_id: 2, audio_url: "/audio/comtam-en.mp3", version: "v1.0.0", status: "inactive", date_generation: "2024-06-11" },
  { audio_id: 6, restaurant_id: 5, language_id: 1, audio_url: "/audio/nemnuong-vi.mp3", version: "v1.3.0", status: "active", date_generation: "2024-06-15" },
  { audio_id: 7, restaurant_id: 6, language_id: 1, audio_url: "/audio/miquang-vi.mp3", version: "v1.0.0", status: "active", date_generation: "2024-06-20" },
  { audio_id: 8, restaurant_id: 7, language_id: 2, audio_url: "/audio/bunbo-en.mp3", version: "v1.1.0", status: "active", date_generation: "2024-07-01" },
];

export const users: User[] = [
  { user_id: 1, username: "admin", password_hash: "***", role: "admin", is_active: true, created_at: "2024-01-01" },
  { user_id: 2, username: "editor1", password_hash: "***", role: "editor", is_active: true, created_at: "2024-01-15" },
  { user_id: 3, username: "editor2", password_hash: "***", role: "editor", is_active: true, created_at: "2024-02-01" },
  { user_id: 4, username: "nguyenvan", password_hash: "***", role: "editor", is_active: false, created_at: "2024-03-01" },
];

export const activityLogs: ActivityLog[] = [
  { id: 1, user: "admin", action: "LOCK", target: "Restaurant", target_name: "Bánh Mì Huỳnh Hoa", timestamp: "2024-06-01 09:15:22" },
  { id: 2, user: "admin", action: "LOCK", target: "Restaurant", target_name: "Cao Lầu Hội An", timestamp: "2024-06-02 14:30:11" },
  { id: 3, user: "admin", action: "UNLOCK", target: "Restaurant", target_name: "Bánh Mì Huỳnh Hoa", timestamp: "2024-06-03 10:45:00" },
  { id: 4, user: "admin", action: "DISABLE", target: "Audio", target_name: "comtam-en.mp3", timestamp: "2024-06-04 11:20:33" },
  { id: 5, user: "admin", action: "UPDATE", target: "Restaurant", target_name: "Phở Hà Nội", timestamp: "2024-06-05 08:10:15" },
  { id: 6, user: "admin", action: "LOCK", target: "Dish", target_name: "Nem rán", timestamp: "2024-06-06 16:55:42" },
  { id: 7, user: "admin", action: "DISABLE", target: "Audio", target_name: "bunbo-en.mp3", timestamp: "2024-06-07 13:25:18" },
  { id: 8, user: "admin", action: "UNLOCK", target: "Dish", target_name: "Nem rán", timestamp: "2024-06-08 09:40:55" },
];

// Chart data
export const dailyListens = [
  { date: "01/06", listens: 245 },
  { date: "02/06", listens: 312 },
  { date: "03/06", listens: 289 },
  { date: "04/06", listens: 456 },
  { date: "05/06", listens: 398 },
  { date: "06/06", listens: 521 },
  { date: "07/06", listens: 478 },
  { date: "08/06", listens: 612 },
  { date: "09/06", listens: 534 },
  { date: "10/06", listens: 489 },
  { date: "11/06", listens: 567 },
  { date: "12/06", listens: 623 },
  { date: "13/06", listens: 701 },
  { date: "14/06", listens: 658 },
];

export const topRestaurants = [
  { name: "Phở Hà Nội", listens: 4829 },
  { name: "Bún Chả Đắc Kim", listens: 3567 },
  { name: "Cơm Tấm Sài Gòn", listens: 2890 },
  { name: "Nem Nướng Nha Trang", listens: 2145 },
  { name: "Mì Quảng Bà Mua", listens: 1823 },
];

// Heatmap data points: [lat, lng, intensity]
export const heatmapData: [number, number, number][] = [
  [21.0285, 105.8542, 0.9],
  [21.0335, 105.8490, 0.7],
  [21.0300, 105.8510, 0.5],
  [21.0250, 105.8560, 0.4],
  [10.7769, 106.7009, 0.8],
  [10.7721, 106.6922, 0.6],
  [10.7750, 106.6960, 0.5],
  [12.2388, 109.1967, 0.6],
  [16.0544, 108.2022, 0.5],
  [16.4637, 107.5909, 0.4],
  [15.8801, 108.3380, 0.3],
  [21.0310, 105.8520, 0.6],
  [21.0270, 105.8550, 0.3],
  [10.7740, 106.6980, 0.4],
];

// User path data
export const userPaths = [
  {
    userId: 1,
    username: "visitor_001",
    points: [
      { lat: 21.0285, lng: 105.8542, restaurant: "Phở Hà Nội", duration: 142 },
      { lat: 21.0335, lng: 105.8490, restaurant: "Bún Chả Đắc Kim", duration: 98 },
      { lat: 21.0300, lng: 105.8510, restaurant: "Quán Bia Hơi", duration: 45 },
    ],
  },
  {
    userId: 2,
    username: "visitor_002",
    points: [
      { lat: 10.7769, lng: 106.7009, restaurant: "Cơm Tấm Sài Gòn", duration: 180 },
      { lat: 10.7721, lng: 106.6922, restaurant: "Bánh Mì Huỳnh Hoa", duration: 65 },
    ],
  },
];
