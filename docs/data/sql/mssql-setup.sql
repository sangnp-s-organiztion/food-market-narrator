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


-- food_market_narrator.dbo.Tour definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Tour;

CREATE TABLE food_market_narrator.dbo.Tour (
	tour_id int IDENTITY(1,1) NOT NULL,
	name nvarchar(200) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	description nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	estimated_duration_minutes int NULL,
	is_active bit DEFAULT 1 NOT NULL,
	created_at datetime2 DEFAULT sysdatetime() NOT NULL,
	url_image varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK__Tour__4B16B9E6D6265369 PRIMARY KEY (tour_id)
);
ALTER TABLE food_market_narrator.dbo.Tour WITH NOCHECK ADD CONSTRAINT CK_Tour_Duration CHECK (([estimated_duration_minutes] IS NULL OR [estimated_duration_minutes]>(0)));


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
	phone varchar(15) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	email varchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	full_name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
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


-- food_market_narrator.dbo.Tour_Restaurant definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.Tour_Restaurant;

CREATE TABLE food_market_narrator.dbo.Tour_Restaurant (
	tour_id int NOT NULL,
	restaurant_id varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	stop_order int NOT NULL,
	created_at datetime2 DEFAULT sysdatetime() NOT NULL,
	CONSTRAINT PK__Tour_POI__48A6434F6AECBDED PRIMARY KEY (tour_id,restaurant_id),
	CONSTRAINT UQ_Tour_POI_Order UNIQUE (tour_id,stop_order),
	CONSTRAINT FK_Tour_POI_Restaurant FOREIGN KEY (restaurant_id) REFERENCES food_market_narrator.dbo.Restaurant(restaurant_id),
	CONSTRAINT FK_Tour_POI_Tour FOREIGN KEY (tour_id) REFERENCES food_market_narrator.dbo.Tour(tour_id) ON DELETE CASCADE
);
 CREATE NONCLUSTERED INDEX IX_Tour_POI_Restaurant ON food_market_narrator.dbo.Tour_Restaurant (  restaurant_id ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- food_market_narrator.dbo.[Translation] definition

-- Drop table

-- DROP TABLE food_market_narrator.dbo.[Translation];

CREATE TABLE food_market_narrator.dbo.[Translation] (
	translation_id int IDENTITY(1,1) NOT NULL,
	entity_type nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	entity_id varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	language_id int NOT NULL,
	field_name nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	translated_text nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	created_at datetime DEFAULT getdate() NULL,
	CONSTRAINT PK__translat__23DB90A436AF4C8A PRIMARY KEY (translation_id),
	CONSTRAINT FK_translation_language FOREIGN KEY (language_id) REFERENCES food_market_narrator.dbo.Languages(language_id) ON DELETE CASCADE
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
	created_at datetime DEFAULT getdate() NULL,
	restaurant_id varchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	image_id int NULL,
	is_active bit NULL,
	CONSTRAINT PK__Dish__9F2B4CF92E013256 PRIMARY KEY (dish_id),
	CONSTRAINT FK__Dish__image_id__5070F446 FOREIGN KEY (image_id) REFERENCES food_market_narrator.dbo.Restaurant_Image(image_id),
	CONSTRAINT FK__Dish__restaurant__4F7CD00D FOREIGN KEY (restaurant_id) REFERENCES food_market_narrator.dbo.Restaurant(restaurant_id)
);