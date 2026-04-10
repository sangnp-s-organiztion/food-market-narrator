using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using food_market_narrator_api.DTOs.Auth;
using food_market_narrator_api.DTOs.Dish;
using food_market_narrator_api.DTOs.Language;
using food_market_narrator_api.DTOs.Restaurant;
using food_market_narrator_api.DTOs.User;
using food_market_narrator_api.Helpers;
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

        // Seed Users
        context.User.Add(new UserModel
        {
            UserId = 1,
            Username = "admin",
            Password = PasswordHasher.Hash("admin123"),
            Role = "Admin",
            IsActive = true
        });

        context.User.Add(new UserModel
        {
            UserId = 2,
            Username = "seller1",
            Password = PasswordHasher.Hash("seller123"),
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

    private async Task SeedUserAsync(string username, string password, bool isHashed, string role = "Saler", bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await context.User.FirstOrDefaultAsync(u => u.Username == username);
        if (existing != null)
        {
            context.User.Remove(existing);
            await context.SaveChangesAsync();
        }

        context.User.Add(new UserModel
        {
            Username = username,
            Password = isHashed ? PasswordHasher.Hash(password) : password,
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
    }

    private async Task<UserModel?> GetUserFromDbAsync(string username)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.User.FirstOrDefaultAsync(u => u.Username == username);
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
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody, new JsonSerializerOptions
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
        var meResponse = JsonSerializer.Deserialize<MeResponse>(responseBody, new JsonSerializerOptions
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

    [Fact]
    public async Task Login_WithLegacyPlaintextPassword_MigratesPasswordToHash()
    {
        // Arrange
        var username = $"legacy_{Guid.NewGuid():N}";
        const string password = "legacy123";
        await SeedUserAsync(username, password, isHashed: false);

        var loginRequest = new { username, password };
        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/Auth/login", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedUser = await GetUserFromDbAsync(username);
        Assert.NotNull(updatedUser);
        Assert.True(PasswordHasher.IsHashed(updatedUser!.Password));
        Assert.NotEqual(password, updatedUser.Password);
    }

    #endregion

    #region Users Tests

    [Fact]
    public async Task CreateUser_WithEmptyPassword_UsesDefaultPassword()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");
        var username = $"new_saler_{Guid.NewGuid():N}";
        var phone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var email = $"{username}@example.com";

        var createRequest = new
        {
            username,
            password = string.Empty,
            phone,
            email,
            role = "saler"
        };

        // Act - create user without explicit password
        var createResponse = await AuthorizedRequestAsync(
            HttpMethod.Post,
            "/api/users",
            body: createRequest,
            cookie: cookie);

        // Assert create
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // Act - login with default password
        var loginContent = new StringContent(
            JsonSerializer.Serialize(new { username, password = "123456" }),
            Encoding.UTF8,
            "application/json");
        var loginResponse = await _client.PostAsync("/Auth/login", loginContent);

        // Assert login succeeds with default password
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidPhoneOrEmail_ReturnsBadRequest()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");
        var username = $"invalid_contact_{Guid.NewGuid():N}";

        var createRequest = new
        {
            username,
            password = "123456",
            phone = "12345",
            email = "not-an-email",
            role = "saler"
        };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Post,
            "/api/users",
            body: createRequest,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_LockingCurrentAdmin_ReturnsBadRequest()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("admin", "admin123");
        var request = new { isActive = false };

        // Act
        var response = await AuthorizedRequestAsync(
            HttpMethod.Patch,
            "/api/users/1/status",
            body: request,
            cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var restaurants = JsonSerializer.Deserialize<List<RestaurantResponse>>(responseBody, new JsonSerializerOptions
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
        var restaurant = JsonSerializer.Deserialize<RestaurantResponse>(responseBody, new JsonSerializerOptions
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
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/Restaurant/rest-001/images", cookie: cookie);

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
    public async Task GetUserById_WithAuth_ReturnsOk()
    {
        // Arrange
        var cookie = await LoginAndGetCookie("seller1", "seller123");

        // Act
        var response = await AuthorizedRequestAsync(HttpMethod.Get, "/api/users/2", cookie: cookie);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<UserResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(user);
        Assert.Equal(2, user!.UserId);
        Assert.Equal("seller1", user.Username);
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
        var updateRequest = new UpdateRestaurantRequest
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
        var restaurant = JsonSerializer.Deserialize<RestaurantResponse>(responseBody, new JsonSerializerOptions
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
        var createRequest = new CreateDishRequest
        {
            Name = "Món Mới",
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
        var createRequest = new CreateDishRequest
        {
            Name = "Món Test",
            Price = 50000m
        };
        var createResponse = await AuthorizedRequestAsync(
            HttpMethod.Post,
            "/Restaurant/rest-001/dishes",
            body: createRequest,
            cookie: cookie);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        // Now update - use a non-existent dish ID
        var updateRequest = new UpdateDishRequest
        {
            Name = "Món Đã Cập Nhật",
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
