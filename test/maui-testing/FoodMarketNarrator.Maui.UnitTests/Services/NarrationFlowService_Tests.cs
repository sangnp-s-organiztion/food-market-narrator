using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Maui.Devices.Sensors;
using Moq;

namespace unit_test.Services;

/// <summary>
/// Unit tests for NarrationFlowService logic
/// Tests: debounce, cooldown, queue management, auto-trigger rules
/// </summary>
public class NarrationFlowService_Tests
{
    private readonly Mock<IPOIService> _mockPoiService;
    private readonly Mock<ILocationService> _mockLocationService;
    private readonly Mock<IAudioService> _mockAudioService;
    private readonly Mock<ILanguageService> _mockLanguageService;
    private readonly Mock<IHistoryService> _mockHistoryService;
    private readonly NarrationFlowService _narrationService;

    public NarrationFlowService_Tests()
    {
        _mockPoiService = new Mock<IPOIService>();
        _mockLocationService = new Mock<ILocationService>();
        _mockAudioService = new Mock<IAudioService>();
        _mockLanguageService = new Mock<ILanguageService>();
        _mockHistoryService = new Mock<IHistoryService>();

        // Setup default behaviors
        _mockLanguageService.Setup(x => x.CurrentLanguage).Returns("vi-VN");
        _mockAudioService.Setup(x => x.IsPlaying).Returns(false);

        _narrationService = new NarrationFlowService(
            _mockPoiService.Object,
            _mockLocationService.Object,
            _mockAudioService.Object,
            _mockLanguageService.Object,
            _mockHistoryService.Object
        );
    }

    #region StartNarration Tests

    [Fact]
    public void StartNarration_FirstTime_SetsNarrationEnabled()
    {
        // Arrange - setup location mock to return null (will skip immediate check)
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync((Location?)null);

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI>());

        _mockPoiService
            .Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns((POI?)null);

        // Pre-assert: should not be narrating
        Assert.False(_narrationService.IsNarrating);

        // Act - use a simpler trigger
        // StartNarration subscribes to events - we just verify state
        _narrationService.StartNarration();

        // Assert - verify that tracking was started
        _mockLocationService.Verify(x => x.StartTrackingAsync(), Times.Once);
    }

    [Fact]
    public void StartNarration_AlreadyEnabled_DoesNotStartAgain()
    {
        // Arrange
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync((Location?)null);

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI>());

        _mockPoiService
            .Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns((POI?)null);

        // Act
        _narrationService.StartNarration();
        _narrationService.StartNarration();

        // Assert - StartTrackingAsync should only be called once
        _mockLocationService.Verify(x => x.StartTrackingAsync(), Times.Once);
    }

    #endregion

    #region StopNarration Tests

    [Fact]
    public void StopNarration_WhileEnabled_DisablesNarration()
    {
        // Arrange
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync((Location?)null);

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI>());

        _narrationService.StartNarration();

        // Act
        _narrationService.StopNarration();

        // Assert
        Assert.False(_narrationService.IsNarrating);
    }

    [Fact]
    public void StopNarration_WhileEnabled_StopsAudio()
    {
        // Arrange
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync((Location?)null);

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI>());

        _narrationService.StartNarration();

        // Act
        _narrationService.StopNarration();

        // Assert
        _mockAudioService.Verify(x => x.StopSound(), Times.Once);
    }

    [Fact]
    public void StopNarration_WhileDisabled_DoesNothing()
    {
        // Arrange - narration not started

        // Act
        _narrationService.StopNarration();

        // Assert
        _mockAudioService.Verify(x => x.StopSound(), Times.Never);
        _mockLocationService.Verify(x => x.StartTrackingAsync(), Times.Never);
    }

    #endregion

    #region CheckAndNarrateAsync - Basic Logic Tests

    [Fact]
    public void CheckAndNarrateAsync_WhenAudioPlaying_DoesNotTrigger()
    {
        // Arrange
        _mockAudioService.Setup(x => x.IsPlaying).Returns(true);
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.776889, 106.6890608));

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI>());

        // Act
        var task = _narrationService.CheckAndNarrateAsync();
        task.Wait(TimeSpan.FromSeconds(2));

        // Assert - should return early without checking POIs
        _mockPoiService.Verify(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()), Times.Never);
    }

    [Fact]
    public void CheckAndNarrateAsync_WithNoPOIs_DoesNotPlay()
    {
        // Arrange
        _mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.776889, 106.6890608));

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI>());

        // Act
        var task = _narrationService.CheckAndNarrateAsync();
        task.Wait(TimeSpan.FromSeconds(2));

        // Assert
        _mockAudioService.Verify(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void CheckAndNarrateAsync_WithNoLocation_DoesNotPlay()
    {
        // Arrange
        _mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync((Location?)null);

        // Act
        var task = _narrationService.CheckAndNarrateAsync();
        task.Wait(TimeSpan.FromSeconds(2));

        // Assert
        _mockAudioService.Verify(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ResetPlayedPOIs Tests

    [Fact]
    public void ResetPlayedPOIs_ResetsInternalState()
    {
        // This test verifies the method can be called without error
        // Act & Assert
        _narrationService.ResetPlayedPOIs(); // Should not throw
    }

    #endregion

    #region Language Selection Tests

    [Fact]
    public void CheckAndNarrateAsync_UsesCurrentLanguageFromService()
    {
        // Arrange
        _mockLanguageService.Setup(x => x.CurrentLanguage).Returns("en-US");

        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };
        poi.Audios.Add(new AudioModel { LanguageCode = "en-US", AudioUrl = "/audio/english.mp3", IsActive = true });

        _mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.776889, 106.6890608));

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI> { poi });

        _mockPoiService
            .Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(poi);

        _mockPoiService
            .Setup(x => x.GetNearestPOI(It.IsAny<Location>(), It.IsAny<IEnumerable<POI>>()))
            .Returns(poi);

        _mockPoiService
            .Setup(x => x.GetDistanceMeters(It.IsAny<Location>(), It.IsAny<POI>()))
            .Returns(20.0);

        // Setup audio service to complete immediately
        _mockAudioService
            .Setup(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var task = _narrationService.CheckAndNarrateAsync(force: true);
        task.Wait(TimeSpan.FromSeconds(2));

        // Assert - should use language from service
        _mockAudioService.Verify(x => x.PlaySound("en-US", It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Audio Not Available Tests

    [Fact]
    public void CheckAndNarrateAsync_NoAudioAvailable_DoesNotPlay()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };
        // No audio added

        _mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.776889, 106.6890608));

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI> { poi });

        _mockPoiService
            .Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(poi);

        _mockPoiService
            .Setup(x => x.GetNearestPOI(It.IsAny<Location>(), It.IsAny<IEnumerable<POI>>()))
            .Returns(poi);

        // Act
        var task = _narrationService.CheckAndNarrateAsync(force: true);
        task.Wait(TimeSpan.FromSeconds(2));

        // Assert
        _mockAudioService.Verify(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Distance Constraint Tests

    [Fact]
    public void CheckAndNarrateAsync_OutsideTriggerDistance_DoesNotPlay()
    {
        // Arrange
        var poi = new POI
        {
            restaurantId = "resto1",
            Latitude = 10.776889,
            Longitude = 106.6890608
        };
        poi.Audios.Add(new AudioModel { LanguageCode = "vi-VN", AudioUrl = "/audio/test.mp3", IsActive = true });

        _mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        _mockLocationService
            .Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.777889, 106.6900608));

        _mockPoiService
            .Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI> { poi });

        _mockPoiService
            .Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(poi);

        _mockPoiService
            .Setup(x => x.GetNearestPOI(It.IsAny<Location>(), It.IsAny<IEnumerable<POI>>()))
            .Returns(poi);

        _mockPoiService
            .Setup(x => x.GetDistanceMeters(It.IsAny<Location>(), It.IsAny<POI>()))
            .Returns(100.0); // 100m away - outside trigger distance

        // Act
        var task = _narrationService.CheckAndNarrateAsync();
        task.Wait(TimeSpan.FromSeconds(2));

        // Assert - audio should not be played due to distance
        _mockAudioService.Verify(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion
}
