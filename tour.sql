CREATE TABLE dbo.Tour (
    tour_id INT IDENTITY(1,1) PRIMARY KEY,
    -- tour_code VARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(200) NOT NULL,
    short_description NVARCHAR(500) NULL,
    description NVARCHAR(MAX) NULL,
    estimated_duration_minutes INT NULL,
    image_id INT NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    is_featured BIT NOT NULL DEFAULT 0,
    sort_priority INT NOT NULL DEFAULT 0,
    created_by INT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    updated_by INT NULL,
    updated_at DATETIME2 NULL,
    CONSTRAINT FK_Tour_CreatedBy FOREIGN KEY (created_by) REFERENCES dbo.Users(user_id),
    CONSTRAINT FK_Tour_UpdatedBy FOREIGN KEY (updated_by) REFERENCES dbo.Users(user_id),
    CONSTRAINT FK_Tour_Image FOREIGN KEY (image_id) REFERENCES dbo.Restaurant_Image(image_id),
    CONSTRAINT CK_Tour_Duration CHECK (estimated_duration_minutes IS NULL OR estimated_duration_minutes > 0)
);

CREATE TABLE dbo.Tour_Restaurant (
    tour_id INT NOT NULL,
    restaurant_id VARCHAR(100) NOT NULL,
    stop_order INT NOT NULL,
    -- stay_minutes INT NULL,
    -- is_must_visit BIT NOT NULL DEFAULT 1,
    custom_radius_meters INT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    PRIMARY KEY (tour_id, restaurant_id),
    CONSTRAINT UQ_Tour_Restaurant_Order UNIQUE (tour_id, stop_order),
    CONSTRAINT FK_Tour_Restaurant_Tour FOREIGN KEY (tour_id) REFERENCES dbo.Tour(tour_id) ON DELETE CASCADE,
    CONSTRAINT FK_Tour_Restaurant_Restaurant FOREIGN KEY (restaurant_id) REFERENCES dbo.Restaurant(restaurant_id),
    -- CONSTRAINT CK_Tour_Restaurant_Stay CHECK (stay_minutes IS NULL OR stay_minutes > 0),
    CONSTRAINT CK_Tour_Restaurant_Radius CHECK (custom_radius_meters IS NULL OR custom_radius_meters > 0)
);

CREATE INDEX IX_Tour_Active_Priority ON dbo.Tour(is_active, is_featured, sort_priority);
CREATE INDEX IX_Tour_Restaurant_Restaurant ON dbo.Tour_Restaurant(restaurant_id);


-- Sample data insertion for testing

-- Cleanup old sample tours (safe to rerun)
DELETE FROM dbo.Tour
WHERE name IN (
    N'Tour Oc Vinh Khanh',
    N'Tour Nuong Lau Buoi Toi',
    N'Tour Gia Dinh Cuoi Tuan',
    N'Tour Hai San Signature'
);

-- Insert 4 sample tours
INSERT INTO dbo.Tour (
    name,
    short_description,
    description,
    image_id,
    estimated_duration_minutes,
    is_active,
    is_featured,
    sort_priority,
    created_by,
    updated_by
)
VALUES
(
    N'Tour Oc Vinh Khanh',
    N'Cluster cac quan oc noi bat tren duong Vinh Khanh.',
    N'Phu hop cho visitor muon trai nghiem nhieu mon oc, di bo ngan va dung chan nhanh.',
    16,
    120,
    1,
    1,
    100,
    NULL,
    NULL
),
(
    N'Tour Nuong Lau Buoi Toi',
    N'Tour danh cho nhom ban thich nuong va lau vao buoi toi.',
    N'Ket hop cac diem nuong lau pho bien, phu hop 2-4 nguoi.',
    1,
    150,
    1,
    1,
    90,
    NULL,
    NULL
),
(
    N'Tour Gia Dinh Cuoi Tuan',
    N'Tour de di cho gia dinh, diem dung can bang giua khong gian va mon an.',
    N'Lich trinh de chiu, co nhieu lua chon mon nuong hai san va nha hang co khong gian rong.',
    4,
    180,
    1,
    0,
    80,
    NULL,
    NULL
),
(
    N'Tour Hai San Signature',
    N'Chon loc cac diem hai san duoc visitor quan tam nhieu.',
    N'Tour tap trung vao nhom mon hai san va oc signature de visitor de chon quan.',
    10,
    165,
    1,
    0,
    85,
    NULL,
    NULL
);

DECLARE @TourOcVinhKhanhId INT;
DECLARE @TourNuongLauId INT;
DECLARE @TourGiaDinhId INT;
DECLARE @TourHaiSanId INT;

SET @TourOcVinhKhanhId = (SELECT TOP 1 tour_id FROM dbo.Tour WHERE name = N'Tour Oc Vinh Khanh' ORDER BY tour_id DESC);
SET @TourNuongLauId = (SELECT TOP 1 tour_id FROM dbo.Tour WHERE name = N'Tour Nuong Lau Buoi Toi' ORDER BY tour_id DESC);
SET @TourGiaDinhId = (SELECT TOP 1 tour_id FROM dbo.Tour WHERE name = N'Tour Gia Dinh Cuoi Tuan' ORDER BY tour_id DESC);
SET @TourHaiSanId = (SELECT TOP 1 tour_id FROM dbo.Tour WHERE name = N'Tour Hai San Signature' ORDER BY tour_id DESC);

-- Tour 1: 8 restaurants
INSERT INTO dbo.Tour_Restaurant (tour_id, restaurant_id, stop_order, custom_radius_meters)
VALUES
(@TourOcVinhKhanhId, 'oc-loan', 1, NULL),
(@TourOcVinhKhanhId, 'oc-oanh', 2, NULL),
(@TourOcVinhKhanhId, 'oc-phat', 3, NULL),
(@TourOcVinhKhanhId, 'oc-cuc-vinh-khanh', 4, NULL),
(@TourOcVinhKhanhId, 'oc-hoa-kieu', 5, NULL),
(@TourOcVinhKhanhId, 'oc-hong-nhung', 6, NULL),
(@TourOcVinhKhanhId, 'quan-oc-thao', 7, NULL),
(@TourOcVinhKhanhId, 'quan-oc-vu', 8, NULL);

-- Tour 2: 6 restaurants
INSERT INTO dbo.Tour_Restaurant (tour_id, restaurant_id, stop_order, custom_radius_meters)
VALUES
(@TourNuongLauId, 'chilli-bbq-hotpot-restaurant', 1, NULL),
(@TourNuongLauId, 'lau-met-nuong-79k', 2, NULL),
(@TourNuongLauId, 'lau-nuong-thuan-viet', 3, NULL),
(@TourNuongLauId, 'sot-lau-alo-quan', 4, NULL),
(@TourNuongLauId, 'them-nuong-yakiniku', 5, NULL),
(@TourNuongLauId, 'the-gioi-bo', 6, NULL);

-- Tour 3: 5 restaurants
INSERT INTO dbo.Tour_Restaurant (tour_id, restaurant_id, stop_order, custom_radius_meters)
VALUES
(@TourGiaDinhId, 'lang-restaurant', 1, NULL),
(@TourGiaDinhId, 'the-gioi-bo', 2, NULL),
(@TourGiaDinhId, 'quan-bo-oc', 3, NULL),
(@TourGiaDinhId, 'chilli-bbq-hotpot-restaurant', 4, NULL),
(@TourGiaDinhId, 'oc-cuc-vinh-khanh', 5, NULL);

-- Tour 4: 7 restaurants
INSERT INTO dbo.Tour_Restaurant (tour_id, restaurant_id, stop_order, custom_radius_meters)
VALUES
(@TourHaiSanId, 'quan-bo-oc', 1, NULL),
(@TourHaiSanId, 'oc-cuc-vinh-khanh', 2, NULL),
(@TourHaiSanId, 'oc-hong-nhung', 3, NULL),
(@TourHaiSanId, 'oc-phat', 4, NULL),
(@TourHaiSanId, 'oc-oanh', 5, NULL),
(@TourHaiSanId, 'quan-oc-vu', 6, NULL),
(@TourHaiSanId, 'lang-restaurant', 7, NULL);

