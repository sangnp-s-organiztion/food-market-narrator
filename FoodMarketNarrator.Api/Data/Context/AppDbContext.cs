// Using global using to make AppDbContext available project-wide
global using AppDbContext = food_market_narrator_api.Data.Context.AppDbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using food_market_narrator_api.Models;

namespace food_market_narrator_api.Data.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Bảng nhà hàng
    public DbSet<RestaurantModel> Restaurant { get; set; }
    public DbSet<RestaurantImageModel> RestaurantImage { get; set; }
    public DbSet<AudioModel> Audio { get; set; }
    public DbSet<DishModel> Dish { get; set; }
    public DbSet<LanguageModel> Language { get; set; }
    public DbSet<UserModel> User { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
}