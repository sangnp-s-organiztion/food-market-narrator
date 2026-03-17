using food_market_narrator.Models;

namespace unit_test.Models;

/// <summary>
/// Unit tests for POI model
/// </summary>
public class POI_Model_Tests
{
    #region GetAudioUrl Tests

    [Fact]
    public void GetAudioUrl_WithMatchingLanguage_ReturnsCorrectAudio()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { AudioId = 1, LanguageCode = "vi-VN", AudioUrl = "/audio/vietnamese.mp3", IsActive = true },
                new AudioModel { AudioId = 2, LanguageCode = "en-US", AudioUrl = "/audio/english.mp3", IsActive = true }
            }
        };

        // Act
        var result = poi.GetAudioUrl("vi-VN");

        // Assert
        Assert.Equal("/audio/vietnamese.mp3", result);
    }

    [Fact]
    public void GetAudioUrl_WithCaseInsensitiveLanguage_ReturnsCorrectAudio()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { AudioId = 1, LanguageCode = "vi-VN", AudioUrl = "/audio/vietnamese.mp3", IsActive = true }
            }
        };

        // Act
        var result = poi.GetAudioUrl("VI-vn");

        // Assert
        Assert.Equal("/audio/vietnamese.mp3", result);
    }

    [Fact]
    public void GetAudioUrl_WithNoMatchingLanguage_ReturnsFirstActiveAudio()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { AudioId = 1, LanguageCode = "vi-VN", AudioUrl = "/audio/vietnamese.mp3", IsActive = true },
                new AudioModel { AudioId = 2, LanguageCode = "en-US", AudioUrl = "/audio/english.mp3", IsActive = true }
            }
        };

        // Act
        var result = poi.GetAudioUrl("ja-JP");

        // Assert
        Assert.NotNull(result);
        Assert.True(result == "/audio/vietnamese.mp3" || result == "/audio/english.mp3");
    }

    [Fact]
    public void GetAudioUrl_WithNoActiveAudios_ReturnsNull()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>()
        };

        // Act
        var result = poi.GetAudioUrl("vi-VN");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAudioUrl_WithInactiveAudios_ReturnsNull()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { AudioId = 1, LanguageCode = "vi-VN", AudioUrl = "/audio/vietnamese.mp3", IsActive = false }
            }
        };

        // Act
        var result = poi.GetAudioUrl("vi-VN");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAudioUrl_WithMultipleVersions_ReturnsHighestVersion()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { AudioId = 1, LanguageCode = "vi-VN", AudioUrl = "/audio/v1.mp3", IsActive = true, Version = 1 },
                new AudioModel { AudioId = 2, LanguageCode = "vi-VN", AudioUrl = "/audio/v2.mp3", IsActive = true, Version = 2 },
                new AudioModel { AudioId = 3, LanguageCode = "vi-VN", AudioUrl = "/audio/v3.mp3", IsActive = true, Version = 3 }
            }
        };

        // Act
        var result = poi.GetAudioUrl("vi-VN");

        // Assert
        Assert.Equal("/audio/v3.mp3", result);
    }

    [Fact]
    public void GetAudioUrl_WithEmptyAudioUrl_ReturnsEmptyString()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { AudioId = 1, LanguageCode = "vi-VN", AudioUrl = "", IsActive = true }
            }
        };

        // Act
        var result = poi.GetAudioUrl("vi-VN");

        // Assert - Empty string passes the IsNullOrWhiteSpace check and gets returned
        Assert.Equal("", result);
    }

    #endregion

    #region PrimaryImage Tests

    [Fact]
    public void PrimaryImage_WithPrimaryImage_ReturnsPrimaryImage()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Images = new List<RestaurantImageModel>
            {
                new RestaurantImageModel { ImageId = 1, ImageUrl = "secondary.jpg", IsPrimary = false, SortOrder = 1 },
                new RestaurantImageModel { ImageId = 2, ImageUrl = "primary.jpg", IsPrimary = true, SortOrder = 2 }
            }
        };

        // Act
        var result = poi.PrimaryImage;

        // Assert
        Assert.Equal("primary.jpg", result);
    }

    [Fact]
    public void PrimaryImage_WithNoPrimaryImage_ReturnsFirstBySortOrder()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Images = new List<RestaurantImageModel>
            {
                new RestaurantImageModel { ImageId = 1, ImageUrl = "second.jpg", IsPrimary = false, SortOrder = 2 },
                new RestaurantImageModel { ImageId = 2, ImageUrl = "first.jpg", IsPrimary = false, SortOrder = 1 }
            }
        };

        // Act
        var result = poi.PrimaryImage;

        // Assert
        Assert.Equal("first.jpg", result);
    }

    [Fact]
    public void PrimaryImage_WithNoImages_ReturnsDefaultImage()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Images = new List<RestaurantImageModel>()
        };

        // Act
        var result = poi.PrimaryImage;

        // Assert
        Assert.Equal("dotnet_bot.svg", result);
    }

    [Fact]
    public void PrimaryImage_WithPathPrefix_RemovesPrefix()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Images = new List<RestaurantImageModel>
            {
                new RestaurantImageModel { ImageId = 1, ImageUrl = "Resources/Images/myimage.jpg", IsPrimary = true }
            }
        };

        // Act
        var result = poi.PrimaryImage;

        // Assert
        Assert.Equal("myimage.jpg", result);
    }

    #endregion

    #region StatusText Tests

    [Fact]
    public void StatusText_WhenActive_ReturnsOpenText()
    {
        // Arrange
        var poi = new POI { restaurantId = "resto1", IsActive = true };

        // Act
        var result = poi.StatusText;

        // Assert
        Assert.Equal("Đang mở cửa", result);
    }

    [Fact]
    public void StatusText_WhenInactive_ReturnsClosedText()
    {
        // Arrange
        var poi = new POI { restaurantId = "resto1", IsActive = false };

        // Act
        var result = poi.StatusText;

        // Assert
        Assert.Equal("Đóng cửa", result);
    }

    #endregion

    #region Display Properties Tests

    [Fact]
    public void OpeningHoursDisplay_WithValue_ReturnsValue()
    {
        // Arrange
        var poi = new POI { restaurantId = "resto1", OpeningHours = "09:00 - 21:00" };

        // Act
        var result = poi.OpeningHoursDisplay;

        // Assert
        Assert.Equal("09:00 - 21:00", result);
    }

    [Fact]
    public void OpeningHoursDisplay_WithoutValue_ReturnsDefault()
    {
        // Arrange
        var poi = new POI { restaurantId = "resto1", OpeningHours = null };

        // Act
        var result = poi.OpeningHoursDisplay;

        // Assert
        Assert.Equal("08:00 - 22:00", result);
    }

    [Fact]
    public void AddressDisplay_WithValue_ReturnsValue()
    {
        // Arrange
        var poi = new POI { restaurantId = "resto1", Address = "123 Nguyen Trai" };

        // Act
        var result = poi.AddressDisplay;

        // Assert
        Assert.Equal("123 Nguyen Trai", result);
    }

    [Fact]
    public void AddressDisplay_WithoutValue_ReturnsDefault()
    {
        // Arrange
        var poi = new POI { restaurantId = "resto1", Address = null };

        // Act
        var result = poi.AddressDisplay;

        // Assert
        Assert.Equal("Đang cập nhật địa chỉ", result);
    }

    #endregion

    #region AudioLanguagesDisplay Tests

    [Fact]
    public void AudioLanguagesDisplay_WithActiveAudios_ReturnsLanguageNames()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { LanguageCode = "vi-VN", LanguageName = "Tiếng Việt", IsActive = true },
                new AudioModel { LanguageCode = "en-US", LanguageName = "English", IsActive = true }
            }
        };

        // Act
        var result = poi.AudioLanguagesDisplay;

        // Assert
        Assert.Contains("Tiếng Việt", result);
        Assert.Contains("English", result);
    }

    [Fact]
    public void AudioLanguagesDisplay_WithoutLanguageName_ReturnsLanguageCode()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { LanguageCode = "vi-VN", LanguageName = "", IsActive = true }
            }
        };

        // Act
        var result = poi.AudioLanguagesDisplay;

        // Assert
        Assert.Contains("vi-VN", result);
    }

    [Fact]
    public void AudioLanguagesDisplay_WithNoActiveAudios_ReturnsDefaultText()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>()
        };

        // Act
        var result = poi.AudioLanguagesDisplay;

        // Assert
        Assert.Equal("Đang cập nhật", result);
    }

    #endregion

    #region AudioSummaryDisplay Tests

    [Fact]
    public void AudioSummaryDisplay_WithActiveAudios_ReturnsCount()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>
            {
                new AudioModel { IsActive = true },
                new AudioModel { IsActive = true },
                new AudioModel { IsActive = false }
            }
        };

        // Act
        var result = poi.AudioSummaryDisplay;

        // Assert
        Assert.Equal("Audio active: 2 bản ghi", result);
    }

    [Fact]
    public void AudioSummaryDisplay_WithNoActiveAudios_ReturnsDefaultText()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Audios = new List<AudioModel>()
        };

        // Act
        var result = poi.AudioSummaryDisplay;

        // Assert
        Assert.Equal("Audio: chưa có bản ghi", result);
    }

    #endregion

    #region CoordinatesDisplay Tests

    [Fact]
    public void CoordinatesDisplay_ReturnsFormattedCoordinates()
    {
        // Arrange
        var poi = new POI { restaurantId = "resto1", Latitude = 10.776889, Longitude = 106.689067 };

        // Act
        var result = poi.CoordinatesDisplay;

        // Assert - check format with 6 decimal places
        Assert.Contains("10.776889", result);
        Assert.Contains("106.689067", result);
    }

    #endregion
}
