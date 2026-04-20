using food_market_narrator_api.Authorization;
using food_market_narrator_api.Models;
using food_market_narrator_api.Services;
using food_market_narrator_api.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using MongoDB.Driver;


public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Theem Repository vào DI container
        builder.Services.AddScoped<RestaurantRepository>();
        builder.Services.AddScoped<RestaurantService>();
        builder.Services.AddScoped<AudioRepository>();
        builder.Services.AddScoped<AudioService>();
        builder.Services.AddScoped<DishRepository>();
        builder.Services.AddScoped<DishService>();
        builder.Services.AddScoped<LanguageRepository>();
        builder.Services.AddScoped<LanguageService>();
        builder.Services.AddScoped<UserRepository>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<AuditLogService>();
        builder.Services.AddScoped<AnalyticsService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<MongoHealthRepository>();
        builder.Services.AddScoped<MongoHealthService>();
        builder.Services.AddScoped<AnalyticsRepository>();
        builder.Services.AddScoped<LocationLogRepository>();
        builder.Services.AddScoped<LocationLogService>();
        builder.Services.AddScoped<UserSessionRepository>();
        builder.Services.AddScoped<UserSessionService>();
        builder.Services.AddScoped<AudioLogRepository>();
        builder.Services.AddScoped<AudioLogService>();
        builder.Services.AddScoped<TourRepository>();
        builder.Services.AddScoped<TourService>();
        builder.Services.AddScoped<UiTranslationRepository>();
        builder.Services.AddScoped<UiTranslationService>();
        builder.Services.AddScoped<TranslationHistoryRepository>();
        builder.Services.AddScoped<TranslationService>();
        builder.Services.AddScoped<AdminTranslationBillingService>();
        builder.Services.AddHttpClient();
        builder.Services.AddMemoryCache();


        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthSchemes.Saler;
                options.DefaultChallengeScheme = AuthSchemes.Saler;
                options.DefaultSignInScheme = AuthSchemes.Saler;
                options.DefaultSignOutScheme = AuthSchemes.Saler;
            })
            .AddCookie(AuthSchemes.Saler, options =>
            {
                options.Cookie.Name = "fmn_saler_auth_v2";
                options.LoginPath = "/Auth/login";
                options.AccessDeniedPath = "/Auth/login";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                };
            })
            .AddCookie(AuthSchemes.Admin, options =>
            {
                options.Cookie.Name = "fmn_admin_auth_v2";
                options.LoginPath = "/Auth/admin/login";
                options.AccessDeniedPath = "/Auth/admin/login";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    },
                    OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            var combinedCookiePolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(AuthSchemes.Saler, AuthSchemes.Admin)
                .RequireAuthenticatedUser()
                .Build();

            options.DefaultPolicy = combinedCookiePolicy;
            options.FallbackPolicy = combinedCookiePolicy;
        });

        builder.Services.AddCors(options =>
        {
            var configuredOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(origin => origin.Trim().TrimEnd('/'))
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            options.AddPolicy("SalerCors", policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                    {
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        {
                            return false;
                        }

                        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }

                        var normalizedOrigin = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
                        return configuredOrigins.Contains(normalizedOrigin);
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // Lấy connection string từ appsettings.json
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // Đăng ký context
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
        builder.Services.Configure<LibreTranslateSettings>(builder.Configuration.GetSection("LibreTranslate"));
        builder.Services.Configure<EdgeTtsSettings>(builder.Configuration.GetSection("EdgeTts"));
        builder.Services.Configure<TranslationPricingSettings>(builder.Configuration.GetSection("TranslationPricing"));
        builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
        builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(mongoSettings.ConnectionString);
        });
        builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
        {
            var mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
            return mongoClient.GetDatabase(mongoSettings.DatabaseName);
        });

        


        // Kiểm tra kết nối đến cơ sở dữ liệu
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                dbContext.Database.CanConnect();
                logger.LogInformation("Kết nối đến cơ sở dữ liệu thành công.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi kết nối đến cơ sở dữ liệu.");
            }
        }










        // Render cấp cổng động qua biến môi trường PORT; fallback:
        // - Development: 5044 (local)
        // - Non-Development: 10000 (mặc định thường dùng trên Render)
        var renderPort = Environment.GetEnvironmentVariable("PORT");
        if (int.TryParse(renderPort, out var parsedPort) && parsedPort > 0)
        {
            builder.WebHost.UseUrls($"http://0.0.0.0:{parsedPort}");
        }
        else
        {
            var fallbackPort = builder.Environment.IsDevelopment() ? 5044 : 10000;
            builder.WebHost.UseUrls($"http://0.0.0.0:{fallbackPort}");
        }




        // Add services to the container.
        builder.Services.AddControllers(options =>
        {
            options.Conventions.Add(new PublicEndpointConvention(PublicEndpoints.Definitions));
        });
        //builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen();
        var app = builder.Build();
        // Configure the HTTP request pipeline.
        //if (app.Environment.IsDevelopment())
        //{
        //    app.UseSwagger();
        //    app.UseSwaggerUI();
        //}
        var isRunningOnRender = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER"));
        if (!app.Environment.IsDevelopment() && !isRunningOnRender)
        {
            app.UseHttpsRedirection();
        }
        app.UseCors("SalerCors");
        app.UseStaticFiles();
        var mauiImagesDir = Path.GetFullPath(
            Path.Combine(
                app.Environment.ContentRootPath,
                "..",
                "FoodMarketNarrator.Maui",
                "Resources",
                "Images"));
        Directory.CreateDirectory(mauiImagesDir);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(mauiImagesDir),
            RequestPath = "/maui-images"
        });

        var mauiNarrationAudioDir = Path.GetFullPath(
            Path.Combine(
                app.Environment.ContentRootPath,
                "..",
                "FoodMarketNarrator.Maui",
                "Resources",
                "Narration",
                "audio"));
        Directory.CreateDirectory(mauiNarrationAudioDir);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(mauiNarrationAudioDir),
            RequestPath = "/maui-audios"
        });

        var uploadedAudiosDir = Path.GetFullPath(
            Path.Combine(
                app.Environment.ContentRootPath,
                "wwwroot",
                "uploads",
                "audios"));
        Directory.CreateDirectory(uploadedAudiosDir);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploadedAudiosDir),
            RequestPath = "/uploads/audios"
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<food_market_narrator_api.Middleware.AuditLoggingMiddleware>();
        // Health check endpoint cho platform (Render) khong phu thuoc DB.
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        // Endpoint tai APK public: neu thieu file se tra 404 thay vi 401.
        app.MapGet("/uploads/apk/{fileName}", (string fileName, IWebHostEnvironment env) =>
        {
            var safeFileName = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                return Results.BadRequest(new { message = "File name is required" });
            }

            var apkPath = Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "apk", safeFileName);
            if (!File.Exists(apkPath))
            {
                return Results.NotFound(new { message = "APK file not found" });
            }

            return Results.File(
                apkPath,
                "application/vnd.android.package-archive",
                fileDownloadName: safeFileName,
                enableRangeProcessing: true);
        }).AllowAnonymous();
        app.MapControllers();
        app.Run();
    }
}
