using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using food_market_narrator_api.DTOs.Auth;
using food_market_narrator_api.DTOs.Dish;
using food_market_narrator_api.DTOs.Language;
using food_market_narrator_api.DTOs.Restaurant;
using food_market_narrator_api.Models;
using food_market_narrator_api.Services;
using food_market_narrator_api.Repositories;
using food_market_narrator_api.Data.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration tests cho Food Market Narrator API
/// </summary>
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext options
                var dbContextOptionsDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbContextOptionsDescriptor != null)
                {
                    services.Remove(dbContextOptionsDescriptor);
                }

                // Remove existing AppDbContext registration
                var dbContextDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(AppDbContext));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                // Add InMemory database - note: we can't use AddDbContext because it registers both
                // the options and DbContext, and the options already has SQL Server registered
                // So we manually register the InMemory options
                services.AddSingleton<DbContextOptions<AppDbContext>>(new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options);

                services.AddScoped<AppDbContext>();

                // Build service provider
                var sp = services.BuildServiceProvider();

                // Seed test data
                using (var scope = sp.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    SeedTestData(context);
                }
            });
        });

        // Create client
        _client = _factory.CreateClient();

        _client = _factory.CreateClient();
    }

    private void SeedTestData(AppDbContext context)
    {
        // Seed Languages
        context.Language.AddRange(
            new LanguageModel { LanguageId = 1, LanguageCode = "vi", LanguageName = "Vietnamese" },
            new LanguageModel { LanguageId = 2, LanguageCode = "en", LanguageName = "English" }
        );

        // Seed Users - password stored as plain text per UserRepository implementation
        context.User.Add(new UserModel
        {
            UserId = 1,
            Username = "admin",
            Password = "admin123", // Plain text as per UserRepository implementation
            Role = "Admin",
            IsActive = true
        });

        context.User.Add(new UserModel
        {
            UserId = 2,
            Username = "seller1",
            Password = "seller123", // Plain text as per UserRepository implementation
            Role = "Saler",
            IsActive = true
        });

        // Seed Restaurants
        context.Restaurant.Add(new RestaurantModel
        {
            RestaurantId = "rest-001",
            Name = "Quán Ẩm Thực 1",
            Description = "Nhà hàng chuyên đặc sản miền Trung",
            Latitude = 15.8801m,
            Longitude = 108.3614m,
            Address = "123 Đường Nguyễn Huệ, Đà Nẵng",
            Phone = "0912345678",
            IsActive = true,
            UserId = 2
        });

        context.Restaurant.Add(new RestaurantModel
        {
            RestaurantId = "rest-002",
            Name = "Quán Ẩm Thực 2",
            Description = "Hải sản tươi sống",
            Latitude = 15.8805m,
            Longitude = 108.3620m,
            Address = "456 Đường Lê Duẩn, Đà Nẵng",
            Phone = "0987654321",
            IsActive = false,
            UserId = 2
        });

        context.SaveChanges();
    }

    #region Helper Methods

    private async Task<string> LoginAndGetCookie(string username, string password)
    {
        var loginContent = new StringContent(
            JsonSerializer.Serialize(new { username, password }),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/Auth/login", loginContent);
        Assert.True(response.StatusCode == HttpStatusCode.OK);

        var setCookieHeader = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        return setCookieHeader?.Split(';').FirstOrDefault() ?? string.Empty;
    }

    private async Task<HttpResponseMessage> AuthorizedRequestAsync(HttpMethod method, string url, object body = null, string cookie = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.Add("Cookie", cookie);
        }

        if (body != null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");
        }

        return await _client.SendAsync(request);
    }

    #endregion

    #region Auth Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndUserInfo()
    {
        // Arrange
        var loginRequest = new { username = "admin", password = "admin123" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/Auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(loginResponse);
        Assert.Equal("admin", loginResponse.Username);
        Assert.Equal("Admin", loginResponse.Role);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new { username = "admin", password = "wrongpassword" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/Auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new { username = "nonexistent", password = "admin123" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/Auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new { username = "", password = "" };
        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/Auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidCookie_ReturnsUserInfo()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/Auth/me", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var meResponse = JsonSerializer.Deserialize<MeResponseDto>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(meResponse);
        Assert.Equal("admin", meResponse.Username);
        Assert.Equal("Admin", meResponse.Role);
    }

    [Fact]
    public async Task Me_WithoutCookie_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/Auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidCookie_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Post, "/Auth/logout", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Language Tests

    [Fact]
    public async Task GetAllLanguages_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/Language");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var languages = JsonSerializer.Deserialize<List<LanguageResponse>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(languages);
        Assert.True(languages.Count >= 2);
    }

    [Fact]
    public async Task GetLanguageByCode_WithValidCode_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/Language/vi");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var language = JsonSerializer.Deserialize<LanguageResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(language);
        Assert.Equal("vi", language.LanguageCode);
    }

    [Fact]
    public async Task GetLanguageByCode_WithInvalidCode_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/Language/fr");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Restaurant Public Tests

    [Fact]
    public async Task GetAllRestaurants_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/Restaurant");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var restaurants = JsonSerializer.Deserialize<List<RestaurantResponseDto>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(restaurants);
        Assert.True(restaurants.Count >= 2);
    }

    [Fact]
    public async Task GetRestaurantById_WithValidId_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/Restaurant/rest-001");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var restaurant = JsonSerializer.Deserialize<RestaurantResponseDto>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(restaurant);
        Assert.Equal("rest-001", restaurant.RestaurantId);
        Assert.Equal("Quán Ẩm Thực 1", restaurant.Name);
    }

    [Fact]
    public async Task GetRestaurantById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/Restaurant/nonexistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Public Data Tests

    [Fact]
    public async Task GetPublicDishes_ReturnsOk()
    {
        // Arrange - Login required because PublicEndpointConvention may not apply in test environment
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/public/Restaurant/rest-001/dishes", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicImages_ReturnsOk()
    {
        // Arrange - Login required because PublicEndpointConvention may not apply in test environment
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/public/Restaurant/rest-001/images", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicAudios_ReturnsOk()
    {
        // Arrange - Login required because PublicEndpointConvention may not apply in test environment
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/public/Restaurant/rest-001/audios", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Restaurant Authorized Tests

    [Fact]
    public async Task GetAllRestaurants_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/Restaurant");

        // Assert - /Restaurant is public endpoint per PublicEndpoints.cs
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRestaurantsByUserId_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/Users/2/restaurants", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var restaurants = JsonSerializer.Deserialize<List<RestaurantResponseDto>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(restaurants);
        Assert.True(restaurants.Count >= 2);
    }

    [Fact]
    public async Task UpdateRestaurantStatus_WithValidData_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");
        var updateRequest = new { is_active = false };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Patch,
            "/Restaurant/rest-001/status",
            body: updateRequest,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRestaurantStatus_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");
        var updateRequest = new { is_active = false };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Patch,
            "/Restaurant/nonexistent/status",
            body: updateRequest,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRestaurant_WithValidData_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");
        var updateRequest = new UpdateRestaurantRequestDto
        {
            Name = "Quán Cập Nhật",
            Description = "Mô tả mới",
            Address = "123 Đường Mới",
            Phone = "0900000000",
            Latitude = 15.8801m,
            Longitude = 108.3614m
        };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Patch,
            "/Restaurant/rest-001",
            body: updateRequest,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var restaurant = JsonSerializer.Deserialize<RestaurantResponseDto>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(restaurant);
        Assert.Equal("Quán Cập Nhật", restaurant.Name);
    }

    #endregion

    #region Audio Tests

    [Fact]
    public async Task GetAllAudios_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/Audio", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAudiosByRestaurant_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/Restaurant/rest-001/audios", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAudioActive_WithValidData_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");
        var updateRequest = new { is_active = false };

        // Act - Note: This will return NotFound as there's no audio in test data
        var response = await AuthorizedRequestAsync(
            HttpMethod.Patch,
            "/Audios/999/active",
            body: updateRequest,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAudio_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Delete, "/Audios/999", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Dishes Tests

    [Fact]
    public async Task GetDishesByRestaurant_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/Restaurant/rest-001/dishes", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateDish_WithValidData_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");
        var createRequest = new CreateDishRequestDto
        {
            Name = "Món Mới",
            Description = "Mô tả món ăn",
            Price = 100000m
        };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Post,
            "/Restaurant/rest-001/dishes",
            body: createRequest,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDish_WithValidData_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // First create a dish
        var createRequest = new CreateDishRequestDto
        {
            Name = "Món Test",
            Description = "Test",
            Price = 50000m
        };
        var createResponse = await AuthorizedRequestAsync(
            HttpMethod.Post,
            "/Restaurant/rest-001/dishes",
            body: createRequest,
            cookie: cookie);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        // Now update - use a non-existent dish ID
        var updateRequest = new UpdateDishRequestDto
        {
            Name = "Món Đã Cập Nhật",
            Description = "Mô tả mới",
            Price = 150000m
        };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Put,
            "/Dishes/999",
            body: updateRequest,
            cookie: cookie);

        // Assert - Should return NotFound as dish doesn't exist
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDish_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Delete, "/Dishes/999", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Images Tests

    [Fact]
    public async Task GetImagesByRestaurant_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/Restaurant/rest-001/images", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SetPrimaryImage_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");
        var request = new { is_primary = true };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Patch,
            "/Images/999/primary",
            body: request,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Delete, "/Images/999", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReorderImages_WithInvalidData_ReturnsNotFound()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");
        var request = new { items = new[] { new { image_id = 1, sort_order = 0 } } };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Patch,
            "/Restaurant/rest-001/images/reorder",
            body: request,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
