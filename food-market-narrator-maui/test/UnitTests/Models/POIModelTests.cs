using food_market_narrator.Models;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Models;

/// <summary>
/// Unit Tests cho POI Model
/// </summary>
public class POIModelTests
{
    #region 1. Theo dõi vị trí - Map Display Tests

    [Fact]
    public void POI_DefaultRadius_ShouldBe500Meters()
    {
        // Arrange & Act
        var poi = new POI();

        // Assert
        Assert.Equal(500, poi.Radius);
    }

    [Fact]
    public void POI_DefaultIsActive_ShouldBeFalse()
    {
        // Arrange & Act
        var poi = new POI();

        // Assert
        Assert.False(poi.IsActive);
    }

    [Fact]
    public void POI_StatusText_WhenActive_ShouldReturnMoCua()
    {
        // Arrange
        var poi = new POI { IsActive = true };

        // Act & Assert
        Assert.Equal("Đang mở cửa", poi.StatusText);
    }

    [Fact]
    public void POI_StatusText_WhenInactive_ShouldReturnDongCua()
    {
        // Arrange
        var poi = new POI { IsActive = false };

        // Act & Assert
        Assert.Equal("Đóng cửa", poi.StatusText);
    }

    #endregion

    #region 2. Hiển thị bản đồ - Map Display Tests

    [Fact]
    public void POI_PrimaryImage_WhenNoImages_ShouldReturnDefaultImage()
    {
        // Arrange
        var poi = new POI { Images = new List<RestaurantImageModel>() };

        // Act
        var primaryImage = poi.PrimaryImage;

        // Assert
        Assert.Equal("dotnet_bot.svg", primaryImage);
    }

    [Fact]
    public void POI_PrimaryImage_WhenHasImages_ShouldReturnPrimaryImage()
    {
        // Arrange
        var poi = new POI
        {
            Images = new List<RestaurantImageModel>
            {
                new() { ImageUrl = "Resources/Images/secondary.jpg", IsPrimary = false },
                new() { ImageUrl = "Resources/Images/primary.jpg", IsPrimary = true, SortOrder = 1 }
            }
        };

        // Act
        var primaryImage = poi.PrimaryImage;

        // Assert
        Assert.Equal("primary.jpg", primaryImage);
    }

    [Fact]
    public void POI_PrimaryImage_WhenNoPrimaryImage_ShouldReturnFirstImage()
    {
        // Arrange
        var poi = new POI
        {
            Images = new List<RestaurantImageModel>
            {
                new() { ImageUrl = "Resources/Images/first.jpg", SortOrder = 2 },
                new() { ImageUrl = "Resources/Images/second.jpg", SortOrder = 1 }
            }
        };

        // Act
        var primaryImage = poi.PrimaryImage;

        // Assert
        Assert.Equal("second.jpg", primaryImage);
    }

    [Fact]
    public void POI_OpeningHoursDisplay_WhenEmpty_ShouldReturnDefault()
    {
        // Arrange
        var poi = new POI { OpeningHours = null };

        // Act & Assert
        Assert.Equal("08:00 - 22:00", poi.OpeningHoursDisplay);
    }

    [Fact]
    public void POI_OpeningHoursDisplay_WhenHasValue_ShouldReturnValue()
    {
        // Arrange
        var poi = new POI { OpeningHours = "09:00 - 21:00" };

        // Act & Assert
        Assert.Equal("09:00 - 21:00", poi.OpeningHoursDisplay);
    }

    [Fact]
    public void POI_AddressDisplay_WhenEmpty_ShouldReturnDefault()
    {
        // Arrange
        var poi = new POI { Address = null };

        // Act & Assert
        Assert.Equal("Đang cập nhật địa chỉ", poi.AddressDisplay);
    }

    [Fact]
    public void POI_AddressDisplay_WhenHasValue_ShouldReturnValue()
    {
        // Arrange
        var poi = new POI { Address = "123 Đường Nguyễn Trãi, Quận 1" };

        // Act & Assert
        Assert.Equal("123 Đường Nguyễn Trãi, Quận 1", poi.AddressDisplay);
    }

    #endregion

    #region 3. Thuyết minh tự động (Kích hoạt Geofence) - Geofence Tests

    [Fact]
    public void POI_GetAudioUrl_WithMatchingLanguage_ShouldReturnAudioUrl()
    {
        // Arrange
        var poi = new POI
        {
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "en", AudioUrl = "audio/en/test.mp3", IsActive = true, Version = 1 },
                new() { LanguageCode = "vi", AudioUrl = "audio/vi/test.mp3", IsActive = true, Version = 1 }
            }
        };

        // Act
        var audioUrl = poi.GetAudioUrl("en");

        // Assert
        Assert.Equal("audio/en/test.mp3", audioUrl);
    }

    [Fact]
    public void POI_GetAudioUrl_NoMatchingLanguage_ShouldReturnFirstActiveAudio()
    {
        // Arrange
        var poi = new POI
        {
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "ja", AudioUrl = "audio/ja/test.mp3", IsActive = true, Version = 1 },
                new() { LanguageCode = "ko", AudioUrl = "audio/ko/test.mp3", IsActive = true, Version = 1 }
            }
        };

        // Act
        var audioUrl = poi.GetAudioUrl("vi");

        // Assert
        Assert.Equal("audio/ja/test.mp3", audioUrl);
    }

    [Fact]
    public void POI_GetAudioUrl_NoActiveAudios_ShouldReturnNull()
    {
        // Arrange
        var poi = new POI
        {
            Audios = new List<AudioModel>()
        };

        // Act
        var audioUrl = poi.GetAudioUrl("en");

        // Assert
        Assert.Null(audioUrl);
    }

    [Fact]
    public void POI_GetAudioUrl_WithInactiveAudios_ShouldReturnNull()
    {
        // Arrange
        var poi = new POI
        {
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "en", AudioUrl = "audio/en/test.mp3", IsActive = false }
            }
        };

        // Act
        var audioUrl = poi.GetAudioUrl("en");

        // Assert
        Assert.Null(audioUrl);
    }

    [Fact]
    public void POI_GetAudioUrl_HigherVersionShouldBePreferred()
    {
        // Arrange
        var poi = new POI
        {
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "en", AudioUrl = "audio/en/v1.mp3", IsActive = true, Version = 1 },
                new() { LanguageCode = "en", AudioUrl = "audio/en/v2.mp3", IsActive = true, Version = 2 }
            }
        };

        // Act
        var audioUrl = poi.GetAudioUrl("en");

        // Assert
        Assert.Equal("audio/en/v2.mp3", audioUrl);
    }

    #endregion

    #region 4. Thuyết minh audio - Audio Narration Tests

    [Fact]
    public void POI_AudioLanguagesDisplay_WhenNoAudios_ShouldReturnDefault()
    {
        // Arrange
        var poi = new POI { Audios = new List<AudioModel>() };

        // Act
        var display = poi.AudioLanguagesDisplay;

        // Assert
        Assert.Equal("Đang cập nhật", display);
    }

    [Fact]
    public void POI_AudioLanguagesDisplay_WhenHasAudios_ShouldReturnLanguages()
    {
        // Arrange
        var poi = new POI
        {
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "en", LanguageName = "English", IsActive = true },
                new() { LanguageCode = "vi", LanguageName = "Vietnamese", IsActive = true }
            }
        };

        // Act
        var display = poi.AudioLanguagesDisplay;

        // Assert
        Assert.Contains("English", display);
        Assert.Contains("Vietnamese", display);
    }

    [Fact]
    public void POI_AudioSummaryDisplay_WhenNoAudios_ShouldReturnDefault()
    {
        // Arrange
        var poi = new POI { Audios = new List<AudioModel>() };

        // Act
        var display = poi.AudioSummaryDisplay;

        // Assert
        Assert.Equal("Audio: chưa có bản ghi", display);
    }

    [Fact]
    public void POI_AudioSummaryDisplay_WhenHasActiveAudios_ShouldReturnCount()
    {
        // Arrange
        var poi = new POI
        {
            Audios = new List<AudioModel>
            {
                new() { IsActive = true },
                new() { IsActive = true },
                new() { IsActive = false }
            }
        };

        // Act
        var display = poi.AudioSummaryDisplay;

        // Assert
        Assert.Equal("Audio active: 2 bản ghi", display);
    }

    #endregion

    #region 5. Kích hoạt nội dung qua mã QR - QR Code Tests

    [Fact]
    public void POI_CoordinatesDisplay_ShouldFormatCorrectly()
    {
        // Arrange
        var poi = new POI { Latitude = 10.776889, Longitude = 106.688889 };

        // Act
        var display = poi.CoordinatesDisplay;

        // Assert
        Assert.Contains("10.776889", display);
        Assert.Contains("106.688889", display);
    }

    [Fact]
    public void POI_CreatedAtDisplay_ShouldFormatCorrectly()
    {
        // Arrange
        var poi = new POI { CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0) };

        // Act
        var display = poi.CreatedAtDisplay;

        // Assert
        Assert.Equal("2024-01-15T10:30:00.000", display);
    }

    #endregion

    #region 6. Quyền riêng tư của người dùng - Privacy Tests

    [Fact]
    public void POI_RestaurantId_DefaultShouldBeEmpty()
    {
        // Arrange & Act
        var poi = new POI();

        // Assert
        Assert.Equal(string.Empty, poi.restaurantId);
    }

    [Fact]
    public void POI_Name_CanBeNull()
    {
        // Arrange & Act
        var poi = new POI { Name = null };

        // Assert
        Assert.Null(poi.Name);
    }

    [Fact]
    public void POI_Description_CanBeNull()
    {
        // Arrange & Act
        var poi = new POI { Description = null };

        // Assert
        Assert.Null(poi.Description);
    }

    #endregion

    #region Additional Tests - POI Detail Display

    [Fact]
    public void POI_PrimaryDetailImage_WhenNoImages_ShouldReturnDefault()
    {
        // Arrange
        var poi = new POI { Images = new List<RestaurantImageModel>() };

        // Act
        var image = poi.PrimaryDetailImage;

        // Assert
        Assert.Equal("dotnet_bot.svg", image);
    }

    [Fact]
    public void POI_SecondaryDetailImage_WhenNoSecondImage_ShouldReturnDefault()
    {
        // Arrange
        var poi = new POI
        {
            Images = new List<RestaurantImageModel>
            {
                new() { ImageUrl = "Resources/Images/primary.jpg", IsPrimary = true }
            }
        };

        // Act
        var image = poi.SecondaryDetailImage;

        // Assert
        Assert.Equal("dotnet_bot.svg", image);
    }

    [Fact]
    public void POI_ThirdDetailImage_WhenNoThirdImage_ShouldReturnPrimaryImage()
    {
        // Arrange
        var poi = new POI
        {
            Images = new List<RestaurantImageModel>
            {
                new() { ImageUrl = "Resources/Images/primary.jpg", IsPrimary = true }
            }
        };

        // Act
        var image = poi.ThirdDetailImage;

        // Assert
        Assert.Equal("primary.jpg", image);
    }

    #endregion
}
