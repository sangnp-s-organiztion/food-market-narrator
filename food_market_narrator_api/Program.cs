using food_market_narrator_api.Services;
using food_market_narrator_api.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;


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















        // Add services to the container.
        builder.Services.AddControllers();
        //builder.Services.AddEndpointsApiExplorer();
        //builder.Services.AddSwaggerGen();
        var app = builder.Build();
        // Configure the HTTP request pipeline.
        //if (app.Environment.IsDevelopment())
        //{
        //    app.UseSwagger();
        //    app.UseSwaggerUI();
        //}
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
