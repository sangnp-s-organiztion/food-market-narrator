using System.Reflection;
using food_market_narrator.Models;
using food_market_narrator.Services;
using food_market_narrator.Settings;
using Microsoft.Maui.Networking;

namespace unit_test.Services;

/// <summary>
/// Unit tests for TourService internal logic that is platform-safe in test runtime.
/// </summary>
public class TourService_Tests
{
    [Fact]
    public async Task GetToursAsync_WithInternetAndNoCache_CallsApiAndReturnsData()
    {
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            var handler = new TestHttpMessageHandler(request =>
            {
                var payload = "[{\"tourId\":11,\"name\":\"Tour API\",\"stops\":[]}]";
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(payload)
                };
            });

            var service = CreateService(
                handler: handler,
                baseAddress: "http://api-primary:5044/",
                networkAccess: NetworkAccess.Internet,
                appDataDirectory: tempDir);

            // Act
            var tours = await service.GetToursAsync();

            // Assert
            Assert.Single(tours);
            Assert.Equal(11, tours[0].TourId);
            Assert.Equal("Tour API", tours[0].Name);
            Assert.True(handler.CallCount >= 1);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task GetToursAsync_WithOfflineAndValidCache_ReturnsCacheWithoutApiCall()
    {
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            var cacheDir = Path.Combine(tempDir, "offline_cache");
            Directory.CreateDirectory(cacheDir);
            var cacheFile = Path.Combine(cacheDir, "tours.json");
            await File.WriteAllTextAsync(cacheFile, "[{\"tourId\":22,\"name\":\"Tour Cache\",\"stops\":[]}]", CancellationToken.None);

            var handler = new TestHttpMessageHandler(_ =>
                throw new InvalidOperationException("Should not call API in offline mode when cache exists."));

            var service = CreateService(
                handler: handler,
                baseAddress: "http://api-primary:5044/",
                networkAccess: NetworkAccess.None,
                appDataDirectory: tempDir);

            // Act
            var tours = await service.GetToursAsync();

            // Assert
            Assert.Single(tours);
            Assert.Equal(22, tours[0].TourId);
            Assert.Equal("Tour Cache", tours[0].Name);
            Assert.Equal(0, handler.CallCount);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task GetToursAsync_WithOfflineAndCorruptedCache_ReturnsEmptyFallback()
    {
        // Arrange
        var tempDir = CreateTempDir();
        try
        {
            var cacheDir = Path.Combine(tempDir, "offline_cache");
            Directory.CreateDirectory(cacheDir);
            var cacheFile = Path.Combine(cacheDir, "tours.json");
            await File.WriteAllTextAsync(cacheFile, "{ invalid json", CancellationToken.None);

            var handler = new TestHttpMessageHandler(_ =>
                throw new InvalidOperationException("Should not call API while offline."));

            var service = CreateService(
                handler: handler,
                baseAddress: "http://api-primary:5044/",
                networkAccess: NetworkAccess.None,
                appDataDirectory: tempDir);

            // Act
            var tours = await service.GetToursAsync();

            // Assert
            Assert.Empty(tours);
            Assert.Equal(0, handler.CallCount);
        }
        finally
        {
            SafeDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public void BuildTourEndpoint_WithoutLocation_ReturnsBaseTourPath()
    {
        // Arrange
        const string baseUrl = "http://localhost:5044";

        // Act
        var endpoint = (string)InvokePrivateStatic(
            typeof(TourService),
            "BuildTourEndpoint",
            baseUrl,
            null!);

        // Assert
        Assert.Equal($"{baseUrl}/{AppSettings.TourEndpoint}", endpoint);
    }

    [Fact]
    public void BuildTourEndpoint_WithLocation_IncludesCoordinatesAndRadius()
    {
        // Arrange
        const string baseUrl = "http://localhost:5044";
        var location = new Location(15.8801, 108.3614);

        // Act
        var endpoint = (string)InvokePrivateStatic(
            typeof(TourService),
            "BuildTourEndpoint",
            baseUrl,
            location);

        // Assert
        Assert.Contains($"{baseUrl}/{AppSettings.TourEndpoint}?", endpoint);
        Assert.Contains("latitude=15.8801", endpoint);
        Assert.Contains("longitude=108.3614", endpoint);
        Assert.Contains($"radiusMeters={AppSettings.PoiEnterRadiusMeters}", endpoint);
    }

    [Fact]
    public void BuildTourDetailEndpoint_WithLocation_IncludesTourIdAndQuery()
    {
        // Arrange
        const string baseUrl = "http://localhost:5044";
        const int tourId = 7;
        var location = new Location(15.8805, 108.3620);

        // Act
        var endpoint = (string)InvokePrivateStatic(
            typeof(TourService),
            "BuildTourDetailEndpoint",
            baseUrl,
            tourId,
            location);

        // Assert
        Assert.Contains($"{baseUrl}/{AppSettings.TourEndpoint}/{tourId}?", endpoint);
        Assert.Contains("latitude=15.8805", endpoint);
        Assert.Contains("longitude=108.362", endpoint);
        Assert.Contains($"radiusMeters={AppSettings.PoiEnterRadiusMeters}", endpoint);
    }

    [Fact]
    public void BuildBaseUrlCandidates_LastSuccessfulComesFirst_AndDistinctApplied()
    {
        // Arrange
        var service = CreateService(baseAddress: "http://primary:5044/");

        SetPrivateField(service, "_lastSuccessfulBaseUrl", "http://fast:5044/");

        // Act
        var candidates = ((IEnumerable<string>)InvokePrivateInstance(service, "BuildBaseUrlCandidates"))
            .ToList();

        // Assert
        Assert.NotEmpty(candidates);
        Assert.Equal("http://fast:5044", candidates[0]);
        Assert.Contains("http://primary:5044", candidates);
        Assert.Equal(candidates.Count, candidates.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void HasFreshMemoryCache_WithRecentCache_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        SetPrivateField(service, "_cachedTours", new List<TourModel>
        {
            new() { TourId = 1, Name = "Tour 1" }
        });
        SetPrivateField(service, "_memoryCachedAtUtc", DateTime.UtcNow);

        // Act
        var hasFreshCache = (bool)InvokePrivateInstance(service, "HasFreshMemoryCache");

        // Assert
        Assert.True(hasFreshCache);
    }

    [Fact]
    public void HasFreshMemoryCache_WithExpiredCache_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        SetPrivateField(service, "_cachedTours", new List<TourModel>
        {
            new() { TourId = 1, Name = "Tour 1" }
        });
        SetPrivateField(service, "_memoryCachedAtUtc", DateTime.UtcNow.AddMinutes(-10));

        // Act
        var hasFreshCache = (bool)InvokePrivateInstance(service, "HasFreshMemoryCache");

        // Assert
        Assert.False(hasFreshCache);
    }

    [Fact]
    public void ShouldRefreshFromNetwork_WhenLastFetchIsRecent_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        SetPrivateField(service, "_lastNetworkFetchUtc", DateTime.UtcNow);

        // Act
        var shouldRefresh = (bool)InvokePrivateInstance(service, "ShouldRefreshFromNetwork");

        // Assert
        Assert.False(shouldRefresh);
    }

    [Fact]
    public void ShouldRefreshFromNetwork_WhenLastFetchIsOld_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        SetPrivateField(service, "_lastNetworkFetchUtc", DateTime.UtcNow.AddMinutes(-10));

        // Act
        var shouldRefresh = (bool)InvokePrivateInstance(service, "ShouldRefreshFromNetwork");

        // Assert
        Assert.True(shouldRefresh);
    }

    private static TourService CreateService(
        HttpMessageHandler? handler = null,
        string? baseAddress = null,
        NetworkAccess networkAccess = NetworkAccess.Internet,
        string? appDataDirectory = null)
    {
        var effectiveHandler = handler ?? new DummyHttpMessageHandler();
        var client = new HttpClient(effectiveHandler)
        {
            BaseAddress = baseAddress == null ? null : new Uri(baseAddress)
        };

        var resolvedAppDataDir = appDataDirectory ?? Path.Combine(Path.GetTempPath(), "tourservice-tests", "shared");
        Directory.CreateDirectory(resolvedAppDataDir);

        return new TourService(
            client,
            new FakeLocationService(),
            new FakeNetworkAccessService(networkAccess),
            new FakeAppFileSystemService(resolvedAppDataDir));
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "tourservice-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void SafeDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for test temp folders.
        }
    }

    private static object InvokePrivateInstance(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        return method!.Invoke(target, args)!;
    }

    private static object InvokePrivateStatic(Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return method!.Invoke(null, args)!;
    }

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private sealed class DummyHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        }
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public int CallCount { get; private set; }

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_handler(request));
        }
    }

    private sealed class FakeLocationService : ILocationService
    {
        public event EventHandler<Location> LocationChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<Location?> LocationSampled
        {
            add { }
            remove { }
        }

        public Location? LastKnownLocation => null;

        public Task<Location?> GetCurrentLocationAsync() => Task.FromResult<Location?>(null);

        public Task StartTrackingAsync() => Task.CompletedTask;

        public Task<bool> RequestBackgroundLocationPermissionAsync() => Task.FromResult(true);

        public Task<bool> HasBackgroundLocationPermissionAsync() => Task.FromResult(true);

        public void StopTracking() { }
    }

    private sealed class FakeNetworkAccessService : INetworkAccessService
    {
        public FakeNetworkAccessService(NetworkAccess currentNetworkAccess)
        {
            CurrentNetworkAccess = currentNetworkAccess;
        }

        public NetworkAccess CurrentNetworkAccess { get; }
    }

    private sealed class FakeAppFileSystemService : IAppFileSystemService
    {
        public FakeAppFileSystemService(string appDataDirectory)
        {
            AppDataDirectory = appDataDirectory;
        }

        public string AppDataDirectory { get; }
    }
}
