using food_market_narrator_api.Authorization;
using food_market_narrator_api.Services;
using food_market_narrator_api.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;


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
        builder.Services.AddScoped<AuthService>();

        builder.Services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "fmn_saler_auth";
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
            });

        builder.Services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("SalerCors", policy =>
            {
                policy
                    .SetIsOriginAllowed(origin =>
                    {
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        {
                            return false;
                        }

                        return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
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

        


        // Kiểm tra kết nối đến cơ sở dữ liệu
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            try
            {
                dbContext.Database.CanConnect();
                Console.WriteLine("Kết nối đến cơ sở dữ liệu thành công.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kết nối đến cơ sở dữ liệu: {ex.Message}");
            }
        }










        builder.WebHost.UseUrls("http://0.0.0.0:5044");




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
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        app.UseCors("SalerCors");
        app.UseStaticFiles();
        var mauiImagesDir = Path.GetFullPath(
            Path.Combine(
                app.Environment.ContentRootPath,
                "..",
                "food-market-narrator-maui",
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
                "food-market-narrator-maui",
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
        app.MapControllers();
        app.Run();
    }
}
