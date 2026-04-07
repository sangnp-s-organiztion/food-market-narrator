-- food_market_narrator.dbo.Languages definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Languages;

CREATE TABLE food_market_narrator.dbo.Languages (
	language_id int IDENTITY(1,1) NOT NULL,
	language_code nvarchar(10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	language_name nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK__Language__804CF6B34A65DBED PRIMARY KEY (language_id),
	CONSTRAINT UQ__Language__A6D3AFDB4D5C6A57 UNIQUE (language_code)
);


-- food_market_narrator.dbo.Users definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Users;

CREATE TABLE food_market_narrator.dbo.Users (
	user_id int IDENTITY(1,1) NOT NULL,
	username nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	password_hash nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[role] nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	is_active bit DEFAULT 1 NULL,
	created_at datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__Users__B9BE370F2E780BE1 PRIMARY KEY (user_id),
	CONSTRAINT UQ__Users__F3DBC5728C03A3C7 UNIQUE (username)
);
ALTER TABLE food_market_narrator.dbo.Users WITH NOCHECK ADD CONSTRAINT CK__Users__role__412EB0B6 CHECK (([role]='Saler' OR [role]='Admin'));


-- food_market_narrator.dbo.Restaurant definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Restaurant;

CREATE TABLE food_market_narrator.dbo.Restaurant (
	restaurant_id varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	latitude decimal(10,6) NULL,
	longitude decimal(9,6) NULL,
	phone varchar(10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	address nvarchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	is_active bit DEFAULT 1 NOT NULL,
	created_at datetime2 DEFAULT sysdatetime() NOT NULL,
	user_id int NULL,
	open_time time(3) NULL,
	close_time time(3) NULL,
	CONSTRAINT PK__Restaura__3B0FAA9117414332 PRIMARY KEY (restaurant_id),
	CONSTRAINT FK_Restaurant_User FOREIGN KEY (user_id) REFERENCES food_market_narrator.dbo.Users(user_id)
);


-- food_market_narrator.dbo.Restaurant_Image definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Restaurant_Image;

CREATE TABLE food_market_narrator.dbo.Restaurant_Image (
	image_id int IDENTITY(1,1) NOT NULL,
	restaurant_id varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	image_url varchar(500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	is_primary bit DEFAULT 0 NOT NULL,
	sort_order int DEFAULT 0 NOT NULL,
	CONSTRAINT PK__Restaura__DC9AC955774F0724 PRIMARY KEY (image_id),
	CONSTRAINT FK__Restauran__resta__4BAC3F29 FOREIGN KEY (restaurant_id) REFERENCES food_market_narrator.dbo.Restaurant(restaurant_id) ON DELETE CASCADE
);


-- food_market_narrator.dbo.Audio definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Audio;

CREATE TABLE food_market_narrator.dbo.Audio (
	audio_id int IDENTITY(1,1) NOT NULL,
	restaurant_id varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	language_id int NOT NULL,
	audio_url nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	version int NOT NULL,
	is_active bit DEFAULT 1 NOT NULL,
	date_generation datetime2 DEFAULT getdate() NULL,
	CONSTRAINT PK__Audio__D71A93E7C7FA3A98 PRIMARY KEY (audio_id),
	CONSTRAINT FK__Audio__language___46E78A0C FOREIGN KEY (language_id) REFERENCES food_market_narrator.dbo.Languages(language_id),
	CONSTRAINT FK__Audio__restauran__45F365D3 FOREIGN KEY (restaurant_id) REFERENCES food_market_narrator.dbo.Restaurant(restaurant_id)
);


-- food_market_narrator.dbo.Dish definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Dish;

CREATE TABLE food_market_narrator.dbo.Dish (
	dish_id int IDENTITY(1,1) NOT NULL,
	name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	price decimal(10,2) NULL,
	description nvarchar(1000) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	created_at datetime DEFAULT getdate() NULL,
	restaurant_id varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	image_id int NULL,
	is_active bit NULL,
	CONSTRAINT PK__Dish__9F2B4CF92E013256 PRIMARY KEY (dish_id),
	CONSTRAINT FK__Dish__image_id__5070F446 FOREIGN KEY (image_id) REFERENCES food_market_narrator.dbo.Restaurant_Image(image_id),
	CONSTRAINT FK__Dish__restaurant__4F7CD00D FOREIGN KEY (restaurant_id) REFERENCES food_market_narrator.dbo.Restaurant(restaurant_id)
);

CREATE TABLE dbo.Tour (
    tour_id INT IDENTITY(1,1) PRIMARY KEY,
    -- tour_code VARCHAR(50) NOT NULL UNIQUE,
    name NVARCHAR(200) NOT NULL,
    short_description NVARCHAR(500) NULL,
    description NVARCHAR(MAX) NULL,
    estimated_duration_minutes INT NULL,
    image_url NVARCHAR(500) NULL,
    is_active BIT NOT NULL DEFAULT 1,
    is_featured BIT NOT NULL DEFAULT 0,
    sort_priority INT NOT NULL DEFAULT 0,
    created_by INT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    updated_by INT NULL,
    updated_at DATETIME2 NULL,
    CONSTRAINT FK_Tour_CreatedBy FOREIGN KEY (created_by) REFERENCES dbo.Users(user_id),
    CONSTRAINT FK_Tour_UpdatedBy FOREIGN KEY (updated_by) REFERENCES dbo.Users(user_id),
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
