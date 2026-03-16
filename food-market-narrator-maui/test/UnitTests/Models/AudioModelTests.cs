using food_market_narrator.Models;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Models;

/// <summary>
/// Unit Tests cho AudioModel
/// </summary>
public class AudioModelTests
{
    [Fact]
    public void AudioModel_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var audio = new AudioModel();

        // Assert
        Assert.Equal(string.Empty, audio.RestaurantId);
        Assert.Equal(string.Empty, audio.LanguageName);
        Assert.Equal(string.Empty, audio.LanguageCode);
        Assert.Equal(string.Empty, audio.AudioUrl);
        Assert.Equal(1, audio.Version);
        Assert.True(audio.IsActive);
    }

    [Fact]
    public void AudioModel_CanSetProperties()
    {
        // Arrange & Act
        var audio = new AudioModel
        {
            AudioId = 1,
            RestaurantId = "restaurant-1",
            LanguageId = 1,
            LanguageName = "Vietnamese",
            LanguageCode = "vi",
            AudioUrl = "audio/vi/test.mp3",
            Version = 2,
            IsActive = true,
            DateGeneration = new DateTime(2024, 1, 1)
        };

        // Assert
        Assert.Equal(1, audio.AudioId);
        Assert.Equal("restaurant-1", audio.RestaurantId);
        Assert.Equal(1, audio.LanguageId);
        Assert.Equal("Vietnamese", audio.LanguageName);
        Assert.Equal("vi", audio.LanguageCode);
        Assert.Equal("audio/vi/test.mp3", audio.AudioUrl);
        Assert.Equal(2, audio.Version);
        Assert.True(audio.IsActive);
        Assert.Equal(new DateTime(2024, 1, 1), audio.DateGeneration);
    }
}
