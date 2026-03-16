using food_market_narrator.Models;
using Xunit;

namespace food_market_narrator.Tests.UnitTests.Models;

/// <summary>
/// Unit Tests cho LanguageModel
/// </summary>
public class LanguageModelTests
{
    [Fact]
    public void LanguageModel_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var language = new LanguageModel();

        // Assert
        Assert.Equal(0, language.LanguageId);
        Assert.Equal(string.Empty, language.LanguageName);
        Assert.Equal(string.Empty, language.LanguageCode);
    }

    [Fact]
    public void LanguageModel_CanSetProperties()
    {
        // Arrange & Act
        var language = new LanguageModel
        {
            LanguageId = 1,
            LanguageName = "Vietnamese",
            LanguageCode = "vi"
        };

        // Assert
        Assert.Equal(1, language.LanguageId);
        Assert.Equal("Vietnamese", language.LanguageName);
        Assert.Equal("vi", language.LanguageCode);
    }
}
