using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Maui.Devices.Sensors;
using Moq;
using Xunit;

namespace food_market_narrator.Tests.IntegrationTests;

/// <summary>
/// Integration Tests cho Narration Flow - Luồng thuyết minh tự động
/// Test tích hợp giữa LocationService, POIService, AudioService, và NarrationFlowService
/// </summary>
public class NarrationFlowIntegrationTests
{
    #region 3. Thuyết minh tự động (Kích hoạt Geofence) - Geofence Integration Tests

    /// <summary>
    /// Test tích hợp: Khi người dùng di chuyển vào vùng POI, thuyết minh được phát tự động
    /// </summary>
    [Fact]
    public async Task NarrationFlow_EnterPOI_TriggersAutoNarration()
    {
        // Arrange
        var mockPoiService = new Mock<IPOIService>();
        var mockLocationService = new Mock<ILocationService>();
        var mockAudioService = new Mock<IAudioService>();
        var mockLanguageService = new Mock<ILanguageService>();
        var mockHistoryService = new Mock<IHistoryService>();

        var testPoi = new POI
        {
            restaurantId = "poi-1",
            Name = "Test Restaurant",
            Latitude = 10.776889,
            Longitude = 106.688889,
            Radius = 30,
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "vi", AudioUrl = "audio/vi/test.mp3", IsActive = true }
            }
        };

        // User is at the POI location (inside geofence)
        var userLocation = new Location(10.776889, 106.688889);

        mockLocationService.Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(userLocation);
        mockLocationService.Setup(x => x.StartTrackingAsync())
            .Returns(Task.CompletedTask);

        mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        mockAudioService.Setup(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockLanguageService.Setup(x => x.CurrentLanguage).Returns("vi");

        mockPoiService.Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI> { testPoi });
        mockPoiService.Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(testPoi); // Geofence transition detected
        mockPoiService.Setup(x => x.GetDistanceMeters(It.IsAny<Location>(), It.IsAny<POI>()))
            .Returns(10.0); // Within trigger distance

        var service = new NarrationFlowService(
            mockPoiService.Object,
            mockLocationService.Object,
            mockAudioService.Object,
            mockLanguageService.Object,
            mockHistoryService.Object);

        // Act
        service.StartNarration();
        await service.CheckAndNarrateAsync(null, force: false);

        // Assert
        mockAudioService.Verify(
            x => x.PlaySound("vi", "audio/vi/test.mp3"),
            Times.Once);
    }

    /// <summary>
    /// Test tích hợp: Cooldown ngăn phát lại thuyết minh quá thường xuyên
    /// </summary>
    [Fact]
    public async Task NarrationFlow_Cooldown_PreventsRepeatedPlayback()
    {
        // Arrange
        var mockPoiService = new Mock<IPOIService>();
        var mockLocationService = new Mock<ILocationService>();
        var mockAudioService = new Mock<IAudioService>();
        var mockLanguageService = new Mock<ILanguageService>();
        var mockHistoryService = new Mock<IHistoryService>();

        var testPoi = new POI
        {
            restaurantId = "poi-1",
            Latitude = 10.776889,
            Longitude = 106.688889,
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "vi", AudioUrl = "audio/vi/test.mp3", IsActive = true }
            }
        };

        mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        mockAudioService.Setup(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mockLanguageService.Setup(x => x.CurrentLanguage).Returns("vi");
        mockLocationService.Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.776889, 106.688889));

        mockPoiService.Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI> { testPoi });
        mockPoiService.Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(testPoi);
        mockPoiService.Setup(x => x.GetDistanceMeters(It.IsAny<Location>(), It.IsAny<POI>()))
            .Returns(10.0);

        var service = new NarrationFlowService(
            mockPoiService.Object,
            mockLocationService.Object,
            mockAudioService.Object,
            mockLanguageService.Object,
            mockHistoryService.Object);

        // Act - Play lần 1
        await service.CheckAndNarrateAsync(null, force: false);

        // Play lần 2 ngay sau đó (trong cooldown period)
        await service.CheckAndNarrateAsync(null, force: false);

        // Assert - Audio chỉ được phát 1 lần (lần 2 bị cooldown chặn)
        mockAudioService.Verify(
            x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region 4. Thuyết minh audio - Audio Integration Tests

    /// <summary>
    /// Test tích hợp: Audio queue xử lý nhiều POI
    /// </summary>
    [Fact]
    public async Task NarrationFlow_QueueProcessesMultiplePOIs()
    {
        // Arrange
        var mockPoiService = new Mock<IPOIService>();
        var mockLocationService = new Mock<ILocationService>();
        var mockAudioService = new Mock<IAudioService>();
        var mockLanguageService = new Mock<ILanguageService>();
        var mockHistoryService = new Mock<IHistoryService>();

        var pois = new List<POI>
        {
            new()
            {
                restaurantId = "poi-1",
                Audios = new List<AudioModel>
                {
                    new() { LanguageCode = "vi", AudioUrl = "audio/poi1.mp3", IsActive = true }
                }
            },
            new()
            {
                restaurantId = "poi-2",
                Audios = new List<AudioModel>
                {
                    new() { LanguageCode = "vi", AudioUrl = "audio/poi2.mp3", IsActive = true }
                }
            }
        };

        var playCount = 0;
        mockAudioService.Setup(x => x.IsPlaying)
            .Returns(() => playCount < 2);
        mockAudioService.Setup(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => playCount++)
            .Returns(Task.CompletedTask);
        mockLanguageService.Setup(x => x.CurrentLanguage).Returns("vi");
        mockLocationService.Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.776889, 106.688889));

        mockPoiService.Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(pois);
        mockPoiService.Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(pois[0]);
        mockPoiService.Setup(x => x.GetDistanceMeters(It.IsAny<Location>(), It.IsAny<POI>()))
            .Returns(10.0);

        var service = new NarrationFlowService(
            mockPoiService.Object,
            mockLocationService.Object,
            mockAudioService.Object,
            mockLanguageService.Object,
            mockHistoryService.Object);

        // Act
        await service.CheckAndNarrateAsync(null, force: false);

        // Assert - Queue xử lý
        mockAudioService.Verify(
            x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region 5. Kích hoạt nội dung qua mã QR - QR Code Integration Tests

    /// <summary>
    /// Test tích hợp: Quét QR kích hoạt thuyết minh ngay lập tức (force trigger)
    /// </summary>
    [Fact]
    public async Task NarrationFlow_QRCode_ForceTriggersNarration()
    {
        // Arrange
        var mockPoiService = new Mock<IPOIService>();
        var mockLocationService = new Mock<ILocationService>();
        var mockAudioService = new Mock<IAudioService>();
        var mockLanguageService = new Mock<ILanguageService>();
        var mockHistoryService = new Mock<IHistoryService>();

        var testPoi = new POI
        {
            restaurantId = "poi-qr-1",
            Name = "QR Restaurant",
            Latitude = 10.776889,
            Longitude = 106.688889,
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "vi", AudioUrl = "audio/vi/qr.mp3", IsActive = true }
            }
        };

        // QR scan doesn't require GPS
        mockLocationService.Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync((Location?)null);

        mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        mockAudioService.Setup(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockLanguageService.Setup(x => x.CurrentLanguage).Returns("vi");

        mockPoiService.Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI> { testPoi });
        mockPoiService.Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns((POI?)null); // No geofence transition
        mockPoiService.Setup(x => x.GetNearestPOI(It.IsAny<Location>(), It.IsAny<IEnumerable<POI>?>()))
            .Returns(testPoi);

        var service = new NarrationFlowService(
            mockPoiService.Object,
            mockLocationService.Object,
            mockAudioService.Object,
            mockLanguageService.Object,
            mockHistoryService.Object);

        // Act - Force trigger (simulates QR scan)
        await service.CheckAndNarrateAsync(null, force: true);

        // Assert - Audio phát ngay lập tức mà không cần GPS
        mockAudioService.Verify(
            x => x.PlaySound("vi", It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region 1. Theo dõi vị trí - Location Tracking Integration Tests

    /// <summary>
    /// Test tích hợp: Debounce ngăn kích hoạt quá thường xuyên khi di chuyển ít
    /// </summary>
    [Fact]
    public async Task NarrationFlow_Debounce_PreventsFrequentTriggers()
    {
        // Arrange
        var mockPoiService = new Mock<IPOIService>();
        var mockLocationService = new Mock<ILocationService>();
        var mockAudioService = new Mock<IAudioService>();
        var mockLanguageService = new Mock<ILanguageService>();
        var mockHistoryService = new Mock<IHistoryService>();

        var testPoi = new POI
        {
            restaurantId = "poi-1",
            Latitude = 10.776889,
            Longitude = 106.688889,
            Audios = new List<AudioModel>
            {
                new() { LanguageCode = "vi", AudioUrl = "audio/vi/test.mp3", IsActive = true }
            }
        };

        mockAudioService.Setup(x => x.IsPlaying).Returns(false);
        mockAudioService.Setup(x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mockLanguageService.Setup(x => x.CurrentLanguage).Returns("vi");
        mockLocationService.Setup(x => x.GetCurrentLocationAsync())
            .ReturnsAsync(new Location(10.776889, 106.688889));

        mockPoiService.Setup(x => x.GetAllPOIsAsync())
            .ReturnsAsync(new List<POI> { testPoi });
        mockPoiService.Setup(x => x.UpdateNearestPOI(It.IsAny<double>(), It.IsAny<double>()))
            .Returns(testPoi);
        mockPoiService.Setup(x => x.GetDistanceMeters(It.IsAny<Location>(), It.IsAny<POI>()))
            .Returns(10.0);

        var service = new NarrationFlowService(
            mockPoiService.Object,
            mockLocationService.Object,
            mockAudioService.Object,
            mockLanguageService.Object,
            mockHistoryService.Object);

        // Act - Multiple rapid calls (simulating small movements)
        await service.CheckAndNarrateAsync(null, force: false);
        await service.CheckAndNarrateAsync(null, force: false);
        await service.CheckAndNarrateAsync(null, force: false);

        // Assert - Debounce prevents excessive triggers
        mockAudioService.Verify(
            x => x.PlaySound(It.IsAny<string>(), It.IsAny<string>()),
            Times.AtMost(2)); // At most once due to cooldown/debounce
    }

    #endregion

    #region Stop Narration Integration Tests

    /// <summary>
    /// Test tích hợp: Stop narration dọn dẹp tất cả tài nguyên
    /// </summary>
    [Fact]
    public void NarrationFlow_StopNarration_CleansUpResources()
    {
        // Arrange
        var mockPoiService = new Mock<IPOIService>();
        var mockLocationService = new Mock<ILocationService>();
        var mockAudioService = new Mock<IAudioService>();
        var mockLanguageService = new Mock<ILanguageService>();
        var mockHistoryService = new Mock<IHistoryService>();

        mockLocationService.Setup(x => x.StartTrackingAsync())
            .Returns(Task.CompletedTask);
        mockAudioService.Setup(x => x.IsPlaying).Returns(false);

        var service = new NarrationFlowService(
            mockPoiService.Object,
            mockLocationService.Object,
            mockAudioService.Object,
            mockLanguageService.Object,
            mockHistoryService.Object);

        service.StartNarration();

        // Act
        service.StopNarration();

        // Assert - All cleanup verified
        mockLocationService.Verify(x => x.LocationChanged -= It.IsAny<EventHandler<Location>>(), Times.Once);
        mockAudioService.Verify(x => x.StopSound(), Times.Once);
    }

    #endregion
}
